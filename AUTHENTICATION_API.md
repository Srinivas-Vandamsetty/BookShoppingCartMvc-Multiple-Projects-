# Authentication API

The active host is **BookShoppingCart.WebAPI** (.NET 9). It uses the existing ASP.NET Core Identity users and roles in **BookShoppingCartMvc**. MVC login now calls these APIs through a server-side authentication bridge; the browser receives an Identity cookie containing a session reference. Public catalog reads remain anonymous; genre writes require an Admin Bearer token. The earlier X-Admin-Api-Key mechanism has been replaced.

## Endpoints

| Method / endpoint | Authentication | Result |
|---|---|---|
| POST /api/auth/login | HTTP Basic over HTTPS | 200: access and refresh tokens |
| POST /api/auth/refresh | Refresh token in JSON body | 200: new access and refresh tokens |
| POST /api/auth/logout | JWT Bearer | 204: current session revoked |

Login accepts an email address or username and the Identity password. Supply Authorization: Basic followed by base64(UTF-8(username:password)). Base64 is encoding, not encryption. Requests to /api/auth over HTTP are rejected with 400 without redirecting credentials. Confirmed email is required; existing Identity lockout is enforced (five failed attempts, fifteen minutes). Two-factor-enabled users are rejected until a complete MFA API is implemented; this flow never bypasses MFA. Login failures use a generic 401 response.

Example requests (replace placeholders locally; do not commit credentials):

~~~http
POST https://localhost:<https-port>/api/auth/login
Authorization: Basic <base64-encoded-username-colon-password>

POST https://localhost:<https-port>/api/auth/refresh
Content-Type: application/json

{"refreshToken":"<current-refresh-token>"}

POST https://localhost:<https-port>/api/auth/logout
Authorization: Bearer <access-token>
~~~

Login and refresh return the same JSON shape:

~~~json
{
  "accessToken": "<signed-JWT>",
  "refreshToken": "<opaque-random-token>",
  "accessTokenExpiresAtUtc": "<UTC timestamp>",
  "refreshTokenExpiresAtUtc": "<UTC timestamp>",
  "tokenType": "Bearer"
}
~~~

Use Authorization: Bearer <access-token> on secured API calls. Basic credentials authorize only login. Invalid/expired/revoked tokens return 401; an authenticated user without Admin permission receives 403. Invalid request bodies return 400, throttling returns 429 with Retry-After, and database unavailability returns a sanitized 503. Authentication responses are marked no-store. Do not log Authorization headers or refresh request/response bodies, including at gateways.

## Expiration, rotation and revocation

Defaults are fifteen-minute access tokens, seven-day refresh tokens, and a thirty-day absolute session lifetime. Refresh expiry is capped at the absolute session deadline. Each successful refresh consumes the supplied token and returns a replacement. **Store the replacement atomically and serialize refresh requests per session.** Reusing a consumed token, including two concurrent requests with the same token, revokes the entire session family. A lost refresh response may therefore require login again.

Logout revokes the current login session, including its access token and every refresh descendant. Other devices/sessions remain signed in. Every authenticated request checks the durable session, current Identity security stamp, account lockout/confirmation/MFA status, and current roles. Password/security-stamp changes invalidate existing sessions; role changes invalidate old access tokens and refresh obtains current roles. Logout requires a valid access token; if it expired, refresh first. Expired refresh credentials require login again.

Refresh tokens contain 64 cryptographically random bytes. The API refresh-token table stores only SHA-256 hashes. For browser sessions the MVC bridge additionally stores a Data Protection-encrypted token pair in ApiSessions.ProtectedBrowserTokens; plaintext tokens are never stored in the database. Consumed tokens are retained for reuse detection until their session expires; a background job deletes sessions that expired more than one day ago and cascades token cleanup. SQL Server transactions and per-session application locks serialize refresh/logout across API replicas. No in-process revocation cache is used, so healthy database access is required for protected requests.

For native clients use operating-system secure credential storage. Browser integrations need a deliberate token-storage strategy: avoid persistent JavaScript-accessible refresh-token storage; a server-side BFF or separately designed secure HttpOnly-cookie flow is preferable. This API currently returns tokens in JSON and does not establish an authentication cookie.

## Configuration and deployment

Apply migration **20261105102022_AddApiAuthenticationSessions** before starting the updated API. It adds ApiSessions and ApiRefreshTokens with foreign keys to existing Identity users; it does not change user passwords, roles, or confirmation status. Keep MVC/API pointed at the same intended database. Use the normal controlled EF migration process; startup does not silently migrate.

