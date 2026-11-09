# AI usage

The repository scaffold, persistence model, APIs, and related documentation
were prepared with an AI assistant using Copilot SDK in VS Code.

## Usage

- Reviewed the assignment requirements and drafted the initial project plan.
- Created the initial backend and frontend project scaffolds and repository
  documentation.
- Added the EF Core model, PostgreSQL provider configuration, initial Code
  First migration, and database setup instructions.
- Added the three class endpoints, request/response DTOs, a class repository,
  and HTTP contract tests using an isolated in-memory test database.
- Added the hold lifecycle APIs and service, PostgreSQL row-locking
  transactions, a hosted expiration worker, the active-application unique
  index and migration, and hold API documentation.
- Ran PostgreSQL-backed HTTP checks for all five hold endpoints, response
  fields and statuses, validation, duplicate prevention, ownership,
  confirmation idempotency, cancellation, expiry, seat release, expiry after
  restart, and concurrent requests against a limited class capacity. Also
  checked confirmed-duplicate and expired/cancelled transition rejection.

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
- The Stage 3 backend build completed with no warnings or errors. At that
  stage, hold workflows and UI behavior had not yet been implemented.
- Initial API-test attempts exposed two test setup issues: the test host needed
  the required connection-string environment variable before application
  startup, and each factory needed to reuse one in-memory database across
  requests. Both were corrected; all four HTTP contract tests then passed.
- Package compatibility warnings showed the Npgsql EF Core 8 provider and the
  EF Core design package were on different patch versions. EF Core packages
  and the repository-local EF tool were aligned to version 8.0.11.
- Initial Stage 4 builds caught invalid generic usage for value types in the
  result wrapper and DI registration; these were corrected before verification.
- An initial PostgreSQL HTTP verification attempt used a port already occupied
  locally. Verification was run on an available port without changing the
  configured application port. The first restart-check harness also omitted
  the URL environment variable on its restarted process; the harness was
  corrected and the restart-expiration check then passed.
- The Stage 3 API regression suite passed (4 tests). A shared process held the
  default backend output files, so the final solution build and regression
  tests were run using a temporary output directory.
- The Stage 4 migration was applied to the configured PostgreSQL database.
  PostgreSQL-backed API and concurrency verification was performed; no
  Stage 5 automated business-rule test suite has been added or run.
