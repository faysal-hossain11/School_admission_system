# AI usage

The repository scaffold, persistence model, class API, and related
documentation were prepared with an AI assistant using Copilot SDK in VS Code.

## Usage

- Reviewed the assignment requirements and drafted the initial project plan.
- Created the initial backend and frontend project scaffolds and repository
  documentation.
- Added the EF Core model, PostgreSQL provider configuration, initial Code
  First migration, and database setup instructions.
- Added the three class endpoints, request/response DTOs, a class repository,
  and HTTP contract tests using an isolated in-memory test database.

## Corrections and verification

- The initial assumptions document described the 17-digit student identifier
  as an exact requirement. The assignment example supports 17 digits, but the
  exact length is not explicitly stated as mandatory. This was corrected to a
  provisional assumption and documented in `ASSUMPTIONS.md`.
- An initial EF migration-list check tried to connect to the local PostgreSQL
  service with a temporary placeholder connection string and was rejected by
  authentication. No database changes were applied. Migration generation and
  SQL-script generation succeeded; the model snapshot was separately verified
  to have no pending changes.
- The backend build completed with no warnings or errors. No application
  hold workflows or UI behavior have been generated in this stage.
- Initial API-test attempts exposed two test setup issues: the test host needed
  the required connection-string environment variable before application
  startup, and each factory needed to reuse one in-memory database across
  requests. Both were corrected; all four HTTP contract tests then passed.
- Package compatibility warnings showed the Npgsql EF Core 8 provider and the
  EF Core design package were on different patch versions. EF Core packages
  and the repository-local EF tool were aligned to version 8.0.11.
- The backend and API tests were verified without PostgreSQL credentials.
  PostgreSQL migration SQL generation was checked, but no live database schema
  application or PostgreSQL-backed endpoint test was performed.
