# Contributing

## Prerequisites

- .NET 10 SDK

## Local setup

```bash
dotnet restore
```

## Build

```bash
dotnet build src/Cscentamint.Core/Cscentamint.Core.csproj --configuration Release
dotnet build src/Cscentamint.Api/Cscentamint.Api.csproj --configuration Release
```

## Test

Run both suites (required before commit):

```bash
dotnet test tests/Cscentamint.Core.UnitTests/Cscentamint.Core.UnitTests.csproj --configuration Release
dotnet test tests/Cscentamint.Api.IntegrationTests/Cscentamint.Api.IntegrationTests.csproj --configuration Release
```

Coverage policy:

- `Cscentamint.Core.UnitTests` enforces 100% line/branch/method coverage for `Cscentamint.Core`.
- `Cscentamint.Api.IntegrationTests` enforces 100% line/branch/method coverage for `Cscentamint.Api`.

## CI workflow

CI runs:

- coverage-gated tests
- static analysis/build lane
- extended smoke lane on `workflow_dispatch` and nightly schedule

## API conventions

- Maintain both API surfaces:
  - native routes under `/api/*`
  - root text routes
- Keep root endpoint payloads and status behavior stable unless intentionally versioned.

## Documentation expectations

- Update `README.md` when endpoint behavior, auth, config, or persistence behavior changes.
- Add notable user-facing changes to `CHANGELOG.md`.
