# Book Shopping Cart — comprehensive code review

Audit date: 5 November 2026 (Asia/Calcutta). Scope: current working tree, including eight pre-existing modified files. No fixes have been implemented and no existing source/configuration/test file was edited. This report is a new audit artifact.

## Executive summary

**The application is not ready for production commerce.** The most urgent problems are a fixed seeded administrator, anonymously writable genre APIs, unsafe state-changing MVC routes, untrusted checkout pricing, and loss of historical order lines on book deletion. A new user cannot create a cart through the current repository path, and a freshly initialized database lacks the status required for checkout. Electronic payment implementations are simulations, not gateway integrations.

The code has a useful layered starting structure (MVC/API, Business, Data, Models and Core), uses Identity and EF rather than hand-written password/SQL handling, and generally awaits I/O. However, the layers do not consistently enforce transaction, pricing, authorization, validation or ownership boundaries. Service-only mocked tests leave the most important failures untested.

There are **52 nonduplicated findings**: **2 Critical, 17 High, 27 Medium and 6 Low**. Each entry below has a primary category, exact source locations, evidence/impact, recommended fix and proposed code or implementation sketch. Findings labeled improvement suggestions are not claimed to be observed runtime failures. Some integration/concurrency consequences are inferred directly from verified implementation and are explicitly identified.

## Scope, architecture and validation limits

- Read/reviewed the seven solution projects: MVC UI, active FastEndpoints API, Business, Data, Models, Core and Tests; also the two API projects outside the solution.
- Reviewed controllers/endpoints, all service/repository implementations and interfaces, payment factories/decorators, shipping strategies, proxies/facades/logging, domain/DTO classes, DbContext/seeder, the initial migration/designer/snapshot, Identity account/manage handlers and form bindings, Razor views/layouts, first-party JavaScript/CSS, project/solution dependencies, application/launch settings, Dockerfile, HTTP samples, publish/deployment profiles and the minimal README.
- Third-party minified Bootstrap/jQuery libraries, image binaries, IDE state, generated bin/obj contents and Git internals are not treated as first-party application code. This is not a line-by-line audit of vendor source or repository history.
- The solution projects target **net9.0**, with EF/Identity packages primarily 9.0.1 and selected tooling at 9.0.3. One excluded prototype targets net8.0. Snippets use C#/.NET 9/EF Core 9-compatible constructs. New DTOs/services/properties shown below are proposals requiring implementation and, where applicable, migrations; they are not paste-ready whole-project patches.
- Checked the current Git diff/status. The eight pre-existing edits are DiscountService, GenreService, GenreRepository, IGenreRepository, CartServiceTests, DeleteGenreEndpoint, GenreController and GetUserCart.cshtml. They were included in the audit, not reverted.
- Normal no-restore solution build emitted NU1701, nullable and other analyzer warnings, then failed with **MSB3021/MSB3027** because running application processes held destination DLLs. Those locks are an environment limitation, not counted as a code defect.
- Retried validation using an isolated temporary output directory. Compilation progressed to VSTest; test execution aborted because the testhost did not connect within 90 seconds. **No test pass/fail totals are claimed.** Existing tests were not changed. Earlier default parallel/no-restore attempts also did not complete successfully.
- Did not start the application hosts, execute destructive workflows, connect to the configured database, apply migrations, call a real payment/email provider, or modify live data. Starting MVC would itself trigger migrations/seeding, so a safe database-backed end-to-end execution was not part of this review.
- Runtime/schema observations here describe the checked-in implementation; external production overrides, existing seed data, deployed credentials and gateway/network controls were not available for verification.
- No SQL injection was found in the reviewed active data paths: they use EF LINQ/Find rather than concatenated SQL. No demonstrated memory leak, sync-over-async blocking or singleton-held DbContext was found. These are scoped conclusions, not a guarantee that deployment behavior has no defects.
- DbContext/repositories/services use scoped lifetimes. The singleton DiscountService reads a private initialized dictionary and has no scoped dependency; its current lifetime is reasonable. Absence of an explicit UseAuthentication call is not itself reported as a bug because WebApplication can automatically insert registered authentication middleware.
- The stock action and view both use **ManangeStock**, so their misspelling does **not** cause a view lookup mismatch. Reports has a leftover view but no implemented Reports controller; this is an unexposed feature remnant, not a broken linked workflow found in current navigation.

## Counts by primary category and severity

| Category | Critical | High | Medium | Low | Total |
|---|---:|---:|---:|---:|---:|
| Security | 2 | 7 | 2 | 0 | 11 |
| Bug | 0 | 6 | 8 | 0 | 14 |
| Integration | 0 | 1 | 1 | 0 | 2 |
| Architecture | 0 | 0 | 3 | 1 | 4 |
| Data integrity | 0 | 1 | 0 | 0 | 1 |
| Frontend | 0 | 0 | 9 | 3 | 12 |
| Configuration | 0 | 0 | 1 | 0 | 1 |
| Performance | 0 | 1 | 2 | 0 | 3 |
| Build | 0 | 0 | 1 | 0 | 1 |
| Testing | 0 | 1 | 0 | 0 | 1 |
| Coding standard | 0 | 0 | 0 | 2 | 2 |
| **Total** | **2** | **17** | **27** | **6** | **52** |

Severity reflects likely business impact: Critical = direct privileged/catalog compromise; High = major security, data integrity or core purchasing/release failure; Medium = constrained correctness/integration/scalability failure; Low = usability, maintainability or preventative improvement. Multiple source occurrences of one root cause are grouped under one ID. Secondary concerns are discussed without double counting.

## Prioritized remediation

1. **Contain exposure first:** revoke seeded/committed credentials (BR-001/005), protect mutation APIs (BR-002), stop unsafe GET mutations and enforce MVC antiforgery (BR-003), close arbitrary file deletion and graph binding (BR-006/010), and repair email identity controls (BR-007/008/009).
2. **Restore a safe first purchase:** correct cart creation (BR-011), add the missing status seeds and migration (BR-012/013), validate quantities (BR-016), protect stock and cart concurrency (BR-014/015), and prevent historical deletion (BR-022).
3. **Make checkout financially authoritative:** compute/persist server quotes (BR-004/020/021), replace or explicitly disable simulated electronic payment (BR-017), implement durable idempotent payment/order finalization (BR-018/019), and wire real recovery email delivery (BR-037).
4. **Repair customer/admin failure paths:** wrong model types (BR-023/025), missing-book response (BR-024), form reconstruction and missing-stock handling (BR-026/027), anonymous cart initialization (BR-028), encoding and paging (BR-029–031), and cache/asset consistency (BR-032–035).
5. **Establish release confidence:** add relational/API/workflow tests (BR-046), resolve dependency/prototype build problems (BR-044/045), require deploy-specific configuration (BR-040), size rate limits for the actual topology (BR-043), and address validation/error handling (BR-036/052).
6. **Then optimize and simplify:** database paging and query batching (BR-041/042), DI boundaries and nullable/type contracts (BR-047–049), and remaining UI/seed consistency (BR-038/039/050/051).

## Detailed findings

### BR-001 — Startup provisions a known administrator

**Category:** Security · **Severity:** Critical · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Data/DbSeeder.cs:42-53](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:42>); [BookShoppingCartMvcUI/Program.cs:63-68](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Program.cs:63>).

**Issue and why it matters:** Every deployment that successfully seeds an absent admin account uses a fixed email and password in source. Anyone with repository access can authenticate as administrator if that credential remains active. Seeding runs in Production too. Existing deployments need credential rotation; deleting the literal alone does not revoke it.

**Recommended fix:** Remove automatic production admin creation. Use a one-time provisioning operation with a supplied secret and check every IdentityResult. Rotate any deployed seeded credential.

**Suggested change / implementation sketch:**

```csharp
// In a one-time provisioning command, never a source literal:
var password = configuration["BootstrapAdmin:Password"]
    ?? throw new InvalidOperationException("Provisioning password missing.");
var result = await userManager.CreateAsync(admin, password);
if (!result.Succeeded)
    throw new InvalidOperationException("Administrator provisioning failed.");
```

### BR-002 — Genre mutation endpoints permit anonymous callers

**Category:** Security · **Severity:** Critical · **Classification:** Verified defect

**Locations:** [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:16-17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:16>); [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/UpdateGenreEndpoint.cs:16-17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/UpdateGenreEndpoint.cs:16>); [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/DeleteGenreEndpoint.cs:20-21](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/DeleteGenreEndpoint.cs:20>); [BookShoppingCart.WebAPI/Program.cs:26-33](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Program.cs:26>).

**Issue and why it matters:** The MVC GenreController requires Admin, but the underlying POST, PUT and DELETE API endpoints explicitly call AllowAnonymous. A caller can bypass the MVC controller and mutate the catalog directly. No API authentication scheme is configured.

**Recommended fix:** Configure API authentication and an Admin role/policy, remove AllowAnonymous from mutation endpoints, and make the MVC client forward authenticated credentials using the chosen scheme. Changing endpoint metadata alone without configuring the client would break admin integration.

**Suggested change / implementation sketch:**

```csharp
// Each mutation endpoint, after API authentication is configured:
Roles("Admin");
// Keep AllowAnonymous only on intended public catalog reads.
```

