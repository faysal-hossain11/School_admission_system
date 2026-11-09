# Backend tests

Run the backend tests from the repository root:

```text
dotnet test backend/SchoolAdmission.sln
```

The class API contract tests use an isolated EF Core in-memory database.
Hold lifecycle and concurrency tests exercise the HTTP endpoints against the
configured PostgreSQL database, including the row-locking seat allocation
path. Configure `ConnectionStrings__AdmissionDatabase` to use a test database
before running the full suite. The PostgreSQL tests create uniquely named
classes and remove their hold and class records when each test finishes.
