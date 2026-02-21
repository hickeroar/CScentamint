# Cscentamint

Trainable Naive Bayes text classification for .NET 10 with:

- native ASP.NET Core routes under `/api/*`
- root text endpoints (`/train`, `/classify`, etc.)
- strict quality gates (100% line/branch/method coverage in both test projects)

The repository is CLI-first for VSCode/Cursor workflows (no `.sln` file).

## Project layout

- `src/Cscentamint.Core`: classifier logic, tokenization pipeline, persistence model.
- `src/Cscentamint.Api`: HTTP layer, root text endpoints, auth/probe middleware.
- `tests/Cscentamint.Core.UnitTests`: core behavior, persistence, and property tests.
- `tests/Cscentamint.Api.IntegrationTests`: end-to-end API coverage.

## Requirements

- .NET 10 SDK

## Run locally

```bash
dotnet restore
dotnet run --project src/Cscentamint.Api/Cscentamint.Api.csproj
```

## API surfaces

### Native Cscentamint API (`/api/*`, JSON request bodies)

Request shape:

```json
{ "text": "text to process" }
```

- `POST /api/categories/{category}/samples` -> train sample, `204`
- `DELETE /api/categories/{category}/samples` -> untrain sample, `204`
- `POST /api/scores` -> `{ "<category>": <score> }`
- `POST /api/classifications` -> `{ "category": "<category>|null", "score": <float> }`
- `DELETE /api/model` -> reset model, `204`

### Root text API (raw text body)

- `GET /info` -> `{ "categories": { "<name>": { "tokenTally", "probNotInCat", "probInCat" } } }`
- `POST /train/{category}` -> `{ "success": true, "categories": { ... } }`
- `POST /untrain/{category}` -> `{ "success": true, "categories": { ... } }`
- `POST /classify` -> `{ "category": "<name>|\"\"", "score": <float> }`
- `POST /score` -> `{ "<category>": <score> }`
- `POST /flush` -> `{ "success": true, "categories": {} }`
- `GET /healthz` -> `{ "status": "ok" }`
- `GET /readyz` -> `{ "status": "ready" }` or `503 { "status": "not ready" }`

## Auth, limits, and behavior

- Optional bearer auth:
  - Configure `Auth:Token` (or `auth-token`).
  - When configured, non-probe endpoints require `Authorization: Bearer <token>`.
  - Unauthorized response: `401` with `WWW-Authenticate: Bearer realm="cscentamint"`.
- Probe endpoints (`/healthz`, `/readyz`) are intentionally unauthenticated.
- Root text endpoints enforce a 1 MiB request-body limit and return `413` with:
  - `{ "error": "request body too large" }`
- Category names must match `^[-_A-Za-z0-9]+$` semantics.

## Tokenization and scoring

Default pipeline:

- Unicode normalization (NFKC)
- lowercasing
- split on non-letter/non-digit characters
- basic English stemming

Score values are relative ranking values, not calibrated probabilities.

## Persistence

Core classifier supports explicit persistence APIs:

- `Save(Stream)` / `Load(Stream)`
- `SaveToFile(string? absolutePath)` / `LoadFromFile(string? absolutePath)`

Persistence notes:

- model schema is versioned
- load validates category names, token counts, and tally invariants
- file save uses temp-file + atomic replace
- default path for null file arguments: `/tmp/cscentamint-model.bin` (development fallback)
- for production, provide an explicit absolute model path owned by the service account
- for production, restrict file and directory permissions so only the service account can read/write model files

The HTTP API remains memory-first; persistence is opt-in via core APIs.

## Test and quality gates

Run locally:

```bash
dotnet test tests/Cscentamint.Core.UnitTests/Cscentamint.Core.UnitTests.csproj --configuration Release
dotnet test tests/Cscentamint.Api.IntegrationTests/Cscentamint.Api.IntegrationTests.csproj --configuration Release
```

Both test projects enforce 100% line/branch/method coverage for production assemblies:

- `Cscentamint.Core.UnitTests` -> `Cscentamint.Core`
- `Cscentamint.Api.IntegrationTests` -> `Cscentamint.Api`

CI (`.github/workflows/tests-and-coverage.yml`) includes:

- coverage-gated test job
- static analysis/build job
- scheduled/manual extended smoke job (property + concurrency stress slices)

## Developer workflow

- Use `.vscode/tasks.json` for restore/build/test/run/watch tasks.
- Use `.vscode/launch.json` for API debugging.
- Formatting and analyzer defaults live in `.editorconfig`, `Directory.Build.props`, and `Directory.Packages.props`.
