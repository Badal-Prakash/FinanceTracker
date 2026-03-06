# FinanceTracker

A multi-tenant expense and invoice management platform built with **ASP.NET Core 8** (Clean Architecture) and **Angular 17** (standalone components). Supports role-based access control, JWT authentication, expense workflows, invoice management, budget tracking, and reporting.

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Environment Configuration](#environment-configuration)
- [Project Structure](#project-structure)
- [User Roles & Permissions](#user-roles--permissions)
- [API Endpoints](#api-endpoints)
  - [Auth](#auth)
  - [Expenses](#expenses)
  - [Expense Import](#expense-import)
  - [Invoices](#invoices)
  - [Categories](#categories)
  - [Dashboard](#dashboard)
  - [Reports](#reports)
- [Frontend Pages](#frontend-pages)
- [Data Models](#data-models)
- [Expense Workflow](#expense-workflow)
- [Invoice Workflow](#invoice-workflow)
- [Import File Format](#import-file-format)

---

## Tech Stack

### Backend
| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL (via Npgsql) |
| Auth | JWT Bearer tokens + Refresh tokens |
| CQRS | MediatR |
| Validation | FluentValidation |
| Excel parsing | EPPlus 7 |

### Frontend
| Layer | Technology |
|---|---|
| Framework | Angular 17 (standalone components) |
| Language | TypeScript |
| Styling | SCSS with CSS custom properties |
| State | Angular Signals |
| HTTP | Angular HttpClient |
| Routing | Angular Router |

---

## Architecture

```
FinanceTracker/
├── backend/
│   └── src/
│       ├── FinanceTracker.Domain          # Entities, enums, domain events
│       ├── FinanceTracker.Application     # CQRS commands/queries, DTOs, interfaces
│       ├── FinanceTracker.Infrastructure  # EF Core, JWT, persistence
│       └── FinanceTracker.API             # Controllers, middleware, program entry
└── frontend/
    └── src/app/
        ├── core/                          # Models, services, interceptors
        └── features/                      # Page components (auth, dashboard, expenses, invoices, reports)
```

Multi-tenancy is enforced at the EF Core level via **global query filters** — every entity carries a `TenantId`, and all queries are automatically scoped to the authenticated user's tenant.

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- PostgreSQL 15+

### Backend
```bash
cd backend

# Restore packages
dotnet restore

# Install EPPlus (for Excel import)
dotnet add src/FinanceTracker.Application package EPPlus --version 7.*

# Apply migrations
dotnet ef database update --project src/FinanceTracker.Infrastructure --startup-project src/FinanceTracker.API

# Run
dotnet run --project src/FinanceTracker.API
# API available at: https://localhost:7001
```

### Frontend
```bash
cd frontend
npm install
ng serve
# App available at: http://localhost:4200
```

---

## Environment Configuration

### `appsettings.json` (backend)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=FinanceTrackerDB;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "FinanceTrackerAPI",
    "Audience": "FinanceTrackerAngularApp",
    "ExpiryMinutes": "60"
  }
}
```

### `environment.ts` (frontend)
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api'
};
```

---

## Project Structure

### Backend — Application Layer
```
FinanceTracker.Application/
├── Auth/               LoginCommand, RegisterTenantCommand, RefreshTokenCommand
├── Expenses/           CRUD + Submit/Approve/Reject + ImportExpensesFeature
├── Invoices/           InvoicesFeature (CRUD + MarkPaid + Cancel)
├── Categories/         CategoriesFeature (CRUD)
├── Dashboard/          DashboardFeature (stats + charts)
├── Reports/            ReportsFeature (expense report + CSV exports)
└── Common/
    └── Interfaces/     IApplicationDbContext, ICurrentUserService, IJwtTokenService, IEmailService
```

### Frontend — Features
```
src/app/features/
├── auth/
│   ├── login/          Login page
│   └── register/       Tenant registration page
├── dashboard/          Dashboard with KPIs and charts
├── expenses/
│   ├── expense-list/   Paginated expense list with filters + Import button
│   ├── expense-form/   Create / edit expense form
│   ├── expense-detail/ Expense detail + approve/reject actions
│   └── expense-import/ 3-step CSV/Excel import modal
├── invoices/
│   ├── invoice-list/   Invoice list with stats + search + filter
│   ├── invoice-form/   Create / edit invoice with line items
│   └── invoice-detail/ Printable invoice detail page
├── reports/            Expense report with date/category filters + CSV export
├── budgets/            Monthly budget management by category
└── shell/              App shell (sidebar, nav, user footer, theme toggle)
```

---

## User Roles & Permissions

| Role | Expenses | Invoices | Categories | Reports | Dashboard |
|---|---|---|---|---|---|
| **Employee** | Own only | — | Read | Own only | Own stats |
| **Manager** | All — approve/reject | Read | Read | All | Full |
| **Admin** | All — approve/reject | Full | Full CRUD | All | Full |
| **SuperAdmin** | All | Full | Full CRUD | All | Full |

---

## API Endpoints

All endpoints except Auth require an `Authorization: Bearer <token>` header.

Base URL: `https://localhost:7001/api`

---

### Auth

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register a new tenant + admin user |
| `POST` | `/api/auth/login` | Public | Login, returns JWT access + refresh token |
| `POST` | `/api/auth/refresh-token` | Public | Exchange refresh token for new access token |

**Register body:**
```json
{
  "tenantName": "Acme Corp",
  "subdomain": "acme",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@acme.com",
  "password": "SecurePass123!"
}
```

**Login body:**
```json
{
  "email": "john@acme.com",
  "password": "SecurePass123!"
}
```

**Auth response:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "expiresAt": "2026-03-06T18:00:00Z",
  "user": {
    "id": "guid",
    "fullName": "John Doe",
    "email": "john@acme.com",
    "role": "Admin"
  }
}
```

---

### Expenses

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/expenses` | Any role | Paginated expense list (employees see own only) |
| `GET` | `/api/expenses/{id}` | Any role | Single expense detail |
| `POST` | `/api/expenses` | Any role | Create a new expense (status: Draft) |
| `POST` | `/api/expenses/{id}/submit` | Any role | Submit a draft expense for approval |
| `POST` | `/api/expenses/{id}/approve` | Manager+ | Approve a submitted expense |
| `POST` | `/api/expenses/{id}/reject` | Manager+ | Reject a submitted expense with reason |
| `DELETE` | `/api/expenses/{id}` | Any role | Delete a draft expense |

**GET `/api/expenses` — query params:**
```
page        int     (default: 1)
pageSize    int     (default: 20)
status      string  Draft | Submitted | Approved | Rejected
categoryId  guid
userId      guid    (Admin/Manager only)
fromDate    date
toDate      date
```

**POST `/api/expenses` body:**
```json
{
  "title": "Office Supplies",
  "description": "Printer paper",
  "amount": 45.99,
  "expenseDate": "2026-03-01T00:00:00Z",
  "categoryId": "guid"
}
```

**POST `/api/expenses/{id}/reject` body:**
```json
{ "reason": "Missing receipt" }
```

---

### Expense Import

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/expenses/import/template` | Any role | Download CSV template file |
| `POST` | `/api/expenses/import/preview` | Any role | Parse file, validate rows, return preview |
| `POST` | `/api/expenses/import` | Any role | Run bulk import, returns result summary |

**POST `/api/expenses/import/preview`** — `multipart/form-data`:
```
file    File    CSV or .xlsx file
```

**POST `/api/expenses/import`** — `multipart/form-data`:
```
file               File     CSV or .xlsx file
submitAfterImport  bool     Submit imported expenses immediately (default: false)
skipErrors         bool     Skip invalid rows and import valid ones (default: true)
```

**Preview response:**
```json
{
  "rows": [
    {
      "rowNumber": 2,
      "title": "Office Supplies",
      "description": "Printer paper",
      "amount": 45.99,
      "expenseDate": "2026-03-01",
      "category": "Office",
      "isValid": true,
      "error": null
    }
  ],
  "validCount": 3,
  "errorCount": 1,
  "availableCategories": ["Office", "Travel", "Food & Dining"]
}
```

**Import result response:**
```json
{
  "imported": 3,
  "skipped": 1,
  "errors": ["Row 4: Category 'Entertainment' not found."]
}
```

---

### Invoices

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/invoices/stats` | Any role | KPI totals — unpaid, overdue, paid this month |
| `GET` | `/api/invoices` | Any role | Paginated invoice list |
| `GET` | `/api/invoices/{id}` | Any role | Invoice detail with line items |
| `POST` | `/api/invoices` | Any role | Create invoice with line items (auto-generates invoice number) |
| `PUT` | `/api/invoices/{id}` | Any role | Update invoice and replace all line items |
| `POST` | `/api/invoices/{id}/mark-paid` | Any role | Mark invoice as Paid |
| `POST` | `/api/invoices/{id}/cancel` | Any role | Cancel an invoice |
| `DELETE` | `/api/invoices/{id}` | Any role | Delete invoice (not allowed if Paid) |

**GET `/api/invoices` — query params:**
```
page        int
pageSize    int     (default: 20)
status      string  Unpaid | Paid | Overdue | Cancelled
clientName  string  partial match
fromDate    date
toDate      date
```

**POST `/api/invoices` body:**
```json
{
  "clientName": "Acme Corp",
  "clientEmail": "billing@acme.com",
  "clientAddress": "123 Main St, New York",
  "dueDate": "2026-04-01T00:00:00Z",
  "notes": "Net 30",
  "lineItems": [
    { "description": "Web development", "quantity": 10, "unitPrice": 150.00 },
    { "description": "Hosting (monthly)", "quantity": 1, "unitPrice": 49.00 }
  ]
}
```

**Invoice stats response:**
```json
{
  "totalUnpaid": 5200.00,
  "totalPaidThisMonth": 3800.00,
  "totalOverdue": 1200.00,
  "unpaidCount": 4,
  "overdueCount": 1,
  "paidThisMonthCount": 3
}
```

---

### Categories

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/categories` | Any role | All active categories |
| `GET` | `/api/categories/{id}` | Any role | Single category |
| `POST` | `/api/categories` | Admin+ | Create category |
| `PUT` | `/api/categories/{id}` | Admin+ | Update category name, color, icon |
| `DELETE` | `/api/categories/{id}` | Admin+ | Deactivate category |

**POST `/api/categories` body:**
```json
{
  "name": "Travel",
  "color": "#f59e0b",
  "icon": "airplane"
}
```

---

### Dashboard

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/dashboard/stats` | Any role | Full dashboard data — KPIs, charts, recent expenses |

**Response includes:**
```json
{
  "totalExpensesThisMonth": 1250.00,
  "totalExpensesLastMonth": 980.00,
  "monthOverMonthChange": 27.6,
  "pendingApprovalsCount": 3,
  "approvedThisMonth": 5,
  "rejectedThisMonth": 1,
  "approvalRate": 83.3,
  "totalApprovedAmountThisMonth": 870.00,
  "totalCategories": 8,
  "totalBudgetedThisMonth": 2000.00,
  "budgetUtilisationPercent": 43.5,
  "topCategories": ["..."],
  "monthlyTrend": ["..."],
  "statusBreakdown": ["..."],
  "recentExpenses": ["..."]
}
```

---

### Reports

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/reports/expenses` | Any role | Expense report JSON (rendered + printable in browser) |
| `GET` | `/api/reports/expenses/csv` | Any role | Export filtered expenses as CSV download |
| `GET` | `/api/reports/budget/csv` | Admin+ | Export monthly budget vs actuals as CSV |

**GET `/api/reports/expenses` — query params:**
```
fromDate    date
toDate      date
status      string  Draft | Submitted | Approved | Rejected
categoryId  guid
userId      guid
```

**GET `/api/reports/budget/csv` — query params:**
```
month   int   1–12
year    int   e.g. 2026
```

---

## Frontend Pages

| Route | Component | Description |
|---|---|---|
| `/login` | `LoginComponent` | Email + password login form |
| `/register` | `RegisterComponent` | New tenant registration — company name, subdomain, admin user details |
| `/dashboard` | `DashboardComponent` | KPI cards (total spend, pending, approval rate, budget utilisation), monthly trend bar chart, category donut chart, status breakdown bars, recent expenses list |
| `/expenses` | `ExpenseListComponent` | Paginated expense table, status filter tabs, search, **Import** button to open import modal |
| `/expenses/new` | `ExpenseFormComponent` | Create expense — title, description, amount, date, category picker |
| `/expenses/:id` | `ExpenseDetailComponent` | Full expense detail, submit / approve / reject actions based on role |
| `/expenses/:id/edit` | `ExpenseFormComponent` | Edit a Draft expense |
| `/invoices` | `InvoiceListComponent` | Invoice table with 3 stat cards (unpaid / overdue / paid this month), client name search, status filter, inline mark-paid and delete buttons |
| `/invoices/new` | `InvoiceFormComponent` | Create invoice — client details, due date, dynamic line item rows, running total |
| `/invoices/:id` | `InvoiceDetailComponent` | Printable invoice document with brand header, client address block, line item table, notes; mark paid / cancel / delete actions |
| `/invoices/:id/edit` | `InvoiceFormComponent` | Edit an Unpaid or Overdue invoice |
| `/reports` | `ReportsComponent` | Date range + category + status filters, summary cards (total / approved / pending), full expense table, **Print** button (hides UI, prints report only), **Export CSV** download |
| `/budgets` | `BudgetsComponent` | Monthly budget grid by category — set/update budget amount per category, utilisation progress bars, month/year selector |

### Expense Import Modal (`/expenses` → Import button)

A 3-step modal overlay:

| Step | Description |
|---|---|
| **1 — Upload** | Drag-and-drop zone or file browser for CSV / .xlsx. Download template CSV link. Shows accepted column format. |
| **2 — Preview** | Parsed row table with ✅ valid / ❌ error status per row. Toggle to show only errors. Options: skip errors, submit after import. Import button shows valid row count. |
| **3 — Done** | Summary: X imported, Y skipped. Lists any skipped row error messages. Options to import another file or go to expenses list. |

On successful import the expenses list automatically refreshes.

### Dashboard Charts (no external libraries — pure SVG/CSS)

| Chart | Implementation |
|---|---|
| Monthly trend bar | 6-month side-by-side bars — total (light indigo) vs approved (indigo), heights as % of max |
| Category donut | SVG circles with `pathLength="100"` stroke-dasharray for percentage segments, colour-coded legend |
| Status breakdown | Horizontal progress bars per status (Draft / Submitted / Approved / Rejected) for current month |

---

## Data Models

### Expense
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | |
| `title` | string | Max 200 chars |
| `description` | string? | |
| `amount` | decimal | Must be > 0 |
| `expenseDate` | DateTime | |
| `status` | ExpenseStatus | Draft → Submitted → Approved / Rejected |
| `receiptUrl` | string? | Blob storage URL |
| `rejectionReason` | string? | Set on rejection |
| `categoryId` | Guid | FK → Category |
| `submittedById` | Guid | FK → User |
| `approverId` | Guid? | FK → User (set on approve/reject) |
| `tenantId` | Guid | Auto-scoped |

### Invoice
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | |
| `invoiceNumber` | string | Auto-generated: `INV-YYYYMM-XXXXXX` |
| `clientName` | string | |
| `clientEmail` | string | |
| `clientAddress` | string? | |
| `amount` | decimal | Sum of line items, updated on edit |
| `dueDate` | DateTime | |
| `status` | InvoiceStatus | Unpaid → Paid / Overdue / Cancelled |
| `paidAt` | DateTime? | Set when marked paid |
| `notes` | string? | Payment terms, bank details, etc. |
| `lineItems` | InvoiceLineItem[] | |
| `tenantId` | Guid | Auto-scoped |

### InvoiceLineItem
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | |
| `invoiceId` | Guid | FK → Invoice (cascade delete) |
| `description` | string | |
| `quantity` | int | Must be > 0 |
| `unitPrice` | decimal | Must be > 0 |
| `total` | decimal | Computed: `quantity × unitPrice` (not stored) |

### Category
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | |
| `name` | string | Max 100 chars |
| `color` | string | Hex colour e.g. `#6366f1` |
| `icon` | string | Icon name |
| `isActive` | bool | Soft delete via deactivation |
| `tenantId` | Guid | |

### Budget
| Field | Type | Notes |
|---|---|---|
| `id` | Guid | |
| `categoryId` | Guid | FK → Category |
| `amount` | decimal | Monthly budget amount |
| `month` | int | 1–12 |
| `year` | int | |
| `tenantId` | Guid | |

---

## Expense Workflow

```
[Create] ──► Draft
                │
           [Submit] ──► Submitted
                             │
                  ┌──────────┴──────────┐
             [Approve]             [Reject + reason]
                  │                       │
              Approved                Rejected
```

- Only **Draft** expenses can be edited or deleted
- Only **Manager / Admin / SuperAdmin** can approve or reject
- Employees only see and act on their own expenses

---

## Invoice Workflow

```
[Create] ──► Unpaid
                │
     ┌──────────┼────────────────┐
[Mark Paid]  [Cancel]    [Auto — past due date]
     │           │               │
   Paid      Cancelled        Overdue
                                 │
                            [Mark Paid]
                                 │
                               Paid
```

- **Paid** invoices cannot be edited or deleted
- Invoice number is auto-generated on creation: `INV-202603-A1B2C3`
- `amount` on the Invoice is always recomputed from line items on create/update

---

## Import File Format

Download the template from `GET /api/expenses/import/template` or use the **Download template CSV** link inside the import modal.

### CSV
```csv
Title,Description,Amount,Date,Category
Office Supplies,Printer paper and pens,45.99,2026-03-01,Office
Team Lunch,Quarterly team lunch,120.00,2026-03-05,Food & Dining
Flight to NYC,Conference travel,380.00,2026-03-10,Travel
```

### Excel (.xlsx)
Same 5 columns in the same order on the first worksheet, with a header row on row 1.

### Column rules
| Column | Required | Format | Notes |
|---|---|---|---|
| Title | ✅ | Text | Max 200 chars |
| Description | — | Text | Can be blank |
| Amount | ✅ | Decimal | Positive number e.g. `45.99` |
| Date | ✅ | `YYYY-MM-DD` | |
| Category | ✅ | Text | Must exactly match an existing category name (case-insensitive) |

All imported expenses are created with **Draft** status.