### BR-003 — Cookie-authenticated mutations accept GET and lack antiforgery validation

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/CartController.cs:73-95](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/CartController.cs:73>); [BookShoppingCartMvcUI/Controllers/BookController.cs:169-204](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/BookController.cs:169>); [BookShoppingCartMvcUI/Controllers/GenreController.cs:173](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/GenreController.cs:173>); [BookShoppingCartMvcUI/Controllers/AdminOperationsController.cs:28](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/AdminOperationsController.cs:28>); [BookShoppingCartMvcUI/Program.cs:30](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Program.cs:30>).

**Issue and why it matters:** Cart add/remove, book delete/clone, genre delete and payment toggling mutate data through GET-capable actions. MVC POST actions also have no ValidateAntiForgeryToken or global equivalent. Form-generated tokens are not automatically validated by MVC. A cross-site top-level GET can carry a Lax authentication cookie and trigger a mutation.

**Recommended fix:** Convert all mutations to POST and enforce antiforgery globally for MVC. Update links and fetch calls to send tokens. Razor Pages already have antiforgery conventions; preserve them.

**Suggested change / implementation sketch:**

```csharp
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

[HttpPost]
public async Task<IActionResult> RemoveItem(int bookId)
{
    await _cartService.RemoveItem(bookId);
    return RedirectToAction(nameof(GetUserCart));
}
```

### BR-004 — Checkout accepts an untrusted payment amount, normally zero

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Services/CartService.cs:61-73](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/CartService.cs:61>); [BookShoppingCart.Models/Models/DTOs/CheckoutModel.cs:24-25](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/DTOs/CheckoutModel.cs:24>); [BookShoppingCartMvcUI/Views/Cart/Checkout.cshtml:16-75](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Cart/Checkout.cshtml:16>).

**Issue and why it matters:** ProcessPayment receives model.TotalAmount directly. The checkout form contains no TotalAmount input, so normal model binding leaves decimal at zero; Required on a nonnullable decimal does not enforce a positive amount. A crafted request can submit any amount. Coupons and shipping are calculated only while rendering the cart and are neither passed to checkout nor persisted.

**Recommended fix:** Remove payable amounts from the input contract. Build an authoritative server quote from the user's persisted cart, validated coupon and shipping selection, round according to the currency policy, and persist that quote with the order.

**Suggested change / implementation sketch:**

```csharp
// Proposed checkout orchestration; quote service is a new abstraction:
var quote = await _pricing.CreateQuoteAsync(userId, request.CouponCode, ct);
if (quote.Lines.Count == 0 || quote.Total <= 0)
    return CheckoutResult.InvalidCart();
var result = await _payments.PayAsync(quote.Total, request.IdempotencyKey, ct);
```

### BR-005 — A database password is committed in production configuration

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCat-WebAPI/appsettings.Production.json:3](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCat-WebAPI/appsettings.Production.json:3>).

**Issue and why it matters:** The tracked production settings contain a SQL username and plaintext password. This is in a project outside the solution, but it is still a committed credential. Whether the credential is currently valid was not tested; no connection was attempted.

**Recommended fix:** Rotate the credential if ever used, remove it from tracked configuration and deployment artifacts, use environment/secret-store injection, and assess repository history exposure. Do not copy the credential into reports.

**Suggested change / implementation sketch:**

```csharp
// Deployment environment variable (value supplied by the secret store):
// ConnectionStrings__DefaultConnection=<secret connection string>
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection missing.");
```

### BR-006 — Client-controlled image names can reach filesystem deletion

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Facades/BookFacade.cs:62-86](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Facades/BookFacade.cs:62>); [BookShoppingCartMvcCore/Shared/FileService.cs:33-38](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcCore/Shared/FileService.cs:33>); [BookShoppingCartMvcUI/Views/Book/UpdateBook.cshtml:20](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Book/UpdateBook.cshtml:20>).

**Issue and why it matters:** UpdateBook trusts the posted hidden Image value as oldImage and later deletes it after saving a replacement. Path.Combine accepts traversal segments or a rooted filename, and DeleteFile performs no containment check. An admin request, or a forged request exploiting issue 3, can delete files outside the image directory within the app identity's permissions. AddBook also accepts a posted Image name that can later reach deletion.

**Recommended fix:** Load the old image from the database, never from the request. Restrict stored names to generated basenames and reject rooted paths/separators. Resolve and verify containment before deletion.

**Suggested change / implementation sketch:**

```csharp
if (string.IsNullOrWhiteSpace(fileName) ||
    Path.IsPathRooted(fileName) ||
    fileName.Contains('/') || fileName.Contains('\\'))
    throw new ArgumentException("Invalid image name.");
var filePath = Path.GetFullPath(Path.Combine(_imagePath, fileName));
var root = Path.GetFullPath(_imagePath) + Path.DirectorySeparatorChar;
if (!filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Invalid image path.");
```

### BR-007 — Email ownership verification is bypassed in two paths

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml.cs:125-127](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml.cs:125>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/RegisterConfirmation.cshtml.cs:45-71](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/RegisterConfirmation.cshtml.cs:45>); [BookShoppingCartMvcUI/Program.cs:25](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Program.cs:25>).

**Issue and why it matters:** Registration sets EmailConfirmed=true without sending a verification message despite RequireConfirmedAccount=true. Separately, an anonymous RegisterConfirmation request for an existing email generates and displays a valid confirmation URL. The second path remains a bypass even if automatic confirmation is removed.

**Recommended fix:** Implement a real email confirmation flow, keep EmailConfirmed false until token verification, and remove public token generation/display. Return the same generic confirmation message for existing and absent accounts.

**Suggested change / implementation sketch:**

```csharp
// After successful registration:
var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
// Encode token, construct callback URL and send it to the user's mailbox.
// RegisterConfirmation must only display a generic 'check your email' message.
```

### BR-008 — Duplicate emails can break email-based sign-in and recovery

**Category:** Bug · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Program.cs:24-28](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Program.cs:24>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml.cs:111-117](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml.cs:111>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:107](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:107>).

**Issue and why it matters:** Users choose independent usernames, but login uses FindByEmailAsync. Identity's RequireUniqueEmail defaults to false and is not changed here; the migration's EmailIndex is nonunique. Two usernames can register the same email, making lookup ambiguous and potentially throwing when the store expects one result. It also undermines recovery routing.

**Recommended fix:** Require unique normalized emails, resolve existing duplicates before rollout, and add a filtered unique database index if email is the login identifier. Handle race-time uniqueness violations cleanly.

**Suggested change / implementation sketch:**

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
});
```

### BR-009 — Password failures do not activate Identity lockout

**Category:** Security · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:110](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:110>).

**Issue and why it matters:** PasswordSignInAsync explicitly sets lockoutOnFailure:false. Repeated incorrect passwords do not increment Identity's lockout counter on this path. The books API limiter does not protect this login endpoint.

**Recommended fix:** Enable lockout counting and add a carefully chosen login rate policy. Use generic failure responses and test both lockout and recovery behavior.

**Suggested change / implementation sketch:**

```csharp
var result = await _signInManager.PasswordSignInAsync(
    user.UserName, Input.Password, Input.RememberMe,
    lockoutOnFailure: true);
```

### BR-010 — Genre creation binds and persists an entity graph

**Category:** Security · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:5-24](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:5>); [BookShoppingCart.Models/Models/Genre.cs:15](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/Genre.cs:15>); [BookShoppingCart.Data/Repositories/BaseRepository.cs:24-27](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/BaseRepository.cs:24>).

**Issue and why it matters:** The request type is the EF Genre entity, including Books. AddGenre passes the object to DbSet.AddAsync, which traverses reachable new entities. A request can therefore create nested books and potentially further related records without going through BookService validation. Authorization in issue 2 and safe input contracts are distinct controls.

**Recommended fix:** Accept a DTO containing only writable scalar fields and construct a new Genre explicitly. Return response DTOs rather than serializing tracked graphs, which can also contain reference cycles.

**Suggested change / implementation sketch:**

```csharp
public sealed record CreateGenreRequest(string GenreName);
// Map only explicitly allowed input:
var genre = new Genre { GenreName = req.GenreName.Trim() };
await _genreService.AddGenre(genre);
```

### BR-011 — A new user's first cart item cannot be saved

**Category:** Bug · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:39-44](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:39>); [BookShoppingCart.Data/Repositories/CartRepository.cs:135-141](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:135>).

**Issue and why it matters:** GetCart returns a new nonnull ShoppingCart when no database cart exists. AddItem only attaches/saves a cart inside if(cart == null), so that branch is unreachable. The new CartDetail receives ShoppingCartId=0 and no cart navigation; SQL Server rejects its foreign key. Normal registration does not create a cart elsewhere.

**Recommended fix:** Let GetCart return null for absence, then explicitly create and save the cart in AddItem. Keep empty presentation models in the read/UI path rather than in persistence lookup.

**Suggested change / implementation sketch:**

```csharp
public Task<ShoppingCart?> GetCart(string userId) =>
    _db.ShoppingCarts.Include(c => c.CartDetails)
        .SingleOrDefaultAsync(c => c.UserId == userId);
