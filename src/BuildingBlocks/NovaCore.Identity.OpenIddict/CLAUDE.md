# CLAUDE.md — NovaCore.Identity.OpenIddict (the identity provider)

Subtree rules for the identity provider add-on. This is the security boundary: changes here are high-risk. It replaced
the `NovaCore.Modules.Identity` module in phase 3b (DECISIONS D-025…D-029); these are that module's invariants, carried
over, plus the provider's own. Design sources: OWASP Authentication / Password Storage / OAuth2 cheat sheets, RFC 9700
(OAuth 2.0 Security BCP), RFC 9068 (JWT access tokens), OpenIddict's docs and samples (RESEARCH R-059).

## Shape

- `builder.AddCoreIdentityServer()` + `app.MapCoreIdentityServer()`. The host registers `IdentityServerDbContext` with its
  provider (`AddModuleDbContext`, `MigrationsAssembly`) and owns the migrations (R-060); the package ships none.
- The core validates this provider's access tokens like any provider's: `Jwt:Authority` is this host, the issuer is set
  from it, discovery + JWKS over HTTP (to itself). Access tokens are signed, **unencrypted** JWTs (`DisableAccessTokenEncryption`,
  D-026 (3)); don't switch to OpenIddict.Validation or encrypted tokens without reopening D-026.
- Keys: ephemeral in Development only. Anywhere else the host adds certificates with OpenIddict's own API; `ValidateOnStart`
  stops the host without them. Don't wrap OpenIddict's key API (Y-15).
- Tested on `tests/.../IdentityServer/IdentityServerTestHost` (core + this add-on only) and, for the Api's wiring, on
  `IdentityServerWiringTests`.

## Invariants — do not regress these

- **`AddIdentityCore`, never `AddIdentity`/`AddDefaultIdentity`.** The API's default scheme is JWT bearer. The provider's
  sign-in session uses Identity's cookies added with the parameterless `AddAuthentication()` (R-061); only `/connect/*`
  and `/account/*` read them.
- **Hardening (keep these values or stronger):** PBKDF2 `IterationCount = 600_000`; password length-first
  (`RequiredLength = 12`, no composition rules); lockout 5 attempts / 15 min, new accounts included; a wrong 2FA code
  counts as a failed access, a recovery code included (`SignInManager` neither checks lockout nor counts a wrong
  recovery code, so `LoginPage` does both before and after redeeming it, R-085); `RequireConfirmedEmail = true`;
  `RequireUniqueEmail = true`.
- **An authenticator code works once** (ASVS 6.5.1): `SingleUseAuthenticatorTokenProvider` replaces Identity's
  `Authenticator` provider, lets Identity verify the code (±2 steps), then refuses a code accepted in the last five
  steps and records the new one in a user token. The save carries the user's concurrency stamp, so two requests racing
  with one code let one in. The codes are compared as numbers, like Identity does (`012345` is `12345`). Every TOTP
  check (sign-in, enable, disable) goes through `UserManager.VerifyTwoFactorTokenAsync`, never around it (D-053 (2)).
