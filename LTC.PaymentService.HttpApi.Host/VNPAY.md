# VNPAY integration

Configure sandbox or production credentials under `VnPay` in `appsettings.json` or environment variables (`VnPay__TmnCode`, `VnPay__HashSecret`, etc.).

## Public URLs (register with VNPAY and align with your reverse proxy)

Use **`PublicBaseUrl`** as the externally reachable base of this API (same concept as `App:SelfUrl`). Relative paths default to:

| Purpose | Path |
|--------|------|
| IPN (server-to-server) | `/api/payment/vnpay-ipn` |
| Browser return | `/api/payment/vnpay-return` |

Examples when `PublicBaseUrl` is `https://localhost:44345/ltc/payment-service`:

- IPN: `https://localhost:44345/ltc/payment-service/api/payment/vnpay-ipn`
- Return: `https://localhost:44345/ltc/payment-service/api/payment/vnpay-return`

Behind a gateway, publish the same paths under your public host (for example `https://api.example.com/ltc/payment-service/...`).

**HTTP API surface (this service):**

The same actions are registered under both prefixes so callers can use either gateway mapping:

- `/ltc/payment-service/api/payment/...`
- `/payment-service/api/payment/...`

Customer web app calls typically use `NEXT_PUBLIC_API_URL` plus `/payment-service/api/payment/...` (same pattern as admin booking calls under `/payment-service`). If your gateway only exposes the `/ltc/...` prefix, use that path instead.

- `POST .../customer/payment/create-payment-url` (authenticated customer route; `X-Tenant` when multi-tenancy is enabled) — body: `CreateVnPayPaymentUrlInputDto` (`BookingId`, `Amount`, `OrderInfo`, optional `Locale`, optional `BankCode` such as `VNPAYQR` / `VNBANK` / `INTCARD`).
- `POST .../{role}/payment/create-payment-url` role-scoped variants are available for `admin`, `manager`, `staff`; the old `.../api/payment/create-payment-url` endpoint remains temporarily for compatibility.
- `GET .../vnpay-ipn` (anonymous) — VNPAY IPN; always returns **HTTP 200** with JSON `RspCode` / `Message`.
- `GET .../vnpay-return` (anonymous) — browser return; **no database payment updates**; redirects to `VnPay:FrontendSuccessUrl` or `VnPay:FrontendFailureUrl`, with `bookingId` appended when known.

Set `VnPay:IncludeIpnUrlInPaymentRequest` to `true` only if you need `vnp_IpnUrl` on the pay URL; the official WebForms demo often omits it and registers the IPN URL in the VNPAY merchant portal only.

## Multi-tenant IPN and `dbo.VnpayTxnRoutings`

VNPAY callbacks are anonymous and do not send `X-Tenant`. The service stores a **host** table `dbo.VnpayTxnRoutings` (entity `VnpayTxnRouting`) when creating a payment URL so the IPN handler can resolve `TenantId` and tenant schema **before** loading `PaymentRequest` / `Payment`. Ensure this table exists in the payment database.

Example DDL:

```sql
IF OBJECT_ID(N'dbo.VnpayTxnRoutings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VnpayTxnRoutings (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_VnpayTxnRoutings PRIMARY KEY,
        TenantId uniqueidentifier NOT NULL,
        TenantName nvarchar(256) NOT NULL
    );
END
```

## Optional unique index on `GatewayOrderId`

An EF unique index on `PaymentRequests.GatewayOrderId` is configured per tenant schema (`PaymentRequests` lives in each tenant schema). If you maintain schemas manually, add a filtered unique index where needed:

```sql
CREATE UNIQUE INDEX IX_PaymentRequests_GatewayOrderId
ON [<TenantSchema>].[PaymentRequests]([GatewayOrderId])
WHERE [GatewayOrderId] IS NOT NULL;
```

## Behaviour summary

- **Money-moving updates** happen only in the **IPN** handler after signature and amount checks. The return URL is for UX only.
- `vnp_TxnRef` is the string form of `PaymentRequest.Id` (`GatewayOrderId` matches).
- Payment success status is stored as `Payment.PaymentStatus = "SUCCESS"` (aligned with existing refund flow).
