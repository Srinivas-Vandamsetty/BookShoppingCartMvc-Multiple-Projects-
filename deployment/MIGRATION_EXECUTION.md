# Database migration execution — 5 November 2026

Server: COGNINE-L218\SQLEXPRESS01
Database: BookShoppingCartMvc

## Outcome

Applied 20261105082638_HardenCommerce successfully.
EF database update subsequently confirmed: “The database is already up to date.”
SQL Server constraint validation passed.

## Legacy baseline reconciliation

The database already contained eight legacy migration records and all tables required by the repository's InitialCreate model.
A schema-only comparison against a disposable initial-migration database found exactly one column difference: ShoppingCart.UserId was already NOT NULL (a stronger constraint).
Expected foreign-key and index names were present.

Recorded 20250331111632_InitialCreate as the verified existing-schema baseline without replaying its CREATE TABLE operations.
All legacy history records remain intact.

## Backup and rehearsal

A COPY_ONLY full backup with CHECKSUM was created and passed RESTORE VERIFYONLY.
Backup retained at:

    C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup\BookShoppingCartMvc_before_upgrade_870C6CDCEA0C408B94B6C69F940E82DF.bak

The upgrade was rehearsed against a disposable restore of this backup before applying it to the application database.
Rehearsal revealed SQL Server batch compilation of backfills referencing newly added columns; corrected those statements to execute after column creation, in both the migration and deployment script.
Both disposable comparison/rehearsal databases were removed afterward.

## Preserved application records

| Entity | Before | After |
|---|---:|---:|
| Users | 4 | 4 |
| Books | 24 | 24 |
| Orders | 12 | 12 |
| Order details | 7 | 7 |
| Cart details | 1 | 1 |

The rehearsal comparison found zero changed user records, including credentials/account state.
No user account or password was reset.

## Validation

- Solution Release build: zero warnings/errors.
- Regression suite: 87 passed, zero failed/skipped on disposable SQL Express databases.
- Added a test executing the generated idempotent migration script twice.
- Verified decimal price/totals, Book.IsArchived, checkout-key columns and applied migration history.
- DBCC CHECKCONSTRAINTS completed without violations.
- Final EF update check confirmed no pending migrations.

The initial-baseline.sql artifact is a fresh-schema comparison script; do not execute it against an existing populated database.
The legacy warning in earlier implementation records describes the database before this upgrade and is now resolved.



## Authentication migration

Applied **20261105102022_AddApiAuthenticationSessions** on 5 November 2026 to the same SQL Express database.
The migration adds ApiSessions and ApiRefreshTokens, their indexes, and cascading Identity/session foreign keys.
The generated idempotent deployment script now includes this migration and passed twice-executed replay tests against a disposable database.

Before applying it, a fresh COPY_ONLY backup with CHECKSUM passed RESTORE VERIFYONLY:

    C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup\BookShoppingCartMvc_before_auth_6C5CF78908B9454896138040D6C49867.bak

Afterward: 4 users, 24 books, 12 orders, 0 API sessions and 0 refresh-token rows.
DBCC CHECKCONSTRAINTS completed without violations. No credentials, roles or confirmation flags were modified.
The nine-project Release solution build passed with zero warnings/errors.
The final regression run passed **114 tests**, including SQL Server refresh concurrency, migration replay and HTTP authentication tests, with zero failures/skips.


## Frontend authentication integration

Applied **20261105121209_ProtectBrowserAuthenticationTokens** on 5 November 2026 to BookShoppingCartMvc on COGNINE-L218\SQLEXPRESS01.
This adds one nullable ApiSessions.ProtectedBrowserTokens column for MVC Data Protection-encrypted token pairs. API refresh-token verification remains hash-based.
The full idempotent deployment script includes this migration and passed the existing twice-executed replay test.

A fresh COPY_ONLY backup with CHECKSUM passed RESTORE VERIFYONLY before the update:

    C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup\BookShoppingCartMvc_before_ui_auth_302DE785C0A4401A80A714F71079AB74.bak

Validation: the nine-project Release build passed with zero warnings/errors; all 121 .NET tests and 6 JavaScript tests passed.
Frontend integration tests use separate randomly named disposable SQL databases, never application accounts.
Desktop/mobile login layouts were verified in a temporary local HTTPS MVC host, which was stopped afterward.
See AUTHENTICATION_API.md for deployment key persistence, API URL settings and session behavior.

Post-update verification preserved 4 users, 24 books and 12 orders. The new token column is nullable; DBCC CHECKCONSTRAINTS passed without violations. No application credentials or account states were changed.


## P1 commerce API migration — 6 November 2026

Applied **20261106062559_AddCommerceOperationAudit** to BookShoppingCartMvc on COGNINE-L218\SQLEXPRESS01. The migration adds the CommerceAuditEvents table and its entity/history index. The generated idempotent script includes the migration and passed the disposable-database replay test.

Verified COPY_ONLY backup with CHECKSUM:

    C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS01\MSSQL\Backup\BookShoppingCartMvc_before_p1_A3C0512A3F4845D7835F9B038285C514.bak

Before and after: 4 users, 24 books, 12 orders, 21 stock rows. The new audit table is empty. DBCC CHECKCONSTRAINTS completed without violations. No production data was used by integration tests.

The nine-project Release solution build passed with zero warnings/errors. 134 .NET tests and 6 JavaScript tests passed. A temporary HTTPS API instance verified Swagger discovery, book details, availability, cash payment methods and rejection of anonymous cart access against the migrated local database; the temporary instance was stopped.
