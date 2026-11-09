# School Admission System

This repository is being developed in ten committed stages for the School
Admission System assignment. The PDF assignment is confidential and is not
included in this repository.

## Current status

Stage 4 is complete: the backend includes PostgreSQL persistence, class
management APIs, and the admission hold lifecycle. Stage 5 adds automated
backend tests for the hold lifecycle, ownership, duplicate prevention, and
concurrent seat allocation. Frontend features remain scheduled for later
stages.

## Repository layout

```text
backend/
  src/
    SchoolAdmission.Api/
      Contracts/
      Controllers/
      Data/
        Migrations/
      Models/
      Repositories/
      Services/
      Workers/
  tests/
    SchoolAdmission.Api.Tests/
frontend/
  src/
ASSUMPTIONS.md
AI_USAGE.md
README.md
```

The backend targets .NET 8 and uses Entity Framework Core with PostgreSQL. The
frontend scaffold uses React 18, TypeScript, and Vite. Root-level Docker
Compose, Dockerfiles, and runtime instructions will be added in a later stage.

## Planned development stages

1. Scaffold the repository and project layout.
2. Create the persistent data model.
3. Implement class APIs.
4. Implement the hold lifecycle and remaining APIs.
5. Add mandatory backend tests.
6. Build the core React UI.
7. Complete student workflows and live behavior.
8. Verify UI workflows and pursue the frontend-test bonus.
9. Containerize and compose the application.
10. Finalize documentation and submission checks.

## Class APIs

The backend exposes these endpoints:

- `POST /classes` creates a class and returns `201`.
- `GET /classes` returns classes with held, confirmed, and available seat
  summaries.
- `GET /classes/{classId}` returns one class summary or `404` when it does not
  exist.

The class response fields are `id`, `name`, and `totalSeats`. Summary responses
also include `heldSeats`, `confirmedSeats`, and `availableSeats`. Seat counts
are calculated from persisted holds; no separate counters are stored.

## Hold APIs and lifecycle

- `POST /classes/{classId}/holds` requires `X-Student-Id` and returns `201` or
  `409` when the class is full or the student already has an active or
  confirmed application.
- `GET /holds/{holdId}` returns one application or `404`.
- `POST /holds/{holdId}/confirm` requires `X-Student-Id` and confirms the
  owner's unexpired hold; repeated owner confirmation is idempotent.
- `DELETE /holds/{holdId}` requires `X-Student-Id` and cancels the owner's
  active hold with `204`.
- `GET /me/holds` requires `X-Student-Id` and returns that student's
  applications.

Set `HOLD_DURATION_SECONDS` to configure the hold duration; it defaults to
`600`. A hosted background worker expires due holds in PostgreSQL. Capacity
changes use transactions and lock the class row, and a partial unique index
prevents more than one `HELD` or `CONFIRMED` application per student and class.

## Backend database setup

Set `ConnectionStrings__AdmissionDatabase` in the backend process environment
to a PostgreSQL connection string. For example, use the connection details of
your own local PostgreSQL instance; do not commit credentials.

Restore the repository-local EF Core command-line tool and apply the checked-in
migrations from the `backend/` directory:

```text
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/SchoolAdmission.Api --startup-project src/SchoolAdmission.Api
```

Build the backend from the repository root:

```text
dotnet build backend/SchoolAdmission.sln
```

Run the backend tests from the repository root:

```text
dotnet test backend/SchoolAdmission.sln
```

The class API contract tests use an isolated in-memory database. Hold lifecycle
and concurrency tests use the configured PostgreSQL database and remove the
unique test classes they create. Configure the backend connection string to
point to a test database before running the full suite.

## Frontend scaffold

The frontend scaffold can be built with Node.js installed:

```text
cd frontend
npm install
npm run build
```

Frontend features are not implemented yet. Docker Compose run instructions
will be documented as those capabilities are implemented.
# School_admission_system
