# ITBees.UserManager

## Security notes for applications

### `LastUsedCompanyGuid` always names a company of the user

Applications usually scope their data by the `LastUsedCompanyGuid` claim of the login token. The
library keeps that claim trustworthy:

- `PUT /MyAccount` stores a company only if the current user belongs to it (`UsersInCompany`);
  anything else is answered with 403 and nothing is saved. An empty guid leaves the company unchanged.
- Every token the library issues (password, Google / Apple / Facebook, e-mail confirmation, operator
  "login as", the refresh in `PUT /MyAccount`) names a company the user belongs to at that moment. A
  stored company the user was removed from falls back to the oldest company of the user, and the
  account is corrected.

A token issued earlier keeps its claim until it expires, so an application that has to react to a
removed membership immediately still needs its own check.

### Restricted tokens

`PUT /MyAccount` answers with a regular login token of the token subject. An application that issues
its own tokens with narrowed-down rights (a kiosk session, a device token, ...) must mark them with the
`TokenPurpose` claim - regular login tokens must not carry it. `PUT /MyAccount` refuses such a token
with 403: it neither changes the account nor exchanges the token for a login token.

Tokens marked in another way are added in the application:

```csharp
services.Configure<RestrictedTokenOptions>(o => o.ClaimTypes.Add("MyClaimType"));
```

## Roles and permissions

A role is a name plus a set of permissions; applications and libraries declare their permissions in
code and protect endpoints with `[HasPermission]`. See [docs/PERMISSIONS.md](docs/PERMISSIONS.md).
