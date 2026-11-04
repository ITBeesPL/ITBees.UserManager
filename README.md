# ITBees.UserManager

## Security notes for applications

### `LastUsedCompanyGuid` always names a company of the user

Applications usually scope their data by the `LastUsedCompanyGuid` claim of the login token. The
library keeps that claim trustworthy:

- `PUT /MyAccount` stores a company only if the current user belongs to it (`UsersInCompany`);
  anything else is answered with 403 and nothing is saved. An empty guid leaves the company unchanged.
- Every token the library issues (password, Google / Apple / Facebook, e-mail confirmation, operator
  "login as", `POST /RefreshToken`, the refresh in `PUT /MyAccount`) names a company the user belongs
  to at that moment. A
  stored company the user was removed from falls back to the oldest company of the user, and the
  account is corrected.

A token issued earlier keeps its claim until it expires, so an application that has to react to a
removed membership immediately still needs its own check.

### Restricted tokens

`POST /RefreshToken` and `PUT /MyAccount` answer with a regular login token of the token subject. An
application that issues its own tokens with narrowed-down rights (a kiosk session, a device token, ...)
must mark them with the `TokenPurpose` claim - regular login tokens must not carry it. Both endpoints
refuse such a token with 403: it is never exchanged for a login token, and `PUT /MyAccount` does not
change the account either.

Tokens marked in another way are added in the application:

```csharp
services.Configure<RestrictedTokenOptions>(o => o.ClaimTypes.Add("MyClaimType"));
```

### Refreshing a login token

The platform issues no refresh tokens: a login token is valid for `TokenLifeTime` minutes (configuration)
and the client keeps it until then. A client that wants to keep its session alive calls
`POST /RefreshToken` - empty body, the current token in the `Authorization` header - before the token
expires and gets a fresh `TokenVm` (the model `POST /Login` answers with) for the same user with a new
expiry. The claims come from the account as it is at that moment, so a changed language or selected
company shows up in the refreshed token. A refresh is not a login: `LoginsCount` stays, only
`LastLoginDateTime` moves. An expired token is refused with 401 - the user has to log in again - and a
restricted token with 403 (see above).

### Links of the invitation e-mails

`CreateAndInviteNewUserToCompany` (and `POST /NewInvitedUserRegistration`, which calls it) sends the invited
person to a page of the application: a new or never confirmed account to the page where it sets the password
(fas-auth `auth/acceptInvitation`), a registered user to the page that logs them in and accepts the invitation
(`auth/acceptInvitationExistingUser`; `GET /AcceptInvitation` needs their token, so a link straight at the API
cannot accept anything). The address of the page comes from, in this order:

1. `accountEmailActivationBaseLink` passed to `CreateAndInviteNewUserToCompany` - Kilometrówka, Kartony24 and
   Octopark pass their own pages; `POST /NewInvitedUserRegistration` passes none;
2. the platform settings `AcceptInvitationUrl` (set the password) and `AcceptInvitationExistingUserUrl`
   (registered user) in `config.json`, for an application whose pages are elsewhere;
3. the fas-auth pages under `IUserManagerSettings.APPLICATION_SITE_URL`:
   `{APPLICATION_SITE_URL}/auth/acceptInvitation` and `{APPLICATION_SITE_URL}/auth/acceptInvitationExistingUser` -
   the default of every application using fas-auth, with or without the trailing slash in the setting;
4. `DefaultApiUrl`, as up to 8.0.413, only when there is no site URL either.

The library appends `?emailInvitation=true&token=...` to the first page and
`/acceptInvitation?emailInvitation=true&email=...&companyGuid=...` to the second.

## Roles and permissions

A role is a name plus a set of permissions; applications and libraries declare their permissions in
code and protect endpoints with `[HasPermission]`. See [docs/PERMISSIONS.md](docs/PERMISSIONS.md).