// Existing AddItem creation branch now becomes reachable.
```

### BR-012 — Fresh databases contain no checkout order status

**Category:** Bug · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:178-182](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:178>); [BookShoppingCart.Data/Data/DbSeeder.cs:56-64](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:56>); [BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:66-78](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:66>).

**Issue and why it matters:** DoCheckout requires an OrderStatus named Pending. The seeder creates roles, genres and books only; the migration creates an empty status table and contains no InsertData. No other status creation implementation exists. A new database cannot complete checkout even after cart creation is corrected.

**Recommended fix:** Seed stable order statuses idempotently using a migration or controlled initialization, and reference a stable code rather than a mutable display string.

**Suggested change / implementation sketch:**

```csharp
if (!await context.OrderStatuses.AnyAsync(s => s.StatusName == "Pending"))
{
    context.OrderStatuses.Add(new OrderStatus
        { StatusId = 1, StatusName = "Pending" });
    await context.SaveChangesAsync();
}
```

### BR-013 — The current cart model differs from the migration snapshot

**Category:** Bug · **Severity:** High · **Classification:** Verified model mismatch; startup impact inferred

**Locations:** [BookShoppingCart.Models/Models/ShoppingCart.cs:15-16](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/ShoppingCart.cs:15>); [BookShoppingCart.Data/Migrations/ApplicationDbContextModelSnapshot.cs:222-223](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/ApplicationDbContextModelSnapshot.cs:222>); [BookShoppingCart.Data/Data/DbSeeder.cs:22-24](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:22>).

**Issue and why it matters:** ShoppingCart.UserId is Required now, but the latest snapshot and initial migration make it nullable. No follow-up migration exists. EF Core 9 throws on MigrateAsync when model changes are pending, so a fresh/pending-migration startup can fail before roles/catalog are seeded; the seeder then swallows that failure. When no migration is pending, this conditional skips migration validation and leaves schema drift.

**Recommended fix:** Generate and inspect an additive migration, account for any existing null values, and test migration from empty and prior schemas. Do not suppress PendingModelChangesWarning to hide the mismatch.

**Suggested change / implementation sketch:**

```text
// Commands to run only after fixes are approved:
// dotnet ef migrations add RequireCartUserId --project BookShoppingCart.Data --startup-project BookShoppingCartMvcUI
// dotnet ef migrations has-pending-model-changes --project BookShoppingCart.Data --startup-project BookShoppingCartMvcUI
```

### BR-014 — Concurrent checkouts can oversell inventory

**Category:** Bug · **Severity:** High · **Classification:** Verified concurrency defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:214-223](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:214>); [BookShoppingCart.Models/Models/Stock.cs:8-10](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/Stock.cs:8>).

**Issue and why it matters:** The code reads stock, checks availability and writes a new quantity without a rowversion or conditional update. The explicit transaction uses default isolation and does not prevent two different users from both reading quantity 1 and each buying it; both can write 0 while two orders succeed. Admin stock updates can similarly overwrite concurrent deductions.

**Recommended fix:** Use a conditional atomic decrement inside the order transaction, or rowversion concurrency with controlled retries. Avoid mixing stale tracked Stock values with ExecuteUpdate.

**Suggested change / implementation sketch:**

```csharp
var changed = await _db.Stocks
    .Where(s => s.BookId == item.BookId && s.Quantity >= item.Quantity)
    .ExecuteUpdateAsync(s => s.SetProperty(
        x => x.Quantity, x => x.Quantity - item.Quantity));
if (changed != 1)
    throw new InvalidOperationException("Insufficient stock.");
```

### BR-015 — Cart row uniqueness and quantity updates are not concurrency-safe

**Category:** Bug · **Severity:** Medium · **Classification:** Verified concurrency defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:47-65](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:47>); [BookShoppingCart.Data/Data/ApplicationDbContext.cs:8-19](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/ApplicationDbContext.cs:8>); [BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:366-379](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:366>).

**Issue and why it matters:** Find-then-add and read-modify-write can create duplicate book rows or lose increments under simultaneous requests. There is no unique cart-per-user or (ShoppingCartId,BookId) index. Once issue 11 is corrected, simultaneous first adds can also create multiple carts, while FirstOrDefault subsequently selects only one.

**Recommended fix:** Add uniqueness constraints after deduplicating existing data; make quantity mutations atomic or use optimistic concurrency and retries. Bound user IDs before creating an index.

**Suggested change / implementation sketch:**

```csharp
modelBuilder.Entity<ShoppingCart>().Property(c => c.UserId).HasMaxLength(450);
modelBuilder.Entity<ShoppingCart>().HasIndex(c => c.UserId).IsUnique();
modelBuilder.Entity<CartDetail>()
    .HasIndex(c => new { c.ShoppingCartId, c.BookId }).IsUnique();
```

### BR-016 — Cart quantity can overflow and bypass stock accounting

**Category:** Security · **Severity:** High · **Classification:** Verified arithmetic/input defect

**Locations:** [BookShoppingCart.Business/Services/CartService.cs:22-28](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/CartService.cs:22>); [BookShoppingCart.Data/Repositories/CartRepository.cs:50](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:50>); [BookShoppingCart.Data/Repositories/CartRepository.cs:219-223](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:219>).

**Issue and why it matters:** Only qty>0 is checked. An existing quantity of 1 plus int.MaxValue wraps to int.MinValue under the default unchecked arithmetic. Negative quantities then pass item.Quantity>stock.Quantity, and stock subtraction can corrupt inventory. Large positive adds also bypass the UI stock limit. This is reachable for existing carts; issue 11 currently blocks new ones.

**Recommended fix:** Enforce a bounded quantity and stock rule on the server, use checked arithmetic/atomic conditional updates, reject nonpositive checkout lines, and add database CHECK constraints as defense in depth.

**Suggested change / implementation sketch:**

```csharp
var newQuantity = checked(cartItem.Quantity + qty);
if (newQuantity <= 0 || newQuantity > availableStock || newQuantity > maxPerOrder)
    throw new ArgumentOutOfRangeException(nameof(qty));
cartItem.Quantity = newQuantity;
// Revalidate positive quantities during checkout as well.
```

### BR-017 — Card and UPI payments are simulated successes

**Category:** Integration · **Severity:** High · **Classification:** Verified implementation gap

**Locations:** [BookShoppingCart.Business/Factories/CreditCardPaymentService.cs:12-21](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Factories/CreditCardPaymentService.cs:12>); [BookShoppingCart.Business/Factories/UpiPaymentService.cs.cs:13-22](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Factories/UpiPaymentService.cs.cs:13>); [BookShoppingCart.Business/Factories/PaymentFactory.cs:15-17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Factories/PaymentFactory.cs:15>).

**Issue and why it matters:** The active checkout factory selects implementations that delay, generate a GUID and always return IsSuccess=true. They never contact a gateway, collect a payment reference, or validate a signed provider result. The parallel AbstractFactory implementations are also simulations. An 'order successful' response is not evidence of collected electronic payment.

**Recommended fix:** Keep explicit fake providers restricted to development/test. Production must use a real provider adapter, server-side provider verification and persisted payment state. Until integrated, disable electronic methods or present the application explicitly as a demo.

**Suggested change / implementation sketch:**

```csharp
// Fail closed until an actual provider is configured:
if (environment.IsProduction() && paymentOptions.UseFakeProvider)
    throw new InvalidOperationException("A production payment provider is required.");