- **Recovery codes are stored hashed** (ASVS 6.5.2): `HashedRecoveryCodesUserStore` (Identity's EF user store, registered
  before `AddEntityFrameworkStores`) keeps them as `PasswordHasher` hashes with its defaults (PBKDF2-HMAC-SHA512,
  100,000 iterations; not the host's 600,000, since a sign-in tries up to ten) under its own token name. Identity's
  plain-text slot is never read (D-053 (3)).
- **Protocol:** Authorization Code with PKCE **S256 only** (`plain` removed, D-029 (1)); refresh tokens rotate, and a
  redeemed one reused after OpenIddict's 30 s leeway revokes every token of its authorization; access tokens 15 minutes,
  refresh tokens 7 days. The token endpoint re-checks the account on every refresh (exists, can sign in, not locked out)
  and rewrites the roles.
- **Anti-enumeration:** the sign-in page shows one message for an unknown account, a wrong password and a locked
  account; the "confirm your email" message comes only after the right password. `SignInManager` answers `NotAllowed`
  before it looks at the password, so `LoginPage` checks the password itself and counts a wrong one (D-030 (1)).
  An unknown email costs a password hash like a known one (`DummyPasswordHash`, made once by the host's hasher), so the
  answer's timing doesn't reveal accounts either (R-087). Registration answers an existing address like a new one. Resending the confirmation answers every address alike and
  emails only an unconfirmed account. The password reset form answers every address alike and emails only a confirmed
  account; the reset page answers an unknown or unconfirmed account like a bad token (R-084).
- **Password reset:** the link carries an Identity token that `ResetPasswordAsync` uses up (it renews the security
  stamp), the new password is typed twice, and a reset revokes every token and authorization of the user like a
  password change. It never signs the user in. A missing `User` role (seeding off) leaves a new account without a role
  instead of failing the registration.
- **Sign-in age:** `auth_time` and `max_age` read the sign-in time the cookie's `OnSigningIn` records in its
  properties, never the cookie's `IssuedUtc`, which moves on every renewal (sliding expiration, the zero-interval stamp
  check; D-030 (2), R-067).
- **Two-factor keys stay secret once enabled:** `/2fa/setup` answers 409 while 2FA is on; a new authenticator starts
  after `/2fa/disable`, which takes a code (D-030 (3)).
- **The second factor changes only with the password** (ASVS 7.5.1, D-053 (4)): `/2fa/setup` takes `{ password }`,
  `/2fa/enable` and `/2fa/disable` take `{ password, code }`. A locked account is refused; a wrong password or code counts
  toward lockout and only full success resets the count, as on the sign-in page. `/2fa/enable` while 2FA is on returns
  new recovery codes (no endpoint of its own). Turning 2FA on or off revokes every refresh token and authorization of
  the user. No `amr`/`acr` or step-up in the token: permissions follow the person on the server, not the token.
- **Claims contract:** access tokens carry `sub`, `name`, `email`, `role` (the names in `NovaClaims`). **Never
  permissions, never the security stamp:** the core resolves permissions per request (`IdentityPermissionResolver`: role
  + direct `permission` claims for a Guid `sub`, every declared permission for an `Administrator` member with
  two-factor authentication on (without it the role gives nothing, D-053 (5); turning 2FA on or off drops the cache),
  `Identity:Clients:n:Permissions` for a service client's id). The core has no super-admin role: the token's `role`
  claim is informational and never passes a permission check (D-036 (2)). The core's
  claims transformation runs on OpenIddict's scheme too, so a principal copied into a token drops `permission` claims
  (D-029 (3)).
- **Role changes (F6b.2, D-053 (6)):** the admin API drops the user's cached permissions
  (`HybridCache.RemovePermissionsAsync`, a tag: the stamp in the cache key is unknown here) and revokes their refresh
  tokens and authorizations on every grant or removal. Granting or removing `Administrator` (compared normalized, so
  `administrator` is the same role) also needs `identity:roles:manage`: `identity:users:manage` alone would let its
  holder make themselves an administrator. The last member keeps the role (409). Anything else that changes what the
  resolver returns for a user must drop the cache the same way (the 2FA endpoints do).
- **The store is pruned:** `ExpiredTokenPruner` (a `BackgroundService`, a pass at startup, then hourly) calls
  OpenIddict's `PruneAsync` for tokens, then authorizations, with a 14-day minimum lifespan (OpenIddict.Quartz's defaults,
  R-088). Keep the lifespan longer than the refresh token lifetime, so a reused refresh token is still found and revokes
  its chain. A failed step is logged (a failed token step doesn't skip the authorizations) and the next pass tries
  again; it never stops the host.
- **Sign-out asks first:** `/connect/endsession` shows a signed-in browser a question whose form posts the request's
  parameters back with an antiforgery token; only that POST signs out (logout CSRF, RP-Initiated Logout §2, R-086). A
  signed-out browser goes straight on to the post-logout redirect.
- **Anonymous by declaration:** the core's fallback policy closes every endpoint without authorization metadata, so the
  protocol endpoints (`ConnectEndpoints`) and the sign-in pages (`/account` group) carry `.AllowAnonymous()`; the
  handlers read the sign-in session themselves. OpenIddict's own endpoints (discovery, JWKS, revocation) answer in the
  authentication middleware, before authorization.
- **Pages and forms:** plain HTML (the core's CSP forbids scripts and styles), rendered by `HtmlPage`; antiforgery
  checked in the handler with `IAntiforgery.IsRequestValidAsync` (the pipeline has no antiforgery middleware, R-061),
  redirects to local URLs only. The `/account` group (anonymous, out of the OpenAPI document, `strict` rate limit) is
  created once in `MapCoreIdentityServer`.
- **Links from configuration, never from the request:** the emailed links (confirmation, password reset) come from
  `EmailLinks`, which starts them from `Jwt:Authority`, not the Host header.
- **Validated request bodies are public records** (`AccountRequests.cs`): .NET 10's validation generator skips internal
  types (R-062). Endpoint classes stay internal.
- **Audit, don't print secrets.** Security events go through `SecurityAuditLogger` (masked emails, `SECURITY` prefix).
  Never log passwords, codes, raw tokens or full JWTs.
- **Password change and "sign out everywhere"** revoke every token and authorization of the user and renew the security
  stamp; the cookie's stamp is checked on every use (`ValidationInterval = 0`), so the provider session ends at once.
  Issued access tokens stay valid until they expire. A wrong current password counts toward lockout and a locked
  account's password doesn't change, as on the sign-in page: a stolen access token mustn't guess more often (R-087).

## Adding an account endpoint

- JSON API: map it in `AccountEndpoints` under `/api/v1/auth` (the `strict` rate limit). The handler returns typed
  results (`Results<Ok<T>, ValidationProblem, UnauthorizedHttpResult>`, ...), with named internal records for bodies
  (`MessageResponse`), not anonymous objects: the union is the OpenAPI contract. `/connect/*` and `/account/*` stay
  `IResult` (protocol and HTML, excluded from the document). `.AllowAnonymous()` only for
  pre-sign-in endpoints (register, confirm-email, resend-confirmation-email); everything else `.RequireAuthorization()` or
  `.RequirePermission(IdentityPermissions.X)`. Put the validated body in `AccountRequests.cs` and list it in
  `PublicAPI.Unshipped.txt`.
- Add the endpoint to the Api host's `ModuleRegistrationTests` and, when it validates, `ValidationTests`.

## Production checklist

- `Jwt:Authority` is the host's public https URL; the signing and encryption certificates are added by the host.
- A real `IEmailSender` is registered (`LoggingEmailSender` writes confirmation links to the log: Development only;
  `EmailSenderStartupCheck` stops the host otherwise).
- A Data Protection key store the host chose (`PersistKeysTo*`, which sets `KeyManagementOptions.XmlRepository`): the
  cookies, antiforgery tokens and confirmation links depend on it. Outside Development `ValidateOnStart` stops the host
  without one (D-036 (6)); the framework's fallback (user profile or memory) doesn't survive a replaced container.
- Client secrets and the seed administrator's password come from user secrets or the environment, never appsettings.
- TLS at the edge; OpenIddict refuses plain HTTP.
