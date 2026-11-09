# School Admission System

This repository is being developed in ten committed stages for the School
Admission System assignment. The PDF assignment is confidential and is not
included in this repository.

## Current status

Stage 2 is complete: the backend contains the PostgreSQL persistence model and
an initial EF Core migration. Admission APIs, application services, business
logic, and UI features have not yet been implemented.

## Repository layout

```text
backend/
  src/
    SchoolAdmission.Api/
      Data/
        Migrations/
      Models/
  tests/
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

## Frontend scaffold

The frontend scaffold can be built with Node.js installed:

```text
cd frontend
npm install
npm run build
```

No application workflows or automated tests are implemented yet. Docker
Compose run instructions and test commands will be documented as those
capabilities are implemented.
# School_admission_system