```

### BR-018 — Payment happens before cart validation, with no idempotency or reconciliation

**Category:** Bug · **Severity:** High · **Classification:** Verified workflow defect; financial consequence upon real integration

**Locations:** [BookShoppingCart.Business/Services/CartService.cs:66-73](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/CartService.cs:66>); [BookShoppingCart.Data/Repositories/CartRepository.cs:155-241](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:155>); [BookShoppingCart.Models/Models/Order.cs:10-37](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/Order.cs:10>).

**Issue and why it matters:** Payment is requested before the repository checks the cart, stock or Pending status. A successful provider call can be followed by false from DoCheckout, leaving a charge without an order. Repeated POSTs can call payment again. TransactionId is discarded, no unique request key exists, and database rollback cannot reverse an external payment. Today payments are fake (issue 17), so actual charging was not observed.

**Recommended fix:** Create a durable pending order/quote and inventory reservation, use a unique idempotency key with the provider, then confirm or compensate/reconcile failures. Keep network calls outside long database transactions.

**Suggested change / implementation sketch:**

```csharp
// Proposed durable workflow:
// 1. Persist PendingPayment order + quote + unique CheckoutKey + reservation.
// 2. Authorize payment using CheckoutKey as provider idempotency key.
// 3. Persist provider reference and transition order atomically.
// 4. Release reservation or queue reconciliation/void/refund on failure.
```

### BR-019 — Successful electronic payments are recorded as unpaid

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:185-195](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:185>); [BookShoppingCart.Business/Services/CartService.cs:68-73](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/CartService.cs:68>); [BookShoppingCartMvcUI/Views/AdminOperations/AllOrders.cshtml:41-43](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/AdminOperations/AllOrders.cshtml:41>).

**Issue and why it matters:** The service requires payment success, but repository order creation always sets IsPaid=false. UPI/card orders are therefore displayed as Not Paid even when the payment adapter reports success. Cash-on-delivery should remain unpaid until collection, so blindly setting true for every method is also wrong.

**Recommended fix:** Pass verified payment status/reference into order finalization. Separate cash collection from captured electronic payment, and persist the provider reference for reconciliation.

**Suggested change / implementation sketch:**

```csharp
// After a verified captured electronic payment:
order.IsPaid = payment.Status == PaymentStatus.Captured;
order.PaymentReference = payment.ProviderReference; // new persisted property
// Cash-on-delivery remains awaiting collection.
```

### BR-020 — Cart and order history use different sources of price

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/CartController.cs:39-40](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/CartController.cs:39>); [BookShoppingCart.Data/Repositories/CartRepository.cs:63](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:63>); [BookShoppingCart.Data/Repositories/CartRepository.cs:208-209](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:208>); [BookShoppingCartMvcUI/Views/UserOrder/UserOrders.cshtml:59-61](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/UserOrder/UserOrders.cshtml:59>).

**Issue and why it matters:** Cart display uses the current Book.Price, but checkout persists CartDetail.UnitPrice captured when first added. Updating a book price between add and checkout therefore makes displayed and recorded totals disagree. The user's order history then uses current Book.Price again, so historical totals change with later catalog edits; the admin modal correctly uses OrderDetail.UnitPrice.

**Recommended fix:** Define a price-lock/repricing policy, calculate and persist one checkout quote, and always display historical totals from order snapshots.

**Suggested change / implementation sketch:**

```csharp
// Order history:
// <td>@item.UnitPrice.ToString("0.00")</td>
// <td>@((item.UnitPrice * item.Quantity).ToString("0.00"))</td>
// Checkout must use the exact quoted unit price shown to the customer.
```

### BR-021 — Money is stored as binary floating point

**Category:** Architecture · **Severity:** Medium · **Classification:** Improvement suggestion

**Locations:** [BookShoppingCart.Models/Models/Book.cs:20](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/Book.cs:20>); [BookShoppingCart.Models/Models/CartDetail.cs:17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/CartDetail.cs:17>); [BookShoppingCart.Models/Models/OrderDetail.cs:17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/OrderDetail.cs:17>); [BookShoppingCartMvcUI/Controllers/CartController.cs:39-40](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/CartController.cs:39>).

**Issue and why it matters:** Prices and unit prices are double/SQL float, then summed as double and cast to decimal. Binary floating point cannot represent many currency fractions exactly, and no consistent rounding policy is defined. No particular incorrect posted charge was measured; this is a financial correctness design risk.

**Recommended fix:** Migrate monetary values, DTOs and calculations to decimal with explicit precision and rounding. Persist subtotal, discount, shipping and grand total on orders. Plan conversion and reconciliation for existing float data.

**Suggested change / implementation sketch:**

```csharp
public decimal Price { get; set; }
// In OnModelCreating (after base.OnModelCreating):
modelBuilder.Entity<Book>().Property(b => b.Price).HasPrecision(18, 2);
modelBuilder.Entity<OrderDetail>().Property(d => d.UnitPrice).HasPrecision(18, 2);
var amount = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
```

### BR-022 — Book deletion destroys historical order lines

**Category:** Data integrity · **Severity:** High · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Services/BookService.cs:128-150](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/BookService.cs:128>); [BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:311-318](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:311>); [BookShoppingCart.Data/Migrations/ApplicationDbContextModelSnapshot.cs:493-497](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/ApplicationDbContextModelSnapshot.cs:493>).

**Issue and why it matters:** DeleteBook performs a hard delete. The OrderDetail->Book foreign key cascades deletion, so deleting a previously purchased book removes its historical order details. An order can remain with no lines and lose its financial/audit information. The current service does not guard against order references.

**Recommended fix:** Archive books instead of deleting those referenced by orders. Use Restrict/NoAction for historical references and snapshot book name and price in order lines. Review genre->book cascade too; the current genre service guard helps but is not a substitute for database protection.

**Suggested change / implementation sketch:**

```csharp
modelBuilder.Entity<OrderDetail>()
    .HasOne(d => d.Book).WithMany(b => b.OrderDetail)
    .HasForeignKey(d => d.BookId).OnDelete(DeleteBehavior.Restrict);
// Add Book.IsArchived and exclude archived books from public catalog queries.
```

### BR-023 — The add-genre error path passes the wrong model type

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/GenreController.cs:62-67](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/GenreController.cs:62>); [BookShoppingCartMvcUI/Controllers/GenreController.cs:87-95](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/GenreController.cs:87>); [BookShoppingCartMvcUI/Views/Genre/AddGenre.cshtml:2](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Genre/AddGenre.cshtml:2>).

**Issue and why it matters:** The AddGenre view expects GenreDTO, but the POST action accepts Genre and returns View(genre) for validation or API failures. Those error paths throw a ViewDataDictionary model incompatibility exception instead of showing validation. The initial GET has no model and is not the same failure.

**Recommended fix:** Use GenreDTO consistently in the MVC action and view; map to the API request DTO explicitly.

**Suggested change / implementation sketch:**

```csharp
[HttpPost]
public async Task<IActionResult> AddGenre(GenreDTO dto)
{
    if (!ModelState.IsValid) return View(dto);
    using var response = await _httpClient.PostAsJsonAsync(
        $"{_baseUrl}Genre/AddGenre", dto);
    if (response.IsSuccessStatusCode) return RedirectToAction(nameof(Index));
    ModelState.AddModelError("", "Unable to save genre.");
    return View(dto);
}
```

### BR-024 — Missing book details return 500 instead of 404

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Services/BookService.cs:63-68](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/BookService.cs:63>); [BookShoppingCartMvcUI/Controllers/BookController.cs:193-201](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/BookController.cs:193>).

**Issue and why it matters:** GetBookById throws KeyNotFoundException for a missing book. BookDetails awaits it before checking for null, so the NotFound branch is unreachable for the service's missing-record behavior. Deleted or fabricated book links become generic 500 responses.

**Recommended fix:** Make the lookup contract consistently nullable, or translate KeyNotFoundException to 404 at the boundary. Treat invalid identifiers as 400/404 rather than unexpected server faults.

**Suggested change / implementation sketch:**

```csharp
try
{
    return View(await _bookFacade.GetBookById(id));
}
catch (KeyNotFoundException)
{
    return NotFound();
}
```

### BR-025 — Order and stock error fallbacks have incompatible view models

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/StockController.cs:35-40](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/StockController.cs:35>); [BookShoppingCartMvcUI/Controllers/UserOrderController.cs:40-47](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/UserOrderController.cs:40>); [BookShoppingCartMvcUI/Views/Stock/Index.cshtml:1](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Stock/Index.cshtml:1>); [BookShoppingCartMvcUI/Views/UserOrder/UserOrders.cshtml:1](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/UserOrder/UserOrders.cshtml:1>).

**Issue and why it matters:** Stock.Index expects IEnumerable<StockDisplayModel> but its catch returns List<StockDTO>. UserOrders expects IEnumerable<Order> but its catch returns List<object>. Whenever an upstream operation fails, rendering the fallback throws another exception and hides the intended error notification.

**Recommended fix:** Return the correctly typed empty sequence, or render a dedicated error model/page.

**Suggested change / implementation sketch:**

```csharp
// StockController:
return View(Enumerable.Empty<StockDisplayModel>());
// UserOrderController:
return View(Enumerable.Empty<Order>());
```

### BR-026 — Book forms lose genre options on validation or save failure

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/BookController.cs:69-76](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/BookController.cs:69>); [BookShoppingCartMvcUI/Controllers/BookController.cs:96-97](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/BookController.cs:96>); [BookShoppingCartMvcUI/Controllers/BookController.cs:138-165](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/BookController.cs:138>); [BookShoppingCartMvcUI/Views/Book/AddBook.cshtml:30](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Book/AddBook.cshtml:30>).

**Issue and why it matters:** Only GET handlers populate BookDTO.GenreList. POST-bound DTOs do not include that list, and every POST error path returns the DTO without rebuilding it. A failed submission therefore renders a dropdown with no available genres and obstructs correction/resubmission.

**Recommended fix:** Use one helper that populates the genre list and call it before every return View(dto), including catch paths. Map expected domain validation failures into ModelState.

**Suggested change / implementation sketch:**

```csharp
dto.GenreList = (await _genreService.GetGenres()).Select(g =>
    new SelectListItem(g.GenreName, g.Id.ToString(), g.Id == dto.GenreId));
