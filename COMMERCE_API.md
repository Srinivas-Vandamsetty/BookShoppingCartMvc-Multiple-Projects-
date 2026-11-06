# P1 commerce APIs

The active .NET 9 API exposes these routes alongside the existing authentication, catalog and genre routes. The MVC UI uses its server-side Bearer client and existing antiforgery-protected Razor forms. JWTs remain on the server; browser requests continue to target MVC routes.

| Access | Method | Route | Contract |
|---|---|---|---|
| Public | GET | /api/books/{bookId} | Book, genre, price, cover filename and current stock |
| Public | GET | /api/books/{bookId}/availability | Available quantity and maximum purchase quantity |
| Admin | POST | /api/books | Create book; 201 with the book and Location |
| Admin | PUT | /api/books/{bookId} | Replace validated book details; returns the updated book |
| Admin | DELETE | /api/books/{bookId} | Archive; 204, preserving historical orders |
| Admin | GET | /api/admin/books?page=1 | Paged active books |
| Customer | GET | /api/cart?coupon=SAVE10 | Cart lines, count, subtotal, discount, shipping and total |
| Customer | POST | /api/cart/items | Add bookId and quantity; returns item count |
| Customer | PUT | /api/cart/items/{bookId} | Set exact quantity; returns item count |
| Customer | DELETE | /api/cart/items/{bookId} | Remove the whole line; 204 |
| Customer | POST | /api/cart/items/{bookId}/decrement | Decrement one item; supports the existing minus button |
| Customer | POST | /api/cart/quote | Accept optional couponCode; return price, checkoutKey, quoteToken and expiresAtUtc |
| Customer | POST | /api/cart/checkout | Submit contact/address, payment method and quote; 201 with orderId, total and Location |
| Customer | GET | /api/orders?page=1 | Paged orders owned by the current user |
| Owner/Admin | GET | /api/orders/{orderId} | Order lines, snapshots, status, payment, totals and delivery contact |
| Admin | GET | /api/admin/orders?page=1&status=Pending&search=42 | Search by order ID, name or email; filter by status |
| Admin | GET | /api/admin/order-statuses | Available status IDs and names |
| Admin | PATCH | /api/admin/orders/{orderId}/status | Apply an allowed fulfillment change and audit it; 204 |
| Public | GET | /api/payment-methods | Enabled payment methods, currently cash on delivery |
| Admin | PUT | /api/admin/orders/{orderId}/cash-collection | Explicit audited collection state update; 204 |
| Customer | GET | /api/customers/me | Own Identity username, email, phone and email confirmation state |
| Customer | PATCH | /api/customers/me | Update phoneNumber only; null/blank clears it. Email confirmation still uses Identity's existing flow |
| Admin | GET | /api/admin/inventory?page=1&search=Book&lowStock=5 | Paged inventory; optional text search and inclusive stock threshold |
| Admin | POST | /api/admin/inventory/{bookId}/adjustments | Audited inventory delta with an expected quantity; 204 |

Protected routes use Authorization: Bearer <access-token>. Ownership comes from the validated JWT subject, never a supplied user ID. Cross-customer order lookups return 404. Paged endpoints return { items, page, hasNext } with at most 50 items. Bad inputs return 400, stale or invalid state changes 409, and unsupported payment methods 422. Existing catalog and genre routes are unchanged.

## Checkout

Request a quote immediately before submitting checkout. Its 15-minute reference is protected by API Data Protection keys and binds the customer, cart quantities/prices, coupon and checkout key. Submit the same checkoutKey, quoteToken and couponCode with name, email, mobileNumber, address and paymentMethod. The only enabled method is cash. Posted totals and fingerprints are ignored. The cart repository verifies the current cart and stock within its checkout transaction, and a valid replay returns the original order. If the UI cannot confirm an outcome, retry the same attempt or check order history before starting another purchase.

## Audited operations

Inventory adjustments accept operationId (a client-generated UUID), expectedQuantity, delta and reason. Status changes accept operationId, expectedStatusId, statusId and reason. Cash collection accepts operationId, expectedCollected, collected and reason. Use a new operationId for a different change; an identical retry returns the original success. A reused ID with different input or stale expected state returns 409. SQL transactions, application locks and conditional updates protect against concurrent changes. The audit records the actor, reason, timestamp, and before/after values.

Fulfillment supports Pending → Shipped → Delivered and unpaid Pending → Cancelled. Cancellation restores stock in the same transaction. Return/refund workflows and electronic payment providers remain later work. Cash collection changes do not contact an electronic payment provider.

## UI and deployment

- Catalog details and book create/edit/archive use the Books APIs; existing validated cover uploads remain in MVC and send only the stored filename to the API.
- Cart controls support incremental and exact quantities plus complete removal. Checkout uses the API quote and available payment methods, and links to the resulting order.
- Customer history/details and the Identity profile page use the Orders and Customers APIs. Admin order filters, status changes, cash collection and stock adjustments use the corresponding audited APIs.
- Apply 20261106062559_AddCommerceOperationAudit before starting the updated hosts. It adds an audit table and index. The API and MVC continue to share the same database and HTTPS API base URL. Persist and share **API** Data Protection keys across API replicas so a checkout quote can be verified by any replica. Keep **MVC** keys shared among MVC replicas for browser sessions.
- MFA was the conditional P1 item. The selected database has no MFA-enabled accounts; the existing fail-closed API behavior is unchanged. Authors, wishlist, reviews, addresses, electronic payments, returns and other P2/P3 features are outside this release.


## Verification

On 6 November 2026, all 134 .NET tests and 6 JavaScript tests passed, with a clean nine-project Release build. Tests cover API authorization/ownership, book CRUD, stock and cash audit replay, stale writes, order status transitions, checkout quote integrity/expiration and duplicate submission, and complete buyer/admin Razor workflows against real API and MVC test hosts.

The audit migration is applied to the selected local SQL Express database after a verified backup. Existing record counts and constraints were preserved. A live HTTPS smoke check also verified public commerce reads, protected cart access and Swagger discovery. Restart the normal API and MVC hosts to use the new routes.