| Environment setting | Purpose / default |
|---|---|
| Jwt__Issuer | Exact trusted issuer; default https://bookshoppingcart.local |
| Jwt__Audience | Exact API audience; default book-shopping-cart-api |
| Jwt__SigningKey | Required secret: base64 encoding of at least 32 cryptographically random bytes |
| Jwt__PreviousSigningKeys__0 (and __1) | Optional previous signing secrets during a bounded key rotation |
| Jwt__AccessTokenMinutes | 1–30; default 15 |
| Jwt__RefreshTokenDays | 1–30; default 7 |
| Jwt__SessionDays | 1–90 and at least refresh lifetime; default 30 |
| RateLimits__AuthenticationPerMinute | Per-process, per-IP aggregate auth quota; default 20 |

The local development signing key is stored in .NET User Secrets for BookShoppingCart.WebAPI, never in appsettings or source. Provision separate production secrets through the deployment secret manager. An operator can generate a key in PowerShell with [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64)) and set it directly in the secret store without committing or logging it. Startup rejects missing/invalid signing configuration. All replicas must use the same issuer, audience and signing keys. Symmetric key holders can issue tokens; restrict key access to this trusted API boundary.

JWT validation requires HS256, at+jwt type, signature, exact issuer/audience, expiration, session ID and token ID; clock skew is thirty seconds. During rotation issue with the new SigningKey, temporarily retain the old value in PreviousSigningKeys, and remove it after the longest outstanding access-token lifetime plus skew. A compromised key requires immediate removal and session revocation according to the incident response policy. Server clocks must be synchronized.

Terminate TLS securely. If a trusted reverse proxy forwards HTTPS, configure ASP.NET Core forwarded headers with explicit trusted proxies/networks before HTTPS/auth middleware; do not trust arbitrary forwarded headers. For multiple replicas, add a shared gateway throttle in addition to the per-process limiter. Do not enable permissive credentialed CORS.

## Verification

HTTP integration tests cover Basic parsing, account confirmation/MFA/lockout, JWT signature/issuer/audience/type/algorithm/session validation, authorization, refresh expiration/rotation/reuse, logout isolation/revocation, role/security-stamp changes, HTTPS enforcement and throttling. SQL-specific tests exercise migrations and concurrent refresh reuse in a disposable BookShoppingCartTests_<GUID> database.

