# To-Do — Task Management

A small, production-minded to-do app: a **.NET** REST API, a **React + TypeScript**
frontend, **SQLite** storage, and **JWT** authentication. Each user signs in and
manages only their own tasks.

---

## Quick start

### Prerequisites

| Tool        | Version    | Pinned by      |
| ----------- | ---------- | -------------- |
| .NET SDK    | 10.0.301   | `global.json`  |
| Node.js     | 26.3.0     | `.nvmrc`       |

(`global.json` / `.nvmrc` pin exact versions
If you use `nvm`, run `nvm use` in `frontend/`.)

### Run it (two terminals)

**Terminal 1 — backend** (http://localhost:5180):

```bash
cd backend
dotnet run
```

On first run this creates `backend/todo.db` (SQLite), applies the schema, and seeds
a demo account.

**Terminal 2 — frontend** (http://localhost:5173):

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173** and log in with the seeded account:

```
Email:    demo@todo.app
Password: Todo-Demo-Acct-2026!
```

Or click **Sign up** to create your own account.

### Run the tests

```bash
cd tests
dotnet test
```

---

## What you can do

- Register / log in (the demo account is pre-filled on the sign-in screen).
- Add a task, optionally with a due date.
- Check a task off (complete) or delete it.
- Overdue tasks (past due date, not done) are highlighted.

---

## Project structure

```
todo/
├── backend/            .NET minimal-API + EF Core + Identity
│   ├── Program.cs              composition root (registers services, maps endpoints)
│   ├── HostingExtensions.cs    DI registration + startup DB seeding helpers
│   ├── Endpoints/              AuthEndpoints + TaskEndpoints (routes, request DTOs, validation)
│   ├── Features/Account/       AccountService (register/login logic, HTTP-agnostic)
│   ├── Auth/                   JwtOptions, TokenService, ClaimsPrincipal helpers
│   ├── Models/                 TodoItem
│   ├── Data/                   DbContext + seed data
│   └── Migrations/             EF Core schema migrations
├── frontend/           Vite + React + TypeScript
│   └── src/
│       ├── App.tsx            UI (auth form + task list)
│       ├── useTasks.ts        hook owning task state + API calls
│       ├── api.ts             thin fetch wrapper (attaches the JWT)
│       └── index.css          plain CSS (semantic classes, no UI library)
├── tests/              xUnit integration tests (high-risk areas)
├── docs/adr/           Architecture Decision Records
```

The backend is organized as **vertical slices**: each feature's routes, request
shapes, and validation live together in one endpoint file, with `Program.cs` kept
to a thin composition root.

---

## API

All `/api/tasks` routes require an `Authorization: Bearer <jwt>` header and act only
on the caller's own tasks.

| Method   | Route                | Body                              | Notes                          |
| -------- | -------------------- | --------------------------------- | ------------------------------ |
| `POST`   | `/api/auth/register` | `{ email, password }`             | Creates a user, returns a JWT  |
| `POST`   | `/api/auth/login`    | `{ email, password }`             | Returns a JWT                  |
| `GET`    | `/api/tasks`         | —                                 | The caller's tasks, newest first |
| `POST`   | `/api/tasks`         | `{ title, dueDate? }`             | Create                         |
| `PUT`    | `/api/tasks/{id}`    | `{ title, isComplete, dueDate? }` | Edit / toggle complete         |
| `DELETE` | `/api/tasks/{id}`    | —                                 | Delete                         |

- `dueDate` is optional and, when present, must be `YYYY-MM-DD`.
- Validation failures return HTTP 400 as `ValidationProblemDetails` (messages keyed
  by field), which the UI shows inline.
- Acting on a task you don't own returns **404, not 403** — deliberately, so the API
  doesn't reveal whether another user's task id exists.

---

## How communication works

In **development**, the React dev server proxies `/api/*` to the backend
(`frontend/vite.config.ts`). The frontend therefore calls **relative URLs** and there
is **no CORS** config and no hardcoded backend URL. If you change the backend port,
update the proxy target there.

For **production** see [Deployment](#deployment-not-included).

---

## Key decisions (and the trade-offs)

- **Durable file-based SQLite (not in-memory).** The brief mentioned in-memory
  SQLite, but data must survive a restart, so the app uses a `todo.db` file. Same
  engine, zero external infrastructure. In-memory SQLite is still used — in the
  tests, where throwaway databases are exactly what you want.
- **EF Core + ASP.NET Identity.** I went with Identity because it was easy to setup
  and handles the security parts that I didn't want to mess with. I chose EF Core 
  because it pairs with ASP.NET Identity out of the box. 
- **JWT bearer tokens.** Stateless auth that suits a SPA + API split. (See Security
  notes for the hardening this MVP intentionally defers.)
- **Minimal frontend.** React hooks + a small fetch wrapper, no UI library and no
  state-management library

---

## Assumptions

- **Single-tenant, per-user data.** No teams, sharing, or roles — a task has exactly
  one owner.
- **One backend instance.** File-based SQLite is single-writer; this MVP assumes a
  single API process (see Scalability).
- **No email server.** So email confirmation, password reset, and 2FA are out of
  scope for now (see Future work).
- **`CreatedAt` is stored UTC** and currently used only for sorting. It is never
  displayed; if you ever show it, convert UTC → local first (when read back from
  SQLite its `DateTimeKind` is `Unspecified`, but the value is UTC).
- **Due dates are date-only and timezone-independent.** A due date is a calendar day
  (`DateOnly`), not an instant, so it never shifts when a user changes timezone.
  "Overdue" is computed against the user's **local** date.

---

## Security notes

- Passwords are hashed by ASP.NET Identity
- The dev JWT signing key lives in `appsettings.Development.json` and is clearly
  marked as throwaway. **In production, supply your own key** via the `Jwt__Key`
  environment variable (and rotate it); never ship the dev key.
- **Token storage / lifetime (deferred hardening):** the MVP stores the JWT in the
  browser and uses a single ~2-hour token.

---

## Testing

Tests cover the following:

- **Ownership isolation** (broken-access-control / IDOR): a user cannot read, update,
  or delete another user's tasks; unauthenticated requests get 401.
- **Auth flow**: login succeeds with valid credentials and is rejected otherwise.

They boot the real API in-process (`WebApplicationFactory`) against **in-memory
SQLite**, so they're fast and need no setup. 

---

## Scalability — where this would go next

This is an MVP; the structure is meant to make the following cheap:

- **Database.** File-based SQLite is single-writer and lives on one machine. The
  first scaling step is **Postgres** — because data access goes through EF Core, this
  is largely a provider + connection-string change, not a rewrite. That unlocks
  multiple backend instances and real write concurrency.
- **Stateless API.** Auth is already stateless (JWT), so the API can be scaled
  horizontally behind a load balancer once the database is shared.
- **Pagination.** `GET /api/tasks` currently returns all of a user's tasks. Add
  cursor/offset pagination before anyone accumulates thousands.
- **Observability.** Add structured logging, request tracing, and health checks for a
  real deployment.

---

## Deployment (not included)

For production, I would use a reverse proxy like nginx:

- An **nginx** container serves the built React files **and** proxies `/api/*` to the
  backend. One origin → **no CORS**, mirroring the dev proxy.
- Alternatively, serve the frontend from a static host and the API from another
  origin; then enable **CORS** on the backend for the frontend's origin.

Either way: provide a production `Jwt__Key`, and point the connection string at a
durable database (Postgres for multi-instance).

---

## Future work

- **Time-of-day on due dates.** Due dates are date-only today (`DateOnly`) — a whole
  calendar day. A planned enhancement is an *optional* time (e.g. "due Jul 1, 5 pm").
  Trade-off: a due *time* is a specific instant, which reintroduces timezone handling
  (it'd need a `DateTimeOffset` or stored-UTC + user-timezone, plus zone-aware
  "overdue" logic) — the very complexity the date-only model currently avoids. A
  clean way to do it is to move the "overdue" calculation to a tested backend
  computation.
- **Account flows:** email confirmation, password reset, and 2FA (need an SMTP/email
  provider).
- **Token hardening:** short-lived access token + httpOnly refresh-token cookie.
- **Richer tasks:** priority, description, tags, sub-tasks, search/filter.
- **Pagination** on the task list.
- **Postgres + horizontal scaling**, and a shipped Docker/compose deployment.
