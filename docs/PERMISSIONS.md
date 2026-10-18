# Roles and permissions (ITBees.UserManager)

Guide for developers and AI agents. Read it before adding an endpoint that an employee with a
limited role may call, or before adding a new permission.

## Model - what lives in code and what in the database

| Concept | Where | Managed by |
|---|---|---|
| **Permission key**, e.g. `octopark.barrier.open` | code (constants + `IPermissionCatalogContributor`) | developer - the key is referenced by `[HasPermission]` and by the panels; a key in use is never renamed |
| **How a key is shown** (name, group, description, order) | table `Permission` | seeded by the migration that adds the key; the admin panel only shows it ("Katalog uprawnień") |
| **Role** | `AspNetRoles` (`FasIdentityRole`) | admin panel ("Role i uprawnienia"); the roles an application relies on are inserted by its migration |
| **Role = name + permissions** | table `RolePermission (RoleId, PermissionKey)`; `*` = full access (also future keys) | seeded by a migration, edited in the admin panel |
| **Assignment** | `UsersInCompany.IdentityRoleId` | a user has a role **in a company** (unchanged) |

Effective permissions of the current user in company C (`IPermissionService`), always resolved on the
server for a request (the keys are never read from the JWT - only the global `PlatformOperator` role is,
like everywhere else in the UserManager):

1. global Identity role `PlatformOperator` -> every key;
2. not a member of C (`UsersInCompany`) -> nothing;
3. owner of C (`Company.OwnerGuid`) -> every key;
4. otherwise the keys stored for the user's role(s) in C (`*` = every key; a role named `PlatformOperator`
   is never restricted); keys no longer declared in code are ignored; a role without rows holds nothing.

Every key means every key, also one that no contributor declares (a typo in `[HasPermission]`, a contributor
that is not registered): such a key is logged as an error and refused to every ordinary role, but full access
still holds it, so a mistake does not lock the platform operator out of the application.

Role permissions are cached for 60 s per process; a change made in AdminApi reaches another API process
within that time.

## Using it in an application

### 1. Declare the keys

```csharp
public static class MyAppPermissions
{
    public static class Barrier
    {
        public const string Open = "myapp.barrier.open";
    }
}

public class MyAppPermissionCatalog : IPermissionCatalogContributor
{
    // Every public const string of the class (and its nested classes) - no second list to keep in sync.
    public IEnumerable<string> GetPermissionKeys() => PermissionKeys.FromConstants(typeof(MyAppPermissions));
}

// DI (after FasDependencyRegistration.Register):
services.AddSingleton<IPermissionCatalogContributor, MyAppPermissionCatalog>();
```

A library that owns endpoints registers its own contributor the same way - all contributors form one
catalog. Duplicate or badly formatted keys stop the application when the catalog is first used.

### 2. Database and seed

`DbModelBuilder.Register(modelBuilder)` already configures `Permission` and `RolePermission`. An
application that configures the UserManager entities itself calls
`ITBees.UserManager.Services.DbModelBuilder.RegisterPermissions(modelBuilder)` in `OnModelCreating`.

The migration that adds keys also seeds their data, so deploying it does not change behaviour:

- `Permission` rows (key, name, group, sort order) - use **literals**, not the constants;
- the roles the application relies on (`INSERT ... WHERE NOT EXISTS` on `NormalizedName`);
- `RolePermission` rows for the roles that should hold the new key
  (`INSERT INTO RolePermission (RoleId, PermissionKey) SELECT Id, 'key' FROM AspNetRoles WHERE NormalizedName IN (...)`).

Example: Octopark `Octopark.Bl/Migrations/20261015120749_AddRolePermissions.cs`. A key declared in code
without a row is still assignable in the panel (listed under "Other" with the key as its name) - add the
row in a migration.

Role names are matched by `UPPER(Name)`: roles created by older versions of `POST /UserRole` have no
`NormalizedName`, and the same name may exist twice. The Octopark migration merges such duplicates and
fills `NormalizedName`; `POST /UserRole` now sets it.

### 3. Protect endpoints