Design references: [Microsoft JWT Bearer validation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication) and [OAuth security best practices: refresh-token protection](https://www.rfc-editor.org/info/rfc9700/). These endpoints are a first-party Identity credential exchange, not a full OAuth/OIDC authorization server.


## Implementation map and validation record

| Files | Changes |
|---|---|
| BookShoppingCart.WebAPI/Controllers/AuthenticationController.cs | Login, refresh and logout routes, body validation, typed OpenAPI responses and cache prevention |
| BookShoppingCart.WebAPI/Security/BasicLoginHandler.cs | HTTPS Basic credential parsing and Identity sign-in/lockout checks |
| BookShoppingCart.WebAPI/Security/JwtOptions.cs; JwtAccessTokenIssuer.cs | Startup configuration validation, signing and strict Bearer validation parameters |
| BookShoppingCart.WebAPI/Security/AuthenticationCleanupService.cs | Scoped, cancellable expired-session cleanup |
| BookShoppingCart.WebAPI/Program.cs; appsettings.json; BookShoppingCart.WebAPI.csproj | Identity/DI/JWT/rate-limit/OpenAPI wiring, non-secret settings and .NET 9 JwtBearer package |
| BookShoppingCart.Business/Services/IApiAuthenticationService.cs; ApiAuthenticationService.cs | Identity session lifecycle, token issuance/rotation and revocation checks |
| BookShoppingCart.Data/Repositories/IAuthSessionRepository.cs; AuthSessionRepository.cs | Indexed token lookup, transactional refresh rotation and distributed SQL session locking |
| BookShoppingCart.Models/Models/ApiSession.cs; Models/DTOs/AuthTokens.cs | Durable session/token entities and response contracts |
| BookShoppingCart.Data/Data/ApplicationDbContext.cs; Migrations/20261105102022_AddApiAuthenticationSessions*.cs; ApplicationDbContextModelSnapshot.cs | Entity mapping, additive migration and model snapshot |
| BookShoppingCart.Tests/Tests/AuthenticationApiTests.cs; AuthenticationSqlTests.cs; HttpBoundaryTests.cs; SqlServerIntegrationTests.cs | New authentication/security/concurrency coverage and updated Bearer/migration boundaries |
| README.md; deployment/commerce-migration.sql; deployment/MIGRATION_EXECUTION.md | Configuration, deployment SQL and verified database upgrade record |

Validation completed 5 November 2026: the nine-project Release solution build passed with zero warnings/errors; all **114 tests passed**, zero failures/skips, including disposable SQL Express tests. The new migration is applied to BookShoppingCartMvc on COGNINE-L218\SQLEXPRESS01 after a verified backup. Existing application counts and database constraints were preserved. A real local API startup using Development User Secrets also succeeded; Swagger exposed all three authentication routes, and a credential-free HTTP login request returned 400 with Cache-Control: no-store. The temporary smoke-test process was stopped afterward. The frontend integration described below supersedes the original separate MVC login flow.

Remaining deployment work: provision production signing secrets/issuer/audience, terminate HTTPS correctly, configure trusted proxy handling if needed, and add a shared gateway limiter when deploying multiple replicas. MFA APIs and external OAuth/OIDC federation are future features rather than password-only fallback paths.


## BookVerse frontend integration

The custom responsive login page remains at /Identity/Account/Login and uses the existing BookVerse navigation, colors and library image. It includes inline validation, accessible password visibility controls, remember-me, reset-password/register links, loading feedback and expired-session notices. Forms remain usable without JavaScript and retain antiforgery protection. A safe local return URL takes precedence; otherwise Admin users go to /AdminOperations/Dashboard and readers to /Home/Index.

The MVC server calls POST /api/auth/login with HTTPS Basic credentials. It then associates the returned session with an HttpOnly, Secure, SameSite=Lax Identity cookie. Access and refresh tokens stay on the server, encrypted with the MVC Data Protection purpose BookVerse.BrowserTokens.v1. They are never placed in HTML, JavaScript storage, or the browser cookie. API refresh-token verification continues to use hashes.

On each authenticated browser request, cookie validation checks the shared durable session, account state/security stamp, current roles and token lifetime. It refreshes within one minute of expiry or after a role change. SQL application locks serialize browser refresh/logout across MVC replicas; parallel catalog requests reuse the request's validated token. The Catalog HttpClient attaches Bearer tokens per request. It never modifies shared default authorization headers. Existing MVC controllers/services retain their architecture and authorization attributes.

The logout form calls the Logout API and clears the cookie and encrypted pair. If the API is unavailable or the access token expired, the shared database session is revoked locally. If the database itself is unavailable, the browser is signed out and the UI reports that durable revocation could not be confirmed. Refresh failures with an ambiguous outcome revoke the family rather than retrying the same potentially consumed token. No background refresh runs while the browser is idle; expiry is handled on the next request.

Both hosts must use the same application database. Apply **20261105121209_ProtectBrowserAuthenticationTokens** before starting the updated hosts. ApiSettings__BaseUrl must be an HTTPS URL ending in /api/; development defaults to https://localhost:7149/api/. Start both hosts with their HTTPS launch profiles. Certificate validation remains enabled. The MVC authentication client does not follow redirects. Persist and protect Data Protection keys, and share them across MVC replicas with the same application discriminator. Losing or mismatching these keys forces sign-in again. Signing keys remain API secrets; MVC does not require the JWT signing key.

Browser DevTools will still show POST /Identity/Account/Login followed by a redirect, because API calls happen on the MVC server. This is expected; the end-to-end tests explicitly verify Basic login, Bearer catalog requests, refresh and logout against the real API host. Existing cookies without a durable API-session reference must sign in again. MFA-enabled accounts remain unsupported by the current API and fail closed.

Changed frontend integration files:

- BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml and Login.cshtml.cs
- BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Logout.cshtml.cs
- BookShoppingCartMvcUI/Services/AuthenticationApiClient.cs, BrowserAuthenticationService.cs and ApiBearerHandler.cs
- BookShoppingCartMvcUI/Program.cs and Controllers/HomeController.cs
- BookShoppingCartMvcUI/wwwroot/css/login.css and wwwroot/js/login.js
- BookShoppingCart.Models/Models/ApiSession.cs, the additive migration/designer and model snapshot
- BookShoppingCart.Tests/Tests/FrontendAuthenticationTests.cs and Frontend/login.test.cjs
- README.md, this guide and deployment SQL/execution notes

Validation on 5 November 2026: all **121 .NET tests and 6 JavaScript tests passed**; the nine-project Release solution built with **zero warnings/errors**. Browser inspection verified desktop and mobile layouts. Tests use real MVC and API test hosts with a disposable SQL Express database, including simultaneous browser refresh, safe redirects, CSRF, role/stamp changes, offline login/logout and refresh expiry/outages.

The server-side token/cookie design follows [Microsoft JWT guidance for web applications](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication); cookie lifecycle validation follows [ASP.NET Core cookie authentication guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-9.0).
