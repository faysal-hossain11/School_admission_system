# School Admission System

This repository is being developed in ten committed stages for the School
Admission System assignment. The PDF assignment is confidential and is not
included in this repository.

## Current status

Stage 1 is complete: this repository contains an empty .NET and React project
scaffold and planning documentation. No admission APIs, database entities,
business logic, or UI features are implemented yet.

## Repository layout

```text
backend/
  src/
    SchoolAdmission.Api/
  tests/
frontend/
  src/
ASSUMPTIONS.md
AI_USAGE.md
README.md
```

The backend targets .NET 8. The frontend scaffold uses React 18, TypeScript,
and Vite. Root-level Docker Compose, Dockerfiles, and runtime instructions will
be added in a later stage.

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

## Development

The scaffold can be restored and built with .NET 8 and Node.js installed:

```text
dotnet build backend/SchoolAdmission.sln
cd frontend
npm install
npm run build
```

These commands validate only the project scaffolds; there are no application
features or tests at this stage. Docker Compose run instructions and test
commands will be documented as those capabilities are implemented.
