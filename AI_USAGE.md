# AI usage

The repository scaffold, persistence model, and related documentation were
prepared with an AI assistant using Copilot SDK in VS Code.

## Usage

- Reviewed the assignment requirements and drafted the initial project plan.
- Created the initial backend and frontend project scaffolds and repository
  documentation.
- Added the EF Core model, PostgreSQL provider configuration, initial Code
  First migration, and database setup instructions.

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
  endpoints, business logic, or UI behavior have been generated in this stage.
