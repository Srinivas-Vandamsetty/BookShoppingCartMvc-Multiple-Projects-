# Book Shopping Cart

ASP.NET Core MVC + FastEndpoints API, EF Core/SQL Server and Identity on .NET 9.
The solution includes the MVC storefront, a single Web API host, shared libraries, and tests.
Start **BookShoppingCart.WebAPI** for authentication, catalog and commerce APIs.

### Run locally with Docker Compose

From the repository root, open PowerShell 7 with Docker Desktop running in Linux container mode. Run the setup command once to generate local secrets and trust the local HTTPS certificate for your Windows account.

```powershell
./scripts/Initialize-Docker.ps1 -TrustCertificate
docker compose up --build
```

Open `https://localhost:5443` for the storefront or `https://localhost:5444/swagger` for the API. SQL Server uses its own persistent volume. On startup the initialization service applies migrations and seeds the catalog before the apps start. The generated `.env` and `.docker-local` files are local credentials and are ignored by Git and Docker builds.

Use `docker compose down` to stop the stack and retain the database, covers, and application keys. The first run downloads the SQL Server image and starts with a separate database from Windows SQL Express.

GitHub Actions runs the .NET and JavaScript suites on pull requests targeting `master` and pushes to `master`. SQL Server integration tests use an isolated SQL Server service on the runner. After validation, both Dockerfiles build. Successful pushes to `master` publish `bookshoppingcart-ui` and `bookshoppingcart-api` to Docker Hub with the full commit SHA as the tag.

Set the repository secrets `DOCKER_USERNAME` and `DOCKER_PASSWORD` using the Docker Hub username and a scoped write token. The workflow uploads test results and records the published image digests in the `image-digests` artifact. Use those digest references when selecting a pair of images for deployment. To make validation required, select the **Build and test** and **Build container images** checks in the GitHub rules for `master`.

## Local setup

Use a .NET 9 SDK and SQL Server/LocalDB. Development defaults use the existing BookShoppingCartMvc database on COGNINE-L218\SQLEXPRESS01.
Override ConnectionStrings__DefaultConnection for a different development database.
The existing SQL Express database was schema-verified, baselined and upgraded through the commerce and authentication migrations on 5 November 2026. See deployment/MIGRATION_EXECUTION.md for the verified backup and validation record.
Never point development/test initialization at production.

1. Restore and build with the commands below.
2. Review the additive HardenCommerce migration and back up existing data. Its preflight deliberately stops for duplicate emails/carts/genres or invalid quantities; reconcile these explicitly rather than deleting data.
3. Initialize the intended local database using the explicit command below. It migrates/seeds roles/catalog and then exits. It creates no administrator.
4. To provision a new administrator, supply BootstrapAdmin__Email and a strong BootstrapAdmin__Password through a temporary process environment/secret store, then run the provision command. Remove bootstrap variables afterward. Existing accounts are never silently elevated or assigned a new password.
5. Start the active API and MVC hosts with the https profiles. Configure ApiSettings__BaseUrl if changing the API address.
6. New users must confirm email. In Development without SMTP, messages are written to **BookShoppingCartMvcUI/.email-outbox**, outside wwwroot and ignored by Git. Open the local message for the confirmation link. Anonymous pages never expose confirmation tokens.

    dotnet restore BookShoppingCartMvc.sln
    dotnet build BookShoppingCartMvc.sln -m:1
    dotnet run --project BookShoppingCartMvcUI -- --initialize-database
    dotnet run --project BookShoppingCartMvcUI -- --provision-admin
    dotnet run --project BookShoppingCart.WebAPI --launch-profile https
    dotnet run --project BookShoppingCartMvcUI --launch-profile https

See [COMMERCE_API.md](COMMERCE_API.md) for the P1 shopping and administration routes, UI integration, and deployment requirements.

## Payments and pricing

Cash on delivery is currently supported. Orders remain unpaid until collection is recorded by an administrator.
Card/UPI/refund implementations fail closed instead of fabricating successful transactions.
Enable electronic methods only after integrating a provider with verified callbacks, durable payment attempts, idempotency and reconciliation tests.
Checkout uses decimal currency, authoritative prices/coupons/shipping, a signed cart review and a per-user idempotency key.
Books are archived on removal; historical order prices/titles and images remain available.
A periodic job retries cleanup of old, unreferenced generated images.

## Deployment configuration

Provide settings through environment variables or a secret manager. No production database or bootstrap password is committed.

| Setting | Purpose |
|---|---|
| ConnectionStrings__DefaultConnection | Same intended SQL database for MVC/API; production requires Encrypt=True and TrustServerCertificate=False |
| ApiSettings__BaseUrl | MVC catalog API absolute HTTPS URL ending in /api/ |
| Email__SmtpHost, Email__SmtpPort | Production TLS SMTP server and port (default 587) |
| Email__From, Email__Username, Email__Password | Sender and secret-backed SMTP credentials |
| Jwt__SigningKey | Secret base64 JWT signing key (at least 32 random bytes); use User Secrets for local development |
| Jwt__Issuer, Jwt__Audience | Exact trusted JWT issuer and API audience |
| RateLimits__AuthenticationPerMinute | Per-IP, per-process login/refresh/logout quota, default 20 |
| RateLimits__CatalogPerMinute | Aggregate service-IP catalog quota, default 600; size for MVC load |
| AllowedHosts | Set to intended deployment host names |
| Database__Initialize | Defaults false; opt in only for controlled initialization |

MVC genre administration uses the shared application service and Identity Admin authorization. It no longer needs an unauthenticated server-to-server write API.
Public API reads remain anonymous; genre writes require an Admin JWT Bearer token.
The custom BookVerse UI login uses the HTTPS Basic Login API through MVC; refresh and logout use durable, revocable sessions.
Tokens remain encrypted on the server; the browser uses a Secure/HttpOnly session cookie.
Apply ProtectBrowserAuthenticationTokens and restart both hosts when upgrading. Existing cookies require sign-in again.
See [AUTHENTICATION_API.md](AUTHENTICATION_API.md) for endpoints, token rotation, signing-key configuration and deployment requirements.
Configure Jwt__SigningKey before starting the API; no signing secret is committed.
Use reviewed deployment migrations. The design-time factory builds models without starting or seeding a web host.

    dotnet ef migrations script --idempotent --project BookShoppingCart.Data --startup-project BookShoppingCartMvcUI

Persist/share Data Protection keys for MVC replicas and protect their storage. Use durable shared image storage/a mounted volume for containers.
The Dockerfile copies all referenced project definitions before restore.

Previously deployed seeded administrator/SQL credentials must be rotated by the operator. Removing literals cannot revoke a live credential or erase Git history.
Migration snapshots current book metadata into old orders and derives old subtotals from stored lines; old shipping/discount values were never stored and cannot be recovered.
Do not roll back the currency/snapshot migration after new orders without a reviewed data-preserving recovery plan.

## Validation

    dotnet test BookShoppingCart.Tests/BookShoppingCart.Tests.csproj -m:1

The suite includes service tests, relational SQLite cart/rollback/history tests, file security tests and in-memory HTTP authentication/antiforgery tests.
SQL-specific migration/concurrent-last-unit tests use BOOKSTORE_TEST_SQL, a connection to an isolated SQL Server/LocalDB test server.
They create a new randomly named BookShoppingCartTests_<GUID> database and remove only that database.
Without that setting, the SQL-specific test is explicitly skipped. Never use a production test server.
If running apps lock Debug DLLs, build/test Release or another isolated configuration instead of stopping unrelated processes.