return View(dto);
```

### BR-027 — A book without a Stock record crashes cart rendering

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Views/Cart/GetUserCart.cshtml:51](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Cart/GetUserCart.cshtml:51>); [BookShoppingCart.Data/Repositories/CartRepository.cs:54-65](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:54>); [BookShoppingCart.Data/Data/DbSeeder.cs:88-112](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:88>).

**Issue and why it matters:** Book.Stock is optional in practice: new/seeded books are created without stock, and AddItem does not require stock. A direct request can add such a book to an existing cart. The Razor view unconditionally dereferences item.Book.Stock.Quantity. Rendering occurs after the controller returns, so the controller's try/catch does not catch this view exception.

**Recommended fix:** Treat missing stock as zero for display, and reject unavailable items server-side. Keep the UI guard as a usability measure, not an inventory authorization rule.

**Suggested change / implementation sketch:**

```csharp
@{ var available = item.Book.Stock?.Quantity ?? 0; }
@if (item.Quantity < available)
{
    // Render the increment control.
}
```

### BR-028 — Anonymous shoppers never receive Add to Cart click handlers

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/wwwroot/js/bookCart.js:3-8](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/wwwroot/js/bookCart.js:3>); [BookShoppingCartMvcUI/wwwroot/js/bookCart.js:17-19](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/wwwroot/js/bookCart.js:17>); [BookShoppingCartMvcUI/Controllers/CartController.cs:10](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/CartController.cs:10>).

**Issue and why it matters:** On page load, the script first fetches an authorized endpoint. An anonymous fetch follows the cookie challenge redirect to the HTML login page; parsing response.json fails (or an unauthorized response is thrown). Handler registration is below that await, so no Add to Cart buttons receive handlers. The intended redirect-to-login code inside the click handler is never reached.

**Recommended fix:** Attach handlers independently of loading cart membership. Skip membership lookup for anonymous users or tolerate its failure, and explicitly handle authentication redirects/401 in fetch code.

**Suggested change / implementation sketch:**

```csharp
const signedIn = !!document.getElementById('username');
let cartBookIds = [];
if (signedIn) {
    try { cartBookIds = await loadCartBookIds(); }
    catch { showCartLoadError(); }
}
// Always attach click handlers; redirect anonymous users on click.
```

### BR-029 — Search terms are interpolated into API URLs without encoding

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Controllers/HomeController.cs:36-37](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/HomeController.cs:36>).

**Issue and why it matters:** A search such as C# places a fragment delimiter in the URL, while & and other query delimiters alter the downstream query parameters. Consequently the API receives a truncated or different search/filter. The client controls only a query value, not the server host; this is a query-encoding defect, not a demonstrated SSRF.

**Recommended fix:** Construct the query using QueryHelpers.AddQueryString or equivalent encoding.

**Suggested change / implementation sketch:**

```csharp
var url = QueryHelpers.AddQueryString($"{_baseUrl}Home/GetBooks",
    new Dictionary<string, string?>
    {
        ["sTerm"] = sTerm,
        ["genreId"] = genreId.ToString(CultureInfo.InvariantCulture)
    });
```

### BR-030 — Unvalidated page query values throw during Razor rendering

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Views/Home/Index.cshtml:4-8](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Home/Index.cshtml:4>).

**Issue and why it matters:** int.Parse reads an arbitrary page query value in the view. ?page=abc, an empty multi-value form or an out-of-range integer can produce a FormatException/OverflowException and a 500 page; negative or excessively large values also produce incorrect/empty paging behavior.

**Recommended fix:** Parse and validate paging in the controller/request model, clamp or reject invalid ranges, and pass a typed pagination model to the view.

**Suggested change / implementation sketch:**

```csharp
if (!int.TryParse(Request.Query["page"].ToString(), out var page) || page < 1)
    page = 1;
// After total count is known:
page = Math.Min(page, Math.Max(1, totalPages));
```

### BR-031 — Pagination drops the selected search and genre

**Category:** Frontend · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Views/Home/Index.cshtml:82-95](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Home/Index.cshtml:82>).

**Issue and why it matters:** Previous, numbered and Next links contain only ?page=..., discarding sTerm and genreId from the current request. Moving to page 2 after a search silently returns the unfiltered catalog.

**Recommended fix:** Generate links with all current filter values. Reset page to 1 when filters change.

**Suggested change / implementation sketch:**

```html
<a asp-action="Index" asp-route-page="@i"
   asp-route-sTerm="@Model.STerm" asp-route-genreId="@Model.GenreId">@i</a>
```

### BR-032 — Genre writes leave two read caches stale

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Repositories/HomeRepository.cs:24-30](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/HomeRepository.cs:24>); [BookShoppingCart.Business/Services/GenreService.cs:48-87](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/GenreService.cs:48>); [BookShoppingCart.Business/Services/BookService.cs:31-43](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/BookService.cs:31>).

**Issue and why it matters:** Home genres are cached for 30 minutes in the API process, but genre create/update/delete never invalidates genres_cache. BookService also caches Books with Genre entities for five minutes in MVC, while genre writes happen through another process. Renames/deletes can therefore leave filters and admin book genre labels stale.

**Recommended fix:** Invalidate relevant read models on successful genre writes. Use a shared version/event mechanism if hosts must agree promptly; process-local Remove in the API cannot invalidate MVC memory. Prefer immutable cached DTOs.

**Suggested change / implementation sketch:**

```csharp
// Immediate local fix in the API write path:
await _genreRepo.UpdateAsync(genre);
_cache.Remove("genres_cache");
// Also invalidate/version the MVC book read model across the process boundary.
```

### BR-033 — Clones share images that deletion treats as exclusively owned

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Models/Models/Book.cs:34-42](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/Book.cs:34>); [BookShoppingCart.Business/Facades/BookFacade.cs:101-104](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Facades/BookFacade.cs:101>); [BookShoppingCart.Business/Services/BookService.cs:148-150](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/BookService.cs:148>).

**Issue and why it matters:** Book.Clone copies Image unchanged. Deleting either book, or replacing its image, deletes the shared physical file without checking other books. The remaining clone/original then has a broken image reference.

**Recommended fix:** Copy the asset when cloning, or use shared asset records/reference checks and delete only when no book references the image.

**Suggested change / implementation sketch:**

```csharp
// Before deleting a shared image, after committing the book change:
var stillUsed = await db.Books.AnyAsync(b => b.Image == imageName);
if (!stillUsed)
    fileService.DeleteFile(imageName);
// A dedicated asset model is safer for concurrent operations.
```

### BR-034 — Failed book saves leave uploaded images orphaned

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Facades/BookFacade.cs:39-57](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Facades/BookFacade.cs:39>); [BookShoppingCart.Business/Facades/BookFacade.cs:64-83](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Facades/BookFacade.cs:64>).

**Issue and why it matters:** The facade saves a file before service validation/database persistence. Invalid price, missing genre, length errors or database failures leave the new file behind. Repeated failures accumulate storage. Deleting an old image after persistence also needs retry behavior if filesystem cleanup fails.

**Recommended fix:** Track newly written assets and compensate on database failure; make deletion retryable. For robust storage, stage uploads and finalize them after persistence, with background cleanup for abandoned assets.

**Suggested change / implementation sketch:**

```csharp
string? uploaded = null;
try
{
    uploaded = await _fileService.SaveFile(file, extensions);
    book.Image = uploaded;
    await _bookService.AddBook(book);
}
catch
{
    if (uploaded != null) _fileService.DeleteFile(uploaded);
    throw;
}
```

### BR-035 — Genre duplicate prevention is incomplete and race-prone

**Category:** Bug · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Business/Services/GenreService.cs:40-48](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/GenreService.cs:40>); [BookShoppingCart.Business/Services/GenreService.cs:59-69](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/GenreService.cs:59>); [BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:52-64](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Migrations/20250331111632_InitialCreate.cs:52>).

**Issue and why it matters:** AddGenre checks all existing names in memory but UpdateGenre permits renaming to an existing name. Neither trims/normalizes consistently, and the database has no unique name constraint, so simultaneous adds can pass the check. The application's stated duplicate rule is not enforced consistently.

**Recommended fix:** Normalize genre names, query existence rather than loading all genres, apply the rule to updates excluding the current ID, and enforce a unique database key after existing duplicates are reconciled.

**Suggested change / implementation sketch:**

```csharp
var normalized = genre.GenreName.Trim().ToUpperInvariant();
if (await db.Genres.AnyAsync(g =>
    g.NormalizedName == normalized && g.Id != genre.Id))
    throw new InvalidOperationException("Genre already exists.");
// Add NormalizedName and a unique index through a migration.
```

### BR-036 — Expected failures are swallowed or translated into misleading outcomes

**Category:** Architecture · **Severity:** Medium · **Classification:** Verified defect

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:237-241](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:237>); [BookShoppingCart.Data/Data/DbSeeder.cs:66-68](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:66>); [BookShoppingCartMvcUI/Controllers/AdminOperationsController.cs:34-37](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/AdminOperationsController.cs:34>); [BookShoppingCartMvcUI/Controllers/CartController.cs:122-125](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/CartController.cs:122>); [BookShoppingCartMvcUI/Middleware/ExceptionMiddleware.cs:25-38](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Middleware/ExceptionMiddleware.cs:25>).

**Issue and why it matters:** Checkout discards exception details and returns false, startup seeding catches errors internally so its caller sees success, payment toggling has an empty catch, and cart count returns HTTP 200/0 on failure. The middleware maps every uncaught error to 500 JSON even for browser pages and writes response headers without checking HasStarted. These paths hide actionable failure causes or produce a second response error.

**Recommended fix:** Use typed domain results for expected validation/conflict/not-found cases, log unexpected exceptions once with correlation, propagate initialization failure, and use appropriate HTML/ProblemDetails boundaries. Never rewrite an already-started response.

**Suggested change / implementation sketch:**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled request failure {TraceId}", context.TraceIdentifier);
    if (context.Response.HasStarted) throw;
    context.Response.Clear();
    // Render an HTML error page or a sanitized ProblemDetails response as appropriate.
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
}
```

