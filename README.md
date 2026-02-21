# Cscentamint
A trainable web API for naive Bayesian text classification on ASP.NET Core and .NET 10.

This repository is optimized for CLI-first development in VSCode/Cursor (no Visual Studio solution file).

## Projects

- `src/Cscentamint.Core`: classifier domain logic and in-memory model implementation.
- `src/Cscentamint.Api`: ASP.NET Core transport layer and HTTP contracts.
- `tests/Cscentamint.Core.UnitTests`: classifier unit tests.
- `tests/Cscentamint.Api.IntegrationTests`: API integration tests with in-memory host.

## Requirements

- .NET 10 SDK

## Run

```bash
dotnet restore
dotnet run --project src/Cscentamint.Api/Cscentamint.Api.csproj
```

The API is exposed through controllers under `/api/*`.

## Test

```bash
dotnet test tests/Cscentamint.Core.UnitTests/Cscentamint.Core.UnitTests.csproj
dotnet test tests/Cscentamint.Api.IntegrationTests/Cscentamint.Api.IntegrationTests.csproj
```

Both test projects enforce **100% line, branch, and method coverage** for production code:

- `Cscentamint.Core.UnitTests` enforces coverage for `Cscentamint.Core`.
- `Cscentamint.Api.IntegrationTests` enforces coverage for `Cscentamint.Api`.

The same `dotnet test` commands above are used in CI.

### Coverage reports

Running tests generates Cobertura reports at:

- `tests/Cscentamint.Core.UnitTests/coverage.cobertura.xml`
- `tests/Cscentamint.Api.IntegrationTests/coverage.cobertura.xml`

## VSCode/Cursor workflow

- Use `.vscode/tasks.json` for restore/build/test/run/watch tasks.
- Use `.vscode/launch.json` to debug `Cscentamint.Api`.
- Workspace code style and build defaults are set in `.editorconfig`, `Directory.Build.props`, and `Directory.Packages.props`.

## API

Most request bodies are JSON with the shape:

```json
{ "text": "text to process" }
```

- `POST /api/categories/{category}/samples`: train category with request body; returns `204 No Content`.
- `DELETE /api/categories/{category}/samples`: untrain category with request body; returns `204 No Content`.
- `POST /api/scores`: returns `{ "<category>": <score> }`.
- `POST /api/classifications`: returns `{ "category": "<category>|null" }`.
- `DELETE /api/model`: clears all in-memory learned state; returns `204 No Content`.

### Validation and errors

- Category must be 1-64 characters and contain only letters, numbers, `_`, or `-`.
- Request body `text` is required, min length 1, max length 4000.
- Validation and argument errors return RFC-style `ProblemDetails` JSON (`400 Bad Request`).

### Operational notes

- The classifier is intentionally in-memory and process-local.
- Data is not persisted and is reset when the process restarts or when `DELETE /api/model` is called.
