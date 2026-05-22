# boutiqueiq — Inventory & Warehouse Management

A full-stack boutique inventory system for managing products across multiple small warehouses, with expiration monitoring, image-based search, real-time alerts, and a polished React UI.

---

## Table of contents

1. [Overview](#1-overview)
2. [Tech stack](#2-tech-stack)
3. [Solution layout](#3-solution-layout)
4. [Domain model](#4-domain-model)
5. [Feature list](#5-feature-list)
6. [API reference](#6-api-reference)
7. [Frontend pages](#7-frontend-pages)
8. [Authentication & security](#8-authentication--security)
9. [Background jobs & real-time alerts](#9-background-jobs--real-time-alerts)
10. [Image search](#10-image-search)
11. [Seed data](#11-seed-data)
12. [Running locally](#12-running-locally)
13. [Configuration](#13-configuration)
14. [SQLite-specific notes](#14-sqlite-specific-notes)

---

## 1. Overview

A boutique owner manages products (perfumes, handbags, shoes, makeup, skincare, accessories) across multiple small warehouses. The system tracks stock location, product expiration dates, categories, and fires alerts when products are approaching expiry. A perceptual-hash image search lets staff find products by photograph.

**Key characteristics:**
- Clean architecture (Domain → Application → Infrastructure → Api)
- REST API on ASP.NET Core (.NET 10) + SQLite
- React 19 + TypeScript single-page frontend (Vite)
- JWT authentication with refresh token rotation
- SignalR hub for real-time expiration alerts
- Perceptual-hash image search (aHash 8×8, Hamming distance ≤ 6)
- FTS5 full-text search via Dapper

---

## 2. Tech stack

### Backend

| Concern | Choice |
|---|---|
| Language / runtime | C# 13, .NET 10 |
| Framework | ASP.NET Core (controller-based) |
| ORM | Entity Framework Core 10 (SQLite) |
| Read / search queries | Dapper 2.1 (parameterized SQL) |
| Full-text search | SQLite FTS5 virtual table |
| Mapping | AutoMapper 15 |
| Validation | FluentValidation 11 + `ValidationFilter` |
| Logging | Serilog (console + rolling file) |
| Background work | `BackgroundService` — daily expiration scan |
| Real-time | ASP.NET Core SignalR (`/hubs/alerts`) |
| API docs | Microsoft.AspNetCore.OpenApi + Scalar UI |
| Error format | RFC 7807 `ProblemDetails` via `IExceptionHandler` |
| Image hashing | SixLabors.ImageSharp — average hash (aHash 8×8) |

### Frontend

| Concern | Choice |
|---|---|
| Framework | React 19 + TypeScript |
| Build tool | Vite 6 |
| Routing | React Router v7 |
| Data fetching | TanStack Query v5 |
| HTTP client | Axios (with 401-refresh interceptor) |
| Forms | React Hook Form + Zod |
| Real-time | `@microsoft/signalr` |
| UI icons | Tabler Icons |
| Toasts | Sonner |
| File upload | react-dropzone |
| CSS | Tailwind CSS v4 + CSS custom properties |

---

## 3. Solution layout

```
warehouse_api/
├── src/
│   ├── BoutiqueInventory.Domain/           ← entities, no dependencies
│   │   └── Entities/
│   │       Warehouse, WarehouseSection, Product,
│   │       Category, ProductCategory, ExpirationAlert
│   │
│   ├── BoutiqueInventory.Application/      ← use-cases, DTOs, validators
│   │   ├── Common/                         ← PagedResult, Pagination, Exceptions
│   │   ├── DTOs/Requests/
│   │   ├── DTOs/Responses/
│   │   ├── Interfaces/                     ← repository + service contracts
│   │   ├── Mappings/MappingProfile.cs      ← AutoMapper profile
│   │   ├── Services/                       ← all business logic
│   │   ├── Validators/                     ← FluentValidation rules
│   │   └── DependencyInjection.cs
│   │
│   ├── BoutiqueInventory.Infrastructure/   ← EF Core, Dapper, jobs, imaging
│   │   ├── BackgroundJobs/
│   │   │   └── ExpirationCheckerJob.cs     ← BackgroundService, daily 08:00
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── DbSeeder.cs                 ← 100 realistic products on first run
│   │   │   ├── Configurations/
│   │   │   ├── Dapper/                     ← SQLite Guid + DateTimeOffset handlers
│   │   │   └── Migrations/
│   │   ├── Imaging/
│   │   │   └── ImageHashService.cs         ← aHash perceptual hashing
│   │   ├── Repositories/
│   │   ├── Search/
│   │   │   └── ProductSearchIndex.cs       ← FTS5 index management
│   │   └── Storage/
│   │       └── WebRootProductImageStorage.cs
│   │
│   └── BoutiqueInventory.Api/              ← thin HTTP entry point
│       ├── Auth/
│       │   └── TokenService.cs             ← JWT + refresh token issuance
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── WarehousesController.cs
│       │   ├── CategoriesController.cs
│       │   ├── ProductsController.cs
│       │   └── AlertsController.cs
│       ├── Hubs/
│       │   └── AlertsHub.cs                ← SignalR hub
│       ├── Filters/ValidationFilter.cs
│       ├── Middleware/GlobalExceptionHandler.cs
│       └── Program.cs
│
└── boutique-ui/                            ← React frontend
    ├── public/
    │   └── favicon.svg                     ← gold droplet icon
    └── src/
        ├── components/
        │   ├── layout/  (Layout, Sidebar, TopBar)
        │   └── ui/      (Modal, Badge, StatCard, SearchBar, …)
        ├── lib/         (api.ts, errors.ts, expiry.ts, media.ts)
        ├── pages/
        │   ├── LoginPage.tsx
        │   ├── DashboardPage.tsx
        │   ├── WarehousesPage.tsx
        │   ├── WarehouseDetailPage.tsx
        │   ├── ProductsPage.tsx
        │   ├── ImageSearchPage.tsx
        │   ├── CategoriesPage.tsx
        │   └── AlertsPage.tsx
        ├── services/    (authService, warehouseService, productService, …)
        └── types/index.ts
```

---

## 4. Domain model

| Entity | Key fields |
|---|---|
| `Warehouse` | `Id`, `Name`, `Location`, `IsActive`, `CreatedAt`, `DeactivatedAt`, `Sections` |
| `WarehouseSection` | `Id`, `WarehouseId`, `Name`, `Products` |
| `Product` | `Id`, `Name`, `Sku` (unique), `Description`, `Size`, `Type`, `ExpirationDate?`, `ImageUrl?`, `ImageMetadata?` (JSON), `WarehouseSectionId`, timestamps, `Categories` |
| `Category` | `Id`, `Name` (unique), `Description` |
| `ProductCategory` | composite `(ProductId, CategoryId)` join |
| `ExpirationAlert` | `Id`, `ProductId`, `AlertDate`, `DaysUntilExpiration`, `IsAcknowledged`, `AcknowledgedAt` |

All PKs are `Guid`. Closing a warehouse is a soft delete (`IsActive = false`). Products are hard-deleted.

---

## 5. Feature list

### Warehouse management
- List all warehouses with active/inactive filter
- View warehouse detail including all sections and product counts
- Create, edit, and rename warehouses
- Deactivate / reactivate warehouses (soft delete)
- Add, rename, and delete sections (deletion blocked if section has products)

### Product management
- Paginated product list with multi-field filtering:
  - Free-text search (name, SKU — powered by SQLite FTS5)
  - Filter by category, warehouse, section, size, type
  - Filter by expiring within N days
- Create and edit products with a tabbed modal:
  - **Details tab** — name, SKU, description, size, type, expiration date
  - **Location tab** — warehouse + section picker (inactive warehouse shown if product already lives there)
  - **Categories tab** — multi-select checkboxes
  - **Image tab** — drag-and-drop upload with current image preview when editing
- Delete product with confirmation dialog
- Form validation with inline error messages; failed submit auto-switches to the tab containing the first error
- Expiration date stored as noon-UTC to avoid timezone drift

### Image search
- Drop any JPG/PNG/WebP/GIF onto the search page
- Backend computes an aHash 8×8 (64-bit perceptual hash) for the query image
- Compares against stored hashes for all products that have images
- Returns matches with Hamming distance ≤ 6 (configurable), ordered closest-first
- Results show "Exact match" (0 bits), green for ≤ 3 bits, amber for 4–6 bits

### Categories
- List all categories with product counts
- Create, edit, and delete categories
- Deletion blocked if any products still use the category

### Expiration alerts
- Dashboard banner shows count of products expiring within 30 days
- Alerts page lists all unacknowledged alerts with product name, SKU, location, and days remaining
- Acknowledge individual alerts
- Acknowledged alerts are excluded from future scans so re-alerts are supported after acknowledgement
- **Real-time**: new alerts pushed instantly via SignalR (`ReceiveAlert` event) — page updates live and a toast notification appears without refreshing

### Dashboard
- Stat cards: active warehouses, total products, expiring ≤ 30 days, expired products
- Warning banner linking to alerts when any products are expiring
- Warehouse list with active/inactive status
- "Expiring soon" panel showing the nearest 6 expiry dates
- Recent products list with numbered rows

---

## 6. API reference

All endpoints require `Authorization: Bearer <token>` except `/api/auth/login` and `/api/auth/refresh`.

Errors return `application/problem+json` (RFC 7807).

### Authentication
| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/login` | Returns `accessToken` + `refreshToken` |
| POST | `/api/auth/refresh` | Rotates refresh token, returns new pair |

### Warehouses
| Method | Route | Description |
|---|---|---|
| GET | `/api/warehouses?isActive=true` | List warehouses |
| GET | `/api/warehouses/{id}` | Detail with sections |
| POST | `/api/warehouses` | Create |
| PUT | `/api/warehouses/{id}` | Update name / location |
| PATCH | `/api/warehouses/{id}/deactivate` | Soft-delete |
| PATCH | `/api/warehouses/{id}/reactivate` | Restore |
| GET | `/api/warehouses/{id}/products` | All products in warehouse |

### Warehouse sections
| Method | Route | Description |
|---|---|---|
| GET | `/api/warehouses/{wId}/sections` | List sections |
| POST | `/api/warehouses/{wId}/sections` | Create section |
| PUT | `/api/warehouses/{wId}/sections/{id}` | Rename |
| DELETE | `/api/warehouses/{wId}/sections/{id}` | Delete (only if empty) |

### Categories
| Method | Route | Description |
|---|---|---|
| GET | `/api/categories` | List all |
| GET | `/api/categories/{id}` | Single |
| POST | `/api/categories` | Create |
| PUT | `/api/categories/{id}` | Update |
| DELETE | `/api/categories/{id}` | Delete (only if unused) |

### Products
| Method | Route | Description |
|---|---|---|
| GET | `/api/products?query=&categoryId=&warehouseId=&sectionId=&size=&type=&expiringWithinDays=&page=&pageSize=` | Paginated search |
| POST | `/api/products` | Create |
| GET | `/api/products/{id}` | Single with full graph |
| PUT | `/api/products/{id}` | Update |
| DELETE | `/api/products/{id}` | Delete |
| GET | `/api/products/expiring?days=30` | Expiring within N days |
| POST | `/api/products/{id}/image` | Upload product image (multipart) |
| POST | `/api/products/search-by-image?maxResults=12&maxHammingDistance=6` | Image similarity search |

### Alerts
| Method | Route | Description |
|---|---|---|
| GET | `/api/alerts` | List unacknowledged alerts |
| PATCH | `/api/alerts/{id}/acknowledge` | Acknowledge alert |
| POST | `/api/alerts/scan?withinDays=30` | Trigger manual expiration scan |

### SignalR hub
| Endpoint | Event | Payload |
|---|---|---|
| `/hubs/alerts` | `ReceiveAlert` | `{ alertId, productId, productName, productSku, expirationDate, daysUntilExpiration }` |

### Status codes
- `200 OK` — successful GET / search
- `201 Created` with `Location` header — creates
- `204 No Content` — deletes / acknowledges / deactivates
- `400 Bad Request` — validation failure (`ValidationProblemDetails`)
- `401 Unauthorized` — missing or expired token
- `404 Not Found` — resource not found
- `409 Conflict` — uniqueness or business-rule violation
- `500 Internal Server Error` — unhandled (logged via Serilog)

---

## 7. Frontend pages

| Route | Page | Description |
|---|---|---|
| `/login` | Login | Username + password form |
| `/` | Dashboard | Stats, warehouse list, expiring panel, recent products |
| `/warehouses` | Warehouses | List with create/edit/deactivate/reactivate |
| `/warehouses/:id` | Warehouse detail | Sections CRUD, product list, rename/deactivate |
| `/products` | Products | Filtered paginated table, create/edit/delete modal |
| `/products/search-by-image` | Image search | Drag-and-drop image → visual similarity results |
| `/categories` | Categories | List with create/edit/delete |
| `/alerts` | Alerts | Real-time alert feed with acknowledge action |

---

## 8. Authentication & security

- Single-owner system: one `username` / `password` pair in `appsettings.json`
- Login returns a short-lived JWT (default 8 hours) and a 64-byte random refresh token
- Refresh token is stored server-side in memory with expiry; old token is consumed on use (rotation)
- Frontend Axios interceptor automatically retries any 401 response by calling `/api/auth/refresh`, then replaying the original request — transparent to UI components
- Multiple concurrent 401s are queued and resolved together after a single refresh round-trip
- On refresh failure, all queued requests are rejected and the user is redirected to `/login`
- SignalR hub connection passes the access token via `accessTokenFactory`

---

## 9. Background jobs & real-time alerts

`ExpirationCheckerJob` (`BackgroundService`):

1. Sleeps until the next 08:00 wall-clock boundary, then repeats every 24 hours
2. Opens a fresh DI scope and calls `ExpirationMonitorService.RunAsync(30)`
3. For each product expiring within 30 days that has no open (unacknowledged) alert:
   - Inserts a new `ExpirationAlert`
   - Pushes a `ReceiveAlert` message to all connected SignalR clients
   - Logs a structured warning via Serilog
4. Manual trigger available at `POST /api/alerts/scan?withinDays=30`

---

## 10. Image search

1. Staff uploads a photo of a product (drag-and-drop or file picker)
2. Backend computes an **aHash 8×8** — scales the image to 8×8 greyscale, compares each pixel to the row mean, produces a 64-bit fingerprint
3. Query hash is compared to stored hashes using **Hamming distance** (count of differing bits via `BitOperations.PopCount(a ^ b)`)
4. Results with distance ≤ 6 are returned ordered by distance ascending
5. Interpretation: 0 = identical, 1–3 = very similar, 4–6 = probably same product in different lighting/angle, 7+ = different image (excluded)
6. Product images are stored under `wwwroot/product-images/` and served as static files

---

## 11. Seed data

On first run with an empty database, `DbSeeder` inserts:

- **5 warehouses:** Skopje Main Store, Bitola Branch, Strumica Branch, Ohrid Seasonal, Veles Storage (inactive)
- **4 sections per warehouse:** Perfumes, Accessories, Skincare & Makeup, Bags & Shoes
- **7 categories:** Perfume, Eau de Toilette, Skincare, Handbag, Shoes, Makeup, Accessories
- **100 products** across all categories with realistic brand names, sizes, and SKUs:
  - Fragrances: Dior, Chanel, Hermès, YSL, Givenchy, Lancôme, Valentino, Burberry, Gucci, Paco Rabanne, Hugo Boss, Calvin Klein, Davidoff, Prada, Dolce & Gabbana
  - Handbags: Chanel, Dior, Hermès, Louis Vuitton, Gucci, Prada, Balenciaga, Bottega Veneta, Fendi, Celine
  - Shoes: Louboutin, Jimmy Choo, Gucci, Prada, Manolo Blahnik, Valentino, Saint Laurent, Versace
  - Makeup/Skincare: Dior, Chanel, YSL, Charlotte Tilbury, Armani, NARS, MAC, Lancôme, La Mer, Estée Lauder, SK-II, Sisley
  - Accessories: Hermès scarves, Gucci belts, Chanel brooches, Dior sunglasses, Prada wallets
- **Expiry mix:** 2 products already expired, 4 expiring within 30 days (for demo), rest 2026–2028

Delete `src/BoutiqueInventory.Api/Data/boutique.db` (and `*.db-shm`, `*.db-wal`) to reset and re-seed.

---

## 12. Running locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org) + npm

### Backend

```powershell
# From repo root
dotnet build

# Run API (listens on http://localhost:5030)
dotnet run --project src/BoutiqueInventory.Api --urls http://localhost:5030
```

The database is created and seeded automatically on first run. Migrations are applied via `db.Database.MigrateAsync()` at startup.

Open `http://localhost:5030/scalar` for the interactive API documentation.

Default credentials: **username** `owner` / **password** `12345678`

### Frontend

```powershell
# From repo root
cd boutique-ui
npm install
npm run dev
```

Frontend runs at `http://localhost:5173` and proxies `/api` and `/hubs` to `http://localhost:5030`.

### Quick smoke test

```powershell
# Login
$r = Invoke-RestMethod -Uri http://localhost:5030/api/auth/login -Method Post `
     -ContentType application/json -Body '{"username":"owner","password":"12345678"}'
$token = $r.accessToken

# List warehouses
Invoke-RestMethod -Uri http://localhost:5030/api/warehouses `
     -Headers @{Authorization="Bearer $token"}

# Trigger expiration scan
Invoke-RestMethod -Uri "http://localhost:5030/api/alerts/scan?withinDays=30" `
     -Method Post -Headers @{Authorization="Bearer $token"}
```

---

## 13. Configuration

All settings live in `src/BoutiqueInventory.Api/appsettings.json`:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/boutique.db"
  },
  "Auth": {
    "Username": "owner",
    "Password": "12345678",
    "Jwt": {
      "Issuer": "boutique-inventory",
      "Audience": "boutique-clients",
      "SigningKey": "dev-only-signing-key-min-32-chars-long!!",
      "ExpirationMinutes": 480,
      "RefreshExpirationDays": 7
    }
  },
  "Notifications": {
    "Email": { "Enabled": false },
    "Webhook": { "Enabled": false }
  }
}
```

Change `Auth.Username` / `Auth.Password` and `Jwt.SigningKey` before any deployment.

### Switching to SQL Server

1. Change `<TargetFramework>` to `net8.0` (or keep net10.0)
2. Replace `UseSqlite(...)` with `UseSqlServer(...)` in `Infrastructure/DependencyInjection.cs`
3. Update the connection string
4. Drop the SQLite Dapper type handlers (SQL Server handles `Guid` / `DateTimeOffset` natively)
5. Re-run `dotnet ef migrations add InitialCreate ...`

---

## 14. SQLite-specific notes

EF Core stores `Guid` and `DateTimeOffset` as `TEXT` in SQLite. Several adjustments keep Dapper round-tripping these correctly:

- `SqliteGuidTypeHandler` and `SqliteDateTimeOffsetTypeHandler` in `Infrastructure/Data/Dapper/` bind those types as canonical strings
- All Dapper Guid comparisons use `COLLATE NOCASE` (EF emits upper-case, `Guid.ToString()` emits lower-case)
- Queries comparing `DateTimeOffset` values are written as raw Dapper SQL instead of LINQ (the SQLite EF provider cannot translate them)

These workarounds disappear when switching to SQL Server.