### BR-037 — Password recovery and email change have no real delivery service

**Category:** Integration · **Severity:** Medium · **Classification:** Verified configuration gap

**Locations:** [BookShoppingCartMvcUI/Program.cs:24-30](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Program.cs:24>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs:70-75](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs:70>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Manage/Email.cshtml.cs:130-148](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Manage/Email.cshtml.cs:130>).

**Issue and why it matters:** The code calls IEmailSender for recovery, confirmation and email change, but there is no application sender implementation/registration or delivery configuration. Identity UI provides a fallback no-op sender; that is not a delivery integration. Users can be told to check their inbox without an email being sent.

**Recommended fix:** Register a production email adapter, validate required delivery configuration at startup, and test recovery/email change end to end without exposing tokens publicly. Keep delivery failures observable.

**Suggested change / implementation sketch:**

```csharp
builder.Services.AddTransient<IEmailSender, TransactionalEmailSender>();
// TransactionalEmailSender is a new provider adapter using secret-backed configuration.
// Verify delivered callbacks in an integration environment.
```

### BR-038 — Login UI asks for a username but accepts only email; recovery link is wrong

**Category:** Frontend · **Severity:** Low · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml:21](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml:21>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml:41](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml:41>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:60-62](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Login.cshtml.cs:60>).

**Issue and why it matters:** The login field placeholder says Username, while Input.Email has EmailAddress validation and lookup uses FindByEmailAsync. A username selected at registration is therefore rejected. The Forgot password link targets ./ rather than ./ForgotPassword, so it does not reach the implemented recovery form.

**Recommended fix:** Label the field Email and point recovery to the actual page, or deliberately support username login with a corresponding validation/lookup contract.

**Suggested change / implementation sketch:**

```html
<input asp-for="Input.Email" placeholder="Email" autocomplete="username" />
<a asp-page="./ForgotPassword">Forgot password?</a>
```

### BR-039 — Client validation scripts load without jQuery on several forms

**Category:** Frontend · **Severity:** Low · **Classification:** Verified defect

**Locations:** [BookShoppingCartMvcUI/Views/Shared/_Layout.cshtml:36-40](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Shared/_Layout.cshtml:36>); [BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml:81-83](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/Account/Register.cshtml:81>); [BookShoppingCartMvcUI/Areas/Identity/Pages/_ValidationScriptsPartial.cshtml:2-14](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Areas/Identity/Pages/_ValidationScriptsPartial.cshtml:2>).

**Issue and why it matters:** The shared layout does not load jQuery. Register and several scaffolded Identity forms load the validation partial, whose scripts require jQuery, but only Login explicitly loads jQuery. Client validation fails on affected pages even though server validation still runs. Checkout and admin book forms also omit validation-script sections.

**Recommended fix:** Load one pinned jQuery dependency before validation scripts for pages that use them, and include validation partials on intended forms. Preserve server-side validation as authoritative.

**Suggested change / implementation sketch:**

```csharp
// In the common layout, before RenderSectionAsync:
// <script src="~/lib/jquery/dist/jquery.min.js"></script>
// In each validated form's Scripts section:
// <partial name="_ValidationScriptsPartial" />
```

### BR-040 — Deployment defaults are tied to a developer machine and disable DB trust checks

**Category:** Configuration · **Severity:** Medium · **Classification:** Verified configuration risk

**Locations:** [BookShoppingCartMvcUI/appsettings.json:3-6](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/appsettings.json:3>); [BookShoppingCart.WebAPI/appsettings.json:3](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/appsettings.json:3>); [BookShoppingCartMvcUI/Dockerfile:6-7](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Dockerfile:6>).

**Issue and why it matters:** The default connection targets a named Windows SQL instance with Integrated Security and Encrypt=False/TrustServerCertificate=True. The MVC API base URL is localhost:7149. These defaults do not describe the separately deployed Azure services or a Linux container. Production overrides may exist outside this repository; their presence and effective values were not verified.

**Recommended fix:** Move development endpoints into Development settings, require explicit production endpoints/secrets, use validated SQL TLS in production, and validate options on startup. Run the MVC/API hosts against the same intended database.

**Suggested change / implementation sketch:**

```csharp
// Deployment overrides:
// ApiSettings__BaseUrl=https://<api-host>/api/
// ConnectionStrings__DefaultConnection=<production connection with Encrypt=True;TrustServerCertificate=False>
// Add options validation requiring an absolute HTTPS API URL outside Development.
```

### BR-041 — Catalog and order reads materialize entire datasets

**Category:** Performance · **Severity:** Medium · **Classification:** Verified inefficiency

**Locations:** [BookShoppingCart.Data/Repositories/HomeRepository.cs:37-73](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/HomeRepository.cs:37>); [BookShoppingCart.Business/Services/HomeService.cs:19-21](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/HomeService.cs:19>); [BookShoppingCartMvcUI/Views/Home/Index.cshtml:6-8](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Home/Index.cshtml:6>); [BookShoppingCart.Data/Repositories/UserOrderRepository.cs:77-98](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/UserOrderRepository.cs:77>); [BookShoppingCart.Data/Repositories/BookRepository.cs:15-17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/BookRepository.cs:15>).

**Issue and why it matters:** HomeRepository exposes page/pageSize but never applies them. The API transfers every matching book and Razor pages in memory. Admin/user order queries also load all orders, full details and related entities with tracking. Data volume and response memory grow with catalog/history size. There is no measured production latency claim.

**Recommended fix:** Page at the database with stable ordering and bounded page size, return a count plus projected DTOs, and use AsNoTracking for read-only views. Add indexes based on resulting query plans, especially order UserId/date.

**Suggested change / implementation sketch:**

```csharp
var query = db.Books.AsNoTracking().Where(/* validated filters */);
var count = await query.CountAsync(ct);
var items = await query.OrderBy(b => b.Id)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(b => new BookListDTO { Id = b.Id, BookName = b.BookName, Price = b.Price })
    .ToListAsync(ct);
```

### BR-042 — Checkout queries stock separately for every cart line

**Category:** Performance · **Severity:** Medium · **Classification:** Verified inefficiency

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:201-214](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:201>).

**Issue and why it matters:** The foreach awaits a stock query for each line while holding an open transaction, in addition to earlier cart/status/order operations. This adds one round trip per line and lengthens lock duration. Fixing this must preserve concurrency protection from issue 14; simply preloading stocks does not make deductions safe.

**Recommended fix:** Fetch required reference data in bounded batches and use concurrency-safe stock mutations. Bound cart size; measure generated commands rather than parallelizing operations on the same DbContext.

**Suggested change / implementation sketch:**

```csharp
var bookIds = cartDetails.Select(d => d.BookId).Distinct().ToArray();
var stocks = await _db.Stocks.AsNoTracking()
    .Where(s => bookIds.Contains(s.BookId))
    .ToDictionaryAsync(s => s.BookId);
// Apply atomic conditional deductions inside the transaction (issue 14).
```

### BR-043 — All MVC shoppers share a ten-per-minute catalog rate bucket

**Category:** Performance · **Severity:** High · **Classification:** Verified integration bottleneck

**Locations:** [BookShoppingCart.WebAPI/Program.cs:38-46](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Program.cs:38>); [BookShoppingCart.WebAPI/Controllers/Endpoints/Home/GetBooksEndpoint.cs:25-27](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Home/GetBooksEndpoint.cs:25>); [BookShoppingCartMvcUI/Controllers/HomeController.cs:36-37](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/HomeController.cs:36>).

**Issue and why it matters:** The books API limiter partitions on its caller's remote IP. Catalog requests originate from the MVC server, so users of that server share its ten-requests-per-minute quota, rather than individual shopper quotas. After queue capacity is exhausted, HomeController turns non-success API responses into a failed/empty catalog page. Multiple MVC instances each have their own API-visible IP behavior.

**Recommended fix:** Use a suitably sized authenticated service-client quota between MVC/API, and apply shopper abuse controls at the MVC ingress. Do not trust an arbitrary forwarded-user header. Return 429 and Retry-After when rejecting and surface a retry message.

**Suggested change / implementation sketch:**

```csharp
options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
// Partition the service quota by validated client identity and size it for aggregate load.
// Apply per-user/IP browser request policy at MVC, where the user context is known.
```

### BR-044 — Legacy MVC5 and unrelated runtime/test packages pollute the dependency graph

**Category:** Architecture · **Severity:** Medium · **Classification:** Verified compatibility warnings; cleanup suggestion

**Locations:** [BookShoppingCart.Business/BookShoppingCart.Business.csproj:7-11](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/BookShoppingCart.Business.csproj:7>); [BookShoppingCart.Models/BookShoppingCart.Models.csproj:6-20](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/BookShoppingCart.Models.csproj:6>); [BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj:18-30](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/BookShoppingCartMvcUI.csproj:18>).

**Issue and why it matters:** Microsoft.AspNet.Mvc 5.3.0 targets .NET Framework and brings Razor/WebPages/Web.Infrastructure dependencies into net9.0 projects. The build emits NU1701 warnings for these packages. Models also references EF/Identity/UI/scaffolding; production MVC references Moq/MSTest. No production call requiring the old System.Web MVC stack was found.

