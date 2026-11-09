# School Admission System

This repository is being developed in ten committed stages for the School
Admission System assignment. The PDF assignment is confidential and is not
included in this repository.

## Current status

Stages 1-7 are implemented: the backend includes PostgreSQL persistence, class
management APIs, and the admission hold lifecycle, with automated lifecycle
and concurrency tests. The React frontend supports class management and
student application workflows. Docker Compose is configured to build and run
the database, backend, and production frontend; end-to-end container
verification remains pending in an environment with Docker installed.

## Repository layout

```text
backend/
  Dockerfile
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
  Dockerfile
  src/
docker-compose.yml
ASSUMPTIONS.md
AI_USAGE.md
README.md
```

The backend targets .NET 8 and uses Entity Framework Core with PostgreSQL. The
frontend uses React 18, TypeScript, and Vite.

## Planned development stages

1. Scaffold the repository and project layout.
2. Create the persistent data model.
3. Implement class APIs.
4. Implement the hold lifecycle and remaining APIs.
5. Add mandatory backend tests.
6. Build the core React UI.
7. Containerize, compose, and finalize runtime documentation.
8. Verify UI workflows and pursue the frontend-test bonus.
9. Verify the optional multi-process bonus if pursued.
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

The API automatically applies checked-in migrations on startup when using a
relational database. To apply migrations manually instead, restore the
repository-local EF Core command-line tool and run this from the `backend/`
directory:

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

## Frontend

With the backend running at `http://localhost:8080`, start the frontend in
development mode:

```text
cd frontend
npm install
npm run dev
```

The browser sends API requests directly to `http://localhost:8080`. The
backend allows requests from the default Vite development and preview origins.
Build the static frontend bundle with:

```text
npm run build
```

The student registration number is stored in this browser's local storage.
Class seat summaries and student applications refresh every five seconds;
hold countdowns update each second.

## Run the complete application with Docker Compose

Install Docker Desktop (or Docker Engine with the Compose plugin), then run
from the repository root:

```text
docker compose up --build
```

Make sure host ports 8080 (API) and 3000 (frontend) are available.
Open the frontend at <http://localhost:3000>. The API listens on
<http://localhost:8080>; the browser uses that URL directly. PostgreSQL is
available to the API on the private Compose network and is not published to
the host. The backend waits for PostgreSQL health and applies its checked-in
EF Core migrations before serving requests.

The Compose file supplies local-development defaults for the PostgreSQL
database, user, and password. Override `POSTGRES_DB`, `POSTGRES_USER`, and
`POSTGRES_PASSWORD` in the shell or a local `.env` file when different
credentials are needed. Do not use the development defaults for production.
`HOLD_DURATION_SECONDS` can also be overridden; it defaults to 600.

Stop the containers with Ctrl+C or `docker compose down`. The named
`postgres_data` volume is retained by `docker compose down`, so data remains
available after a subsequent `docker compose up`. To intentionally remove the
database and persisted data, use `docker compose down --volumes`.

Run the backend tests with PostgreSQL available and configured as described
above:

```text
dotnet test backend/SchoolAdmission.sln
```

There is no automated frontend test project yet. Build-check the frontend
locally with `cd frontend`, `npm ci`, and `npm run build`, or build and start
the production containers with `docker compose up --build`.
# School_admission_system
