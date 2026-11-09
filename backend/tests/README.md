# Backend tests

Run the backend tests from the repository root:

```text
dotnet test backend/SchoolAdmission.sln
```

The class API contract tests use an isolated EF Core in-memory database and
exercise the HTTP endpoints without requiring PostgreSQL credentials. The
assignment-required backend business-rule and concurrency tests remain
scheduled for Stage 5.