**Recommended fix:** Remove obsolete/unneeded references after checking compilation, keep tests/scaffolding tooling in the proper project/private assets, and align EF/ASP.NET package versions. ASP.NET Core MVC comes from the ASP.NET Core framework, not Microsoft.AspNet.Mvc.

**Suggested change / implementation sketch:**

```html
<!-- Remove Microsoft.AspNet.Mvc and unused test/scaffolding packages. -->
<!-- Keep EF/provider packages in Data and test frameworks in Tests. -->
<!-- Use centrally managed compatible 9.0.x package versions where appropriate. -->
```

### BR-045 — Two API projects outside the solution contain unresolved application types

**Category:** Build · **Severity:** Medium · **Classification:** Verified source/build configuration defect

**Locations:** [BookShoppingCart-Web.API/Program.cs:1-12](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart-Web.API/Program.cs:1>); [BookShoppingCat-WebAPI/Program.cs:1-9](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCat-WebAPI/Program.cs:1>); [BookShoppingCat-WebAPI/BookShoppingCat-WebAPI.csproj:10-12](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCat-WebAPI/BookShoppingCat-WebAPI.csproj:10>); [BookShoppingCartMvc.sln:5-18](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvc.sln:5>).

**Issue and why it matters:** BookShoppingCart-Web.API imports BookShoppingCartMvcUI.Data and uses ApplicationDbContext, but that type lives in BookShoppingCart.Data.Data. BookShoppingCat-WebAPI targets net8.0, imports EF and uses the context, but references only Swashbuckle and no data project. Both are absent from the solution, hiding their broken builds from normal solution validation. The active API is BookShoppingCart.WebAPI.

**Recommended fix:** Determine whether these are obsolete prototypes. Remove/archive them if unused, or deliberately fix their imports/references/target frameworks and include them in validation. Do not point a net8 project at a net9-only data assembly.

**Suggested change / implementation sketch:**

```csharp
// If retaining the net9 prototype:
using BookShoppingCart.Data.Data;
// Add/retain the correct Data ProjectReference.
// For the net8 prototype, first choose a compatible framework/dependency strategy.
```

### BR-046 — Tests omit the persistence, endpoint and checkout behaviors most at risk

**Category:** Testing · **Severity:** High · **Classification:** Verified coverage gap

**Locations:** [BookShoppingCart.Tests/Tests/CartServiceTests.cs:14-17](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Tests/Tests/CartServiceTests.cs:14>); [BookShoppingCart.Tests/Tests/CartServiceTests.cs:122-135](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Tests/Tests/CartServiceTests.cs:122>); [BookShoppingCart.Tests/Tests/GenreServiceTests.cs:174-185](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Tests/Tests/GenreServiceTests.cs:174>); [BookShoppingCart.Tests/Tests/BookServiceTests.cs:91-110](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Tests/Tests/BookServiceTests.cs:91>).

**Issue and why it matters:** The test project has three service test files using mocked repositories. It cannot detect first-cart FK failure, missing seed statuses, concurrency, cascade deletion, anonymous API writes, antiforgery or Razor model mismatches. Checkout only tests an absent payment method. The genre delete test does not exercise HasBooks=true, and AddBook_AddsAndClearsCache does not seed/assert cache removal.

**Recommended fix:** Add SQL Server integration tests for actual relational/concurrency behavior, authenticated/anonymous endpoint tests, and checkout/UI workflow tests. Inject payment interfaces so success/failure/idempotency can be tested without real payments. This is a release assurance gap, not a claim that existing assertions fail.

**Suggested change / implementation sketch:**

```csharp
// Proposed integration scenarios:
// NewUser_FirstAdd_PersistsCartAndLine
// TwoUsers_LastStockUnit_OnlyOneCheckoutSucceeds
// Anonymous_GenreMutation_IsRejected
// DeletePurchasedBook_PreservesOrderHistory
// Checkout_UsesServerQuote_AndSameKeyDoesNotRepay
// InvalidGenrePost_RendersValidationWithoutModelTypeException
```

### BR-047 — Application logic is coupled to HTTP context, concrete storage and static factories

**Category:** Architecture · **Severity:** Low · **Classification:** Improvement suggestion

**Locations:** [BookShoppingCart.Data/Repositories/CartRepository.cs:18-26](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/CartRepository.cs:18>); [BookShoppingCart.Data/Repositories/UserOrderRepository.cs:17-27](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/UserOrderRepository.cs:17>); [BookShoppingCart.Business/Services/StockService.cs:12-16](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/StockService.cs:12>); [BookShoppingCart.Business/Services/CartService.cs:66](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Services/CartService.cs:66>); [BookShoppingCart.Business/Logging/AppLogger.cs:9-25](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/Logging/AppLogger.cs:9>).

**Issue and why it matters:** Repositories derive the current user from IHttpContextAccessor/UserManager, StockService depends on concrete StockRepository despite an existing interface, and payment construction/logging bypass DI. This makes use cases harder to test or reuse outside HTTP, and contributed to the missing checkout coverage. Scoped repository/DbContext lifetimes themselves are appropriate; no singleton DbContext capture was found.

**Recommended fix:** Pass authenticated user IDs into use cases, authorize operations at explicit boundaries, implement/use IStockRepository, inject payment adapters and ILogger<T>. Consolidate duplicate payment abstractions and redundant scoped proxy caching.

**Suggested change / implementation sketch:**

```csharp
public StockService(IStockRepository stockRepository)
{
    _stockRepository = stockRepository;
}
// Register AddScoped<IStockRepository, StockRepository>();
// Make StockRepository implement IStockRepository.
// Inject IPaymentService/selector into the checkout service.
```

### BR-048 — Nullable reference annotations are disabled in core libraries

**Category:** Coding standard · **Severity:** Low · **Classification:** Improvement suggestion with verified compiler warnings

**Locations:** [BookShoppingCart.Models/BookShoppingCart.Models.csproj:2-4](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/BookShoppingCart.Models.csproj:2>); [BookShoppingCart.Data/BookShoppingCart.Data.csproj:2-4](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/BookShoppingCart.Data.csproj:2>); [BookShoppingCart.Business/BookShoppingCart.Business.csproj:2-4](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Business/BookShoppingCart.Business.csproj:2>); [BookShoppingCart.Models/Models/ShoppingCart.cs:20](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Models/Models/ShoppingCart.cs:20>).

**Issue and why it matters:** Core projects omit Nullable=enable while using T? in contracts, producing CS8632 warnings and leaving reference-state assumptions unchecked. Collections such as CartDetails are not initialized; HTTP context access assumes a request exists. These are maintainability/null-safety risks beyond the concrete view crash already recorded in issue 27.

**Recommended fix:** Enable nullable checking incrementally, initialize collections, accurately annotate optional navigations/returns, and guard context-dependent boundaries. Avoid blanket null-forgiving operators.

**Suggested change / implementation sketch:**

```html
<Nullable>enable</Nullable>
// Model initialization:
public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
// Optional one-to-one navigation:
public Stock? Stock { get; set; }
```

### BR-049 — Generic repository updates depend on a runtime-only Id contract

**Category:** Coding standard · **Severity:** Low · **Classification:** Improvement suggestion

**Locations:** [BookShoppingCart.Data/Repositories/BaseRepository.cs:12](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/BaseRepository.cs:12>); [BookShoppingCart.Data/Repositories/BaseRepository.cs:34](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Repositories/BaseRepository.cs:34>).

**Issue and why it matters:** BaseRepository<T> permits any class, but UpdateAsync assumes ((dynamic)entity).Id exists and is a compatible key. Current Book/Genre callers have an int Id, so this is not a demonstrated current endpoint failure. A future registration can compile and then fail at runtime; dynamic also obscures the repository's actual contract.

**Recommended fix:** Use an explicit typed entity/key contract or remove the generic update method in favor of targeted updates of tracked entities.

**Suggested change / implementation sketch:**

```csharp
public interface IEntity { int Id { get; } }
public class BaseRepository<T> where T : class, IEntity
{
    // FindAsync(entity.Id), with no dynamic dispatch.
}
```

### BR-050 — Contact and subscription controls have no submit implementation

**Category:** Frontend · **Severity:** Low · **Classification:** Verified feature gap

**Locations:** [BookShoppingCartMvcUI/Views/FrontPage/Contact.cshtml:60-77](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/FrontPage/Contact.cshtml:60>); [BookShoppingCartMvcUI/Views/Shared/_Footer.cshtml:24-29](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Views/Shared/_Footer.cshtml:24>); [BookShoppingCartMvcUI/Controllers/FrontPageController.cs:20-23](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCartMvcUI/Controllers/FrontPageController.cs:20>).

**Issue and why it matters:** The Contact form has no POST action, no named fields and no handler; submitting it only navigates/reloads without delivering a message. The subscription control is a div with an input/button and has no handler or backend. These controls imply actions the application does not perform.

**Recommended fix:** Either implement validated POST flows with delivery/storage, antiforgery, rate limits and visible success/error states, or remove/disable the controls until the features exist. No unsolicited email was sent during this review.

**Suggested change / implementation sketch:**