```csharp
[Authorize]
[HasPermission(MyAppPermissions.Barrier.Open)]           // any of the listed keys
public class OpenGateController : RestfulControllerBase<OpenGateController> { ... }
```

`[HasPermission]` is checked twice:

1. before the action - the user must hold the permission in **at least one** of their companies;
2. by **every company access check** made while the request runs
   (`IAspCurrentUserService.GetMyAcceessToCompany`, so `TryCanIDoForCompany` too, and app wrappers that call
   `IPermissionService.CheckEndpointPermission`) - the user must hold it in **that** company.

So the service keeps doing what it already does - load the entity and check access to the entity's
company - and the permission is checked against the right company automatically. An action-level
attribute replaces the controller-level one (use it when e.g. GET is open to everyone and PUT is not).

An endpoint that declares `[HasPermission]` and reaches a company check in an application where the
permission services are not registered throws instead of passing the check: a missing registration must not
silently turn the per-company check off. `[HasPermission]` and `CheckAnyPermission` need at least one key -
an empty list would allow everything while looking like a guard, so it is refused.

For a check inside a service that must always apply, call
`_permissionService.RequirePermission(key, companyGuid)` (or `HasPermission`).

When the permission depends on the request rather than on the endpoint (e.g. a list filter that also shows
closed sessions), call `_permissionService.CheckAnyPermission(companyGuid, keyA, keyB)` - it behaves like
`[HasPermission]`.

### 4. Denials

Every denial answers 403 and is logged as a warning
(`Permission denied: user ..., company ..., required any of [...], endpoint ...`), so a role missing a grant
shows up in the logs. There is no switch that only logs: the starting grants of a migration must reproduce what
each role could do before.

### 5. Frontend

`GET /MyPermissions?companyGuid=` returns the effective keys of the current user in the company (empty
for a non-member). Panels only **hide** what the user may not use; the API decides.

### 6. Granting roles

A user may give someone else only a role that holds nothing they do not hold themselves in that company
(a full-access role needs full access, the `PlatformOperator` role a platform operator). Without this rule an
employee allowed to add employees could hand out any role, including a full-access one.

- Before a role is granted by a user (invitation, membership change), call
  `_permissionService.CheckRoleGrant(role, companyGuid)` - a denial is logged and answered 403.
- A role picker offers only the roles for which `CanGrantRole(role, companyGuid)` is true.
- Server-to-server flows (no logged-in user) and the platform operator's admin panel do not need it.

### 7. Inviting people to a company

`INewUserRegistrationService.CreateAndInviteNewUserToCompany` (and `POST /NewInvitedUserRegistration`, which
calls it) checks two things before it creates the account or the invitation:

1. **Who invites** - with the default security service (no `IExternalSecurityService` passed),
   `IAccessControlService.CanDo` allows the platform operator, the owner of the target company and its members,
   and refuses everybody else with 403. Up to 8.0.411 it allowed every logged-in user, to any company.
2. **Which role** - when `UserRoleGuid` is set and a user is logged in, whatever security service was passed:
   the platform operator gives any role, the owner any role but `PlatformOperator`, any other member only a role
   passing `CheckRoleGrant` (section 6). An unknown `UserRoleGuid` is answered 400.
   Without a logged-in user (a background job, a server-to-server flow with its own `IExternalSecurityService`)
   the role is not checked - the caller vouches for it.

The owner here is `CompanyOwnership.IsOwner`: `Company.OwnerGuid`, or `CreatedByGuid` of a company without it -
**member of the company or not**, because Kilometrówka creates companies without adding the owner to
`UsersInCompany`. Both checks above use it, so an owner who may invite may also give the roles. (`IPermissionService`
is unchanged: there the owner holds every key only as a member, and only through `OwnerGuid`.)

An application with a stricter rule (e.g. only members holding its "team" key may invite) registers its own
`IAccessControlService` after `FasDependencyRegistration.Register`, or checks the key in its own endpoint and
passes its own `IExternalSecurityService`.

