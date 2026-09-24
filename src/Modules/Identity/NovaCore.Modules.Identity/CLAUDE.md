# CLAUDE.md — Identity / security

Subtree rules for the Identity module. This is the security boundary — changes here are high-risk.
Follow `src/Modules/CLAUDE.md` for module mechanics; the rules below are about **not weakening security**.
Design is grounded in the OWASP Authentication / Password Storage / OAuth2 cheat sheets (see `README.md`).

## Invariants — do not regress these

- **`AddIdentityCore`, never `AddIdentity`.** The API authenticates with JWT bearer; `AddIdentity` would
  register cookie schemes and hijack the default scheme.
- **Hardening (keep these values or stronger):** PBKDF2 `IterationCount = 600_000`; password length-first
  (`RequiredLength = 12`, no character-composition rules); lockout 5 attempts / 15 min;
  `RequireConfirmedEmail = true`; `RequireUniqueEmail = true`.
- **Tokens:** access JWT is short-lived (15 min). Refresh tokens are 256-bit, stored **SHA-256 hashed**
  (never plaintext), **rotated on every use**, with **reuse detection that revokes the whole family**.
  All of this is `AuthTokenService` — change it carefully and keep the family-revoke-on-reuse behavior.
- **Anti-enumeration:** login failures return a uniform **401 "Invalid credentials"** regardless of cause
  (unknown user / bad password / lockout). Registration returns the same generic message whether or not
  the email already exists. Don't add responses that leak account existence.
- **Claims contract:** issue/validate the raw names in `NovaClaims` (`sub`/`name`/`email`/`role`/`permission`)
  with `MapInboundClaims = false`. The `Administrator` role is the super-admin and bypasses permission checks
  (`HttpContextCurrentUser.SuperAdminRole`).
- **Audit, don't print secrets.** Route security events through `ISecurityAuditLogger` (it masks emails).
  Never log passwords, raw refresh tokens, or full JWTs.

## When adding an auth endpoint

- Map it under the `/api/v1/auth` group (already rate-limited with the `"strict"` policy). Use
  `.AllowAnonymous()` only for pre-login endpoints (register/confirm/login/refresh); everything else is
  `.RequireAuthorization()` or `.RequirePermission(IdentityPermissions.X)`.
- Validate DTOs with `.AddEndpointFilter<ValidationFilter<Request>>()`.
- Seeding (`IdentityDataSeeder`) collects permissions from **all** modules' `IPermissionDefinitionProvider`s:
  `Administrator` gets everything; `User` gets non-Identity `:read` permissions (least privilege).

## Production checklist (must be true before shipping)

- `Jwt:SigningKey` set from user-secrets/env (≥ 32 bytes) — the dev fallback only exists in Development.
- A real `IEmailSender` is registered by the host. `LoggingEmailSender` (logs the confirmation link) is wired only in
  Development; anywhere else `EmailSenderStartupCheck` stops startup if no `IEmailSender` is registered.
- TLS/HTTPS enforced at the edge (HSTS is wired for non-dev).
- Admin seed password supplied via user-secrets, never committed.