```html
<form asp-controller="FrontPage" asp-action="Contact" method="post">
    <input name="Name" required />
    <input name="Email" type="email" required />
    <textarea name="Message" required></textarea>
    <button type="submit">Send Message</button>
</form>
// Requires a new validated POST handler and delivery implementation.
```

### BR-051 — Seeded books assume genre identity values are always 1 through 6

**Category:** Bug · **Severity:** Medium · **Classification:** Verified edge-case defect

**Locations:** [BookShoppingCart.Data/Data/DbSeeder.cs:56-63](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:56>); [BookShoppingCart.Data/Data/DbSeeder.cs:92-108](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.Data/Data/DbSeeder.cs:92>).

**Issue and why it matters:** When Books is empty, SeedBooksAsync uses fixed GenreId values. If genres already exist with other identities (for example, genres were deleted/recreated), inserts fail on FK constraints or map books to the wrong genres. The seeder does not look up the named genres it intends to use. Identity creation/update results are also ignored, making partial initialization harder to detect (see issue 36).

**Recommended fix:** Resolve the intended genre keys by stable normalized name/code after ensuring those genres exist. Seed books with returned keys, and fail visibly on missing prerequisites.

**Suggested change / implementation sketch:**

```csharp
var genreIds = await context.Genres
    .ToDictionaryAsync(g => g.GenreName, g => g.Id);
var book = new Book
{
    BookName = "Clean Code", AuthorName = "Robert C. Martin",
    Price = 19.99, GenreId = genreIds["Programming"]
};
```

### BR-052 — Genre API validation is not wired to its actual request types

**Category:** Security · **Severity:** Medium · **Classification:** Verified validation gap

**Locations:** [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:20-24](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/AddGenreEndpoint.cs:20>); [BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/UpdateGenreEndpoint.cs:20-24](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Genre/UpdateGenreEndpoint.cs:20>); [BookShoppingCart.WebAPI/Controllers/Endpoints/Validations/BookRequestValidator.cs:4](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Controllers/Endpoints/Validations/BookRequestValidator.cs:4>); [BookShoppingCart.WebAPI/Program.cs:98](<C:/Users/Srinivas Vandamsetty/source/repos/BookShoppingCart-Updated/BookShoppingCart.WebAPI/Program.cs:98>).

**Issue and why it matters:** The only validator targets BookRequest, which no implemented endpoint accepts. Genre endpoints accept Genre and check ValidationFailed, but have no matching validator; DataAnnotations support is not enabled in UseFastEndpoints. Service validation checks whitespace but not maximum length, and invalid IDs/duplicate errors often escape as 500 instead of a consistent validation response. SQL constraints prevent some bad writes but do not provide a usable API contract.

**Recommended fix:** Add FastEndpoints validators for the actual scalar request DTOs and positive route IDs. Enforce lengths and map expected business failures to 400/404/409 responses. Retain database constraints.

**Suggested change / implementation sketch:**

```csharp
public sealed class CreateGenreValidator : Validator<CreateGenreRequest>
{
    public CreateGenreValidator()
    {
        RuleFor(x => x.GenreName).NotEmpty().MaximumLength(40);
    }
}
// Validate Id > 0 on update/delete requests and map duplicate conflicts to 409.
```

## Recommended refactoring and architecture

Make these incremental changes after the high-impact defects are fixed; a wholesale rewrite is unnecessary.

1. **One checkout application service owns the use case.** Accept the authenticated user ID and an input DTO with address, payment choice, coupon/shipping choice and idempotency key. It creates an authoritative quote, reserves inventory, persists an order/payment attempt and coordinates provider completion/reconciliation. Repositories should not derive users from HttpContext or independently decide payment/pricing.
2. **Explicit domain snapshots and state transitions.** Use decimal money; persist quoted unit prices, title/author snapshots, subtotal, discount, shipping, final total and provider reference. Model PendingPayment/Captured/Failed/CashAwaitingCollection separately from fulfillment statuses. Define permitted cancellation/return/refund transitions with stock/payment consequences instead of arbitrary status replacement and payment toggling. The existing code does not define those business rules, so exact transitions require product decisions.
3. **Contracts at every boundary.** Separate EF entities, API requests/responses and Razor view models. Centralize validation with framework-appropriate validators, normalize business keys and reinforce invariants with constraints. Use typed paginated results and typed cart summaries instead of ViewBag monetary fields.
4. **Lean dependency structure.** Domain/value types should avoid MVC, IFormFile, Identity UI and EF tooling dependencies. Keep transport/upload view models in UI/contracts, infrastructure adapters in Data/infrastructure, and orchestration interfaces in the application layer. Implement IStockRepository, inject payments/pricing/current-user abstractions and ILogger<T>, and remove redundant proxy/cache/factory demonstrations only after verifying references.
5. **Consistent operational boundaries.** Use structured logs and trace IDs, typed expected errors, sanitized ProblemDetails for APIs and suitable HTML error pages for browsers. Propagate CancellationToken from endpoint/controller to EF/HttpClient; avoid parallel EF work on a single DbContext. Make schema migration a controlled deployment step and startup readiness fail clearly when required initialization is incomplete.
6. **Cache and asset ownership policies.** Cache immutable read DTOs with explicit invalidation/versioning across MVC/API hosts. Unused GenreCache output-cache policy currently has no endpoint attachment; either attach it deliberately with invalidation semantics or remove the unused configuration. Give uploaded images explicit ownership/reference rules and retryable cleanup; external object storage is an option if multi-instance deployments require shared durable assets.
7. **Remove misleading remnants.** Consolidate the duplicate IPaymentService/IPaymentsService families, empty Class1/CustomAuthorizeAttribute/partial PageModels and repeated namespace declaration in ShippingContext if retained. Rename 'UserOrderService .cs', 'UpiPaymentService.cs.cs' and 'ManangeStock' consistently as cleanup, maintaining route compatibility where needed. These are improvement suggestions, not separate counted production failures.
8. **Frontend quality follow-through.** After functional repairs, use one pinned Bootstrap bundle (the admin layout loads it twice), avoid duplicate navbar IDs, share shipping/coupon policy text with the actual settings (front page advertises a different threshold), show actionable cart errors and retry controls, and verify mobile/keyboard behavior in a browser. The unused shippingcart.js assumes a total display element exists; remove it or guard that access before wiring it into any page.

## Verification plan after approval

- Build all retained projects and require no compatibility warnings from obsolete MVC dependencies. Run existing tests in an environment where testhost IPC works, with output isolated from running applications.
- On a disposable SQL Server database, migrate from empty and from the previous snapshot, assert required statuses and bounded/unique keys, and verify migration model parity. Never run these checks against production data.
- Exercise registration/duplicate email/confirmation/login lockout/recovery, anonymous and non-admin API writes, and missing/invalid antiforgery tokens.
- Exercise first add, repeated/increment/decrement/last remove, missing stock, server-side quantity bounds, stale prices, coupons/shipping, empty checkout and insufficient stock. Include forged TotalAmount values and two concurrent buyers for the final stock unit.
- Verify payment failure, capture followed by order-finalization failure, duplicate/replayed checkout requests, delayed provider callbacks and reconciliation. Assert exact persisted amounts/references and cash/electronic paid-state differences.
- Render every form's invalid/API-failure path, including GenreDTO, stock/order fallbacks and book dropdown rebuilding. Verify anonymous Add to Cart redirects, encoded search terms such as C# and A&B, malformed page values and filter-preserving paging.
- Verify purchased books cannot erase order history; cloned/shared images survive independent updates/deletion; failed uploads are cleaned; cache reads agree after genre writes; and catalog load remains within expected service quotas.

## Overall quality assessment

**Prototype/learning-project quality with useful structure, but substantial security and purchasing-integrity gaps.** Naming and layering alone do not compensate for the current failures in first-cart persistence, initialization, public write authorization, payment authority and history retention. Correct these before real transactions or public production exposure. The recommended changes fit the current ASP.NET Core MVC + FastEndpoints + EF Core/SQL Server architecture and can be delivered incrementally on net9.0; any framework upgrade should be a separate tested decision.

The report is an audit snapshot rather than proof of successful runtime execution. No fixes were applied. Approval of the report should precede implementation.

## Framework references used to verify behavior

- MVC state-changing GET/antiforgery behavior and global AutoValidateAntiforgeryToken guidance were checked against [Microsoft's ASP.NET Core 9 antiforgery documentation](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-9.0). See BR-003.
- Validation attribute activation was checked against [FastEndpoints validation documentation](https://fast-endpoints.com/docs/validation) and its [ValidationOptions API reference](https://api-ref.fast-endpoints.com/api/FastEndpoints.ValidationOptions.html). The local project does not enable that support or define Genre validators. See BR-052.
- Conditional update/concurrency recommendations were checked against [EF Core ExecuteUpdate documentation](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) and [concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency). See BR-014/015/042.
- EF9 migration rejection on pending model changes was checked against [EF Core 9 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/breaking-changes). The local model/snapshot mismatch is verified; its fresh-start consequence is inferred. See BR-013.
- The email uniqueness default was checked against [Microsoft Identity configuration documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-8.0). The same unchanged option is used by this project. See BR-008.
- Automatic WebApplication authentication middleware behavior was checked against [Microsoft's middleware documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-9.0); a missing explicit call was therefore not counted as a defect.

