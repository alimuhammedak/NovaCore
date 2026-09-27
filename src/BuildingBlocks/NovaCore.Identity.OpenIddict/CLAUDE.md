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
  counts as a failed access; `RequireConfirmedEmail = true`; `RequireUniqueEmail = true`.
- **Protocol:** Authorization Code with PKCE **S256 only** (`plain` removed, D-029 (1)); refresh tokens rotate, and a
  redeemed one reused after OpenIddict's 30 s leeway revokes every token of its authorization; access tokens 15 minutes,
  refresh tokens 7 days. The token endpoint re-checks the account on every refresh (exists, can sign in, not locked out)
  and rewrites the roles.
- **Anti-enumeration:** the sign-in page shows one message for an unknown account, a wrong password and a locked
  account; the "confirm your email" message comes only after the right password. `SignInManager` answers `NotAllowed`
  before it looks at the password, so `LoginPage` checks the password itself and counts a wrong one (D-030 (1)).
  Registration answers an existing address like a new one.
- **Sign-in age:** `auth_time` and `max_age` read the sign-in time the cookie's `OnSigningIn` records in its
  properties, never the cookie's `IssuedUtc`, which moves on every renewal (sliding expiration, the zero-interval stamp
  check; D-030 (2), R-067).
- **Two-factor keys stay secret once enabled:** `/2fa/setup` answers 409 while 2FA is on; a new authenticator starts
  after `/2fa/disable`, which takes a code (D-030 (3)).
- **Claims contract:** access tokens carry `sub`, `name`, `email`, `role` (the names in `NovaClaims`). **Never
  permissions, never the security stamp:** the core resolves permissions per request (`IdentityPermissionResolver`: role
  + direct `permission` claims for a Guid `sub`, `Identity:Clients:n:Permissions` for a service client's id). The core's
  claims transformation runs on OpenIddict's scheme too, so a principal copied into a token drops `permission` claims
  (D-029 (3)).
- **Pages and forms:** plain HTML (the core's CSP forbids scripts and styles), antiforgery checked in the handler with
  `IAntiforgery.IsRequestValidAsync` (the pipeline has no antiforgery middleware, R-061), redirects to local URLs only.
- **Links from configuration, never from the request:** the email confirmation link starts from `Jwt:Authority`, not the
  Host header.
- **Validated request bodies are public records** (`AccountRequests.cs`): .NET 10's validation generator skips internal
  types (R-062). Endpoint classes stay internal.
- **Audit, don't print secrets.** Security events go through `SecurityAuditLogger` (masked emails, `SECURITY` prefix).
  Never log passwords, codes, raw tokens or full JWTs.
- **Password change and "sign out everywhere"** revoke every token and authorization of the user and renew the security
  stamp; the cookie's stamp is checked on every use (`ValidationInterval = 0`), so the provider session ends at once.
  Issued access tokens stay valid until they expire.

## Adding an account endpoint

- JSON API: map it in `AccountEndpoints` under `/api/v1/auth` (the `strict` rate limit). `.AllowAnonymous()` only for
  pre-sign-in endpoints (register, confirm-email); everything else `.RequireAuthorization()` or
  `.RequirePermission(IdentityPermissions.X)`. Put the validated body in `AccountRequests.cs` and list it in
  `PublicAPI.Unshipped.txt`.
- Add the endpoint to the Api host's `ModuleRegistrationTests` and, when it validates, `ValidationTests`.

## Production checklist

- `Jwt:Authority` is the host's public https URL; the signing and encryption certificates are added by the host.
- A real `IEmailSender` is registered (`LoggingEmailSender` writes confirmation links to the log: Development only;
  `EmailSenderStartupCheck` stops the host otherwise).
- Client secrets and the seed administrator's password come from user secrets or the environment, never appsettings.
- TLS at the edge; OpenIddict refuses plain HTTP.
