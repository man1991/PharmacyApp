# ABC Pharmacy — Medicine Tracker

A Single Page Application built with **ASP.NET Core Web API (.NET 8)** and a
framework-free **JavaScript** front end, per the assessment requirements.
Data is persisted as **JSON files** on the server (no database required).

## How the requirements are met

| Requirement | Where |
|---|---|
| View list of medicines in a grid | `wwwroot/index.html` + `js/app.js` render `GET /api/medicines` into a `<table>` |
| Notes excluded from the grid | Grid columns intentionally omit `notes` (still returned by the API, used only if you build a detail view) |
| Red background: expiry < 30 days | `Medicine.IsNearExpiry` (server) / `isNearExpiry()` (client) → CSS class `row-expiry` |
| Yellow background: quantity < 10 | `Medicine.IsLowStock` (server) / `isLowStock()` (client) → CSS class `row-lowstock` |
| Add medicine details | "Add Medicine" modal → `POST /api/medicines` |
| Search by medicine name | Search box → `GET /api/medicines?search=...` |
| Maintain sale records | "Sell" button per row → `POST /api/sales` (reduces stock + appends a sale record); sales history table → `GET /api/sales` |
| .NET Core Web API | `PharmacyApp.Api` project, ASP.NET Core 8 |
| JavaScript front end | Vanilla JS SPA served from `wwwroot/` (no build step needed) |
| JSON storage server-side | `Data/medicines.json`, `Data/sales.json`, via `Services/JsonFileStore.cs` |

## Project layout

```
PharmacyApp/
├── PharmacyApp.sln
└── PharmacyApp.Api/
    ├── Program.cs                     # app startup, middleware pipeline, DI
    ├── appsettings.json
    ├── Controllers/
    │   ├── MedicinesController.cs     # CRUD + search
    │   └── SalesController.cs         # record sale + sales history
    ├── Models/
    │   ├── Medicine.cs                # domain model + business flags (IsNearExpiry / IsLowStock)
    │   ├── MedicineDtos.cs            # request DTOs with data-annotation validation
    │   └── SaleRecord.cs
    ├── Services/
    │   ├── IMedicineService.cs / MedicineService.cs
    │   ├── ISaleService.cs / SaleService.cs
    │   └── JsonFileStore.cs           # generic, thread-safe JSON read/write helper
    ├── Exceptions/
    │   └── AppExceptions.cs           # NotFoundException, BusinessRuleException, DataStoreException
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs  # global error handler → friendly JSON responses
    ├── Data/
    │   ├── medicines.json             # seeded sample data
    │   └── sales.json
    └── wwwroot/                       # the SPA
        ├── index.html
        ├── css/styles.css
        └── js/app.js
```

## Design notes

- **Layered architecture.** Controllers are thin (HTTP concerns only) and delegate to
  `Services`, which own business rules and persistence. This keeps the code testable
  and easy to extend (e.g. swapping the JSON store for a real database later just
  means writing a new `IMedicineService`/`ISaleService` implementation).
- **DTOs vs domain models.** `MedicineCreateDto` / `MedicineUpdateDto` are separate
  from `Medicine` so a client can never set server-controlled fields (like `Id`), and
  so validation rules live in one obvious place (`System.ComponentModel.DataAnnotations`).
- **Thread-safe JSON storage.** `JsonFileStore<T>` guards every read/write with a
  `SemaphoreSlim` and writes via a temp-file-then-swap so concurrent requests can't
  corrupt the file or silently overwrite each other's changes.
- **Centralized exception handling.** Services/controllers simply `throw` a
  `NotFoundException` or `BusinessRuleException` when something goes wrong; a single
  `ExceptionHandlingMiddleware` converts every exception (expected or not) into a
  consistent `{ success, error, traceId }` JSON response with a **user-friendly**
  message, while the real exception detail is only ever written to the server log
  (never leaked to the client).
- **Selling reduces stock atomically.** `SaleService.RecordSaleAsync` calls
  `MedicineService.ReduceStockAsync`, which validates there's enough stock *and*
  decrements it within a single locked read-modify-write — so two near-simultaneous
  sales can't oversell the same batch.
- **No framework/build step for the front end.** Plain HTML/CSS/JS keeps the
  assessment easy to run and review; `apiRequest()` in `app.js` centralizes fetch
  error handling so every screen shows the same friendly messages the API returns.

## Running the solution

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
cd PharmacyApp/PharmacyApp.Api
dotnet run
```

Then open the URL shown in the console (e.g. `http://localhost:5080`) — the API
serves the SPA directly from `wwwroot/`, so there's nothing else to start.

- Swagger UI (API docs, useful for manual testing): `http://localhost:5080/swagger`
- API base path: `http://localhost:5080/api`

### API endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/medicines?search=` | List medicines, optional name filter |
| GET | `/api/medicines/{id}` | Get one medicine |
| POST | `/api/medicines` | Add a medicine |
| PUT | `/api/medicines/{id}` | Update a medicine |
| DELETE | `/api/medicines/{id}` | Remove a medicine |
| GET | `/api/sales` | List sale records (most recent first) |
| POST | `/api/sales` | Record a sale `{ medicineId, quantitySold }` — reduces stock |

## Notes / possible next steps

- This sandbox does not have the .NET SDK installed, so the code was written and
  reviewed carefully but could not be compiled here — please run `dotnet build`
  after downloading to compile and try it locally.
- For production use, the flat JSON file store would typically be swapped for a
  real database (e.g. SQL Server/PostgreSQL via EF Core) — the `IMedicineService`
  / `ISaleService` interfaces make that a contained change.