`GET /AcceptInvitation?companyGuid&email` needs a logged-in user and accepts only **their own** invitation to
the company (`UsersInvitationsToCompanies.UserAccountGuid` = the logged-in user). The link of the invitation
e-mail holds nothing secret (the e-mail address and the company), so up to 8.0.411 anyone who knew both could
accept the invitation for that person, without logging in. `email`, when given, must be the logged-in user's
e-mail - otherwise nothing is accepted and `message` says which account to log in with. The page behind the
link (fas-auth `auth/acceptInvitationExistingUser`) has to log the user in first and call it with their token.
Without `accountEmailActivationBaseLink` the link points at the API itself (`DefaultApiUrl`), which a click in
the e-mail cannot accept any more - pass the address of that page.
A new user accepts all their invitations by setting the password (`POST /AcceptAccount`, which checks the
password-reset token from the e-mail).

An account that exists but has no confirmed e-mail (invited before, never set the password) cannot log in to
accept, so `CreateAndInviteNewUserToCompany` sends it the "set the password" e-mail again, like to a new user
(`ComposeEmailWithUserCreationAndInvitationToOrganization`, fresh tokens); setting the password accepts the
invitations. Both e-mails use the same `accountEmailActivationBaseLink`: a new or unconfirmed account gets
`{link}?emailInvitation=true&token=...` (the fas-auth page `auth/acceptInvitation`), a registered one
`{link}/acceptInvitation?...` (`auth/acceptInvitationExistingUser`) - an application that knows which one it
invites passes the matching page; with `SendEmailInvitation = false` it sends its own e-mail.

## Endpoints

| Route | Access | Purpose |
|---|---|---|
| `GET /MyPermissions?companyGuid` | any logged-in user | own permissions in a company (last used company when omitted) |
| `GET /PermissionCatalog` | PlatformOperator | keys declared in code, grouped and named from the database, with the roles holding them |
| `GET /UserRolePermissions?roleGuid` | PlatformOperator | permissions of a role (`fullAccess` or `permissionKeys`) |
| `PUT /UserRolePermissions` | PlatformOperator | save them (unknown keys -> 400; `*` only through `fullAccess`; only the difference is written, so a failed save never clears the role) |
| `POST /UserRole?roleName` | PlatformOperator | create a role (sets `NormalizedName`; a name already used is refused) |
| `GET /UserRole?roleGuid` | PlatformOperator | a role with `assignedUsersCount` (company memberships, invitations - applied ones too, because their row keeps referencing the role - and global roles) |
| `DELETE /UserRole?roleGuid&replacementRoleGuid` | PlatformOperator | delete a role; when someone holds it, `replacementRoleGuid` is required and they get that role (never `PlatformOperator`); a user already holding the replacement in that company keeps one membership |
| `POST /NewInvitedUserRegistration` | PlatformOperator, owner or member of `companyGuid` | invite an e-mail to the company; a member gives only a role passing `CheckRoleGrant`, the owner any but `PlatformOperator` (section 7) |
| `GET /AcceptInvitation?companyGuid&email` | the invited user, logged in | accept their own invitation to the company (section 7) |

Applications with an explicit controller list (Octopark `ControllersRegistration.cs`) must add these
controllers there. The `PlatformOperator` role cannot be restricted. Every change is logged with the
operator's e-mail and the keys before and after.

## Rules for agents

- Never compare role names in code (`CurrentUserIsInRole("Worker")`, `UserRoles.Contains("Osk")`) to
  decide what an employee may do - declare a key and check it.
- Never decide authorization on a `CompanyGuid` taken from a request model; load the entity and use its
  company. `[HasPermission]` is checked in a company only when the service makes that company check - an
  endpoint whose service never calls it is open to anyone holding the key in any of their companies.
- Granting a role from a user's request goes through `CheckRoleGrant`.
- Never accept an invitation (or anything else that makes someone a member) on data from a link that is not
  secret - the e-mail and the company are not; tie it to the logged-in user or to a token checked on the server.
- Never check permissions only in the frontend.
- New permission: constant in the permissions class + the frontend mirror of the key + a migration that
  inserts its `Permission` row and grants it to the roles that should keep their current abilities.
- Never put names or role grants in code; they are data (migration + admin panel).
- Never rename or reuse a key that is in use; add a new key instead.
