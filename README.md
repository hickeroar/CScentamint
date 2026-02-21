# CScentamint

Trainable naive Bayes text classification for .NET 10 as both:
- a reusable core library (`Cscentamint.Core`)
- an ASP.NET Core API (`Cscentamint.Api`)

---

## Why?

CScentamint is useful when you want lightweight, trainable text categorization without external services.

Typical use cases:
- spam vs ham filtering
- sentiment-like category assignment
- routing incoming messages to a best-fit category

You train categories with representative text, then:
- classify new text into one best category
- inspect relative category scores
- optionally persist and reload classifier state

## Installation

```bash
git clone git@github.com:hickeroar/CScentamint.git
cd CScentamint
dotnet restore
dotnet build --configuration Release
```

This repository is CLI-first for VSCode/Cursor workflows (no `.sln` file).

---

## Run as an API Server

```bash
dotnet run --project src/Cscentamint.Api/Cscentamint.Api.csproj
```

Optional auth can be configured with either:
- `Auth:Token` configuration key
- `auth-token` configuration key

When auth is configured, all endpoints except `/healthz` and `/readyz` require:

```text
Authorization: Bearer <token>
```

## Use as a Library in Your App

Reference `Cscentamint.Core` from your app:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Cscentamint.Core/Cscentamint.Core.csproj" />
</ItemGroup>
```

Library example (train, classify, score, untrain, persist, restore):

```csharp
using Cscentamint.Core;

ITextClassifier classifier = new InMemoryNaiveBayesClassifier();

classifier.Train("spam", "buy now limited offer click here");
classifier.Train("ham", "team meeting schedule for tomorrow");

var prediction = classifier.Classify("limited offer today");
Console.WriteLine($"category={prediction.PredictedCategory ?? "<none>"} score={prediction.Score}");

var scores = classifier.GetScores("team schedule update");
foreach (var score in scores)
{
    Console.WriteLine($"{score.Key}: {score.Value}");
}

classifier.Untrain("spam", "buy now limited offer click here");

classifier.SaveToFile("/tmp/cscentamint-model.bin");

ITextClassifier loaded = new InMemoryNaiveBayesClassifier();
loaded.LoadFromFile("/tmp/cscentamint-model.bin");
```

Notes for library usage:
- `Classify()` returns `PredictedCategory = null` and `Score = 0` when no category can be predicted.
- Scores are relative values, not calibrated probabilities.
- Category names accepted by `Train` and `Untrain` must match `^[a-zA-Z0-9_-]{1,64}$`.
- Stream APIs are available for non-file workflows: `Save(Stream)` and `Load(Stream)`.
- File helpers default to `/tmp/cscentamint-model.bin` when no path is provided.
- When a file path is provided, it must be absolute.

---

## Using the HTTP API

### API Notes
- Root text endpoints accept raw text request bodies.
- Native `/api/*` endpoints accept JSON request bodies: `{ "text": "<content>" }`.
- Root endpoint request size is capped at 1 MiB.
- Root payload-too-large response is `413` with: `{ "error": "request body too large" }`.
- Category route values use `[-_A-Za-z0-9]` semantics.
- `/healthz` and `/readyz` are intentionally unauthenticated.
- When auth is configured, all non-probe endpoints require `Authorization: Bearer <token>`.

### Common Error Responses

| Status | When |
| --- | --- |
| `400` | Invalid request payload or invalid arguments |
| `401` | Missing/invalid bearer token when auth is configured |
| `404` | Route mismatch (for example invalid root category route) |
| `405` | Wrong HTTP method |
| `413` | Root request body exceeds 1 MiB |

### Run-and-test quick start

Start the API:

```bash
dotnet run --project src/Cscentamint.Api/Cscentamint.Api.csproj
```

Optional auth header (if auth is configured):

```text
Authorization: Bearer <token>
```

---

### Native API (`/api/*`, JSON)

Request shape for JSON endpoints:

```json
{ "text": "text to process" }
```

#### Train category sample

`POST /api/categories/{category}/samples` -> `204 No Content`

```bash
curl -i -X POST "http://localhost:5000/api/categories/spam/samples" \
  -H "Content-Type: application/json" \
  -d '{ "text": "buy now limited offer click here" }'
```

#### Untrain category sample

`DELETE /api/categories/{category}/samples` -> `204 No Content`

```bash
curl -i -X DELETE "http://localhost:5000/api/categories/spam/samples" \
  -H "Content-Type: application/json" \
  -d '{ "text": "buy now limited offer click here" }'
```

#### Score text across categories

`POST /api/scores` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/api/scores" \
  -H "Content-Type: application/json" \
  -d '{ "text": "limited offer today" }'
```

Example response:

```json
{
  "ham": 2.438,
  "spam": 8.991
}
```

#### Classify text

`POST /api/classifications` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/api/classifications" \
  -H "Content-Type: application/json" \
  -d '{ "text": "limited offer today" }'
```

Example response:

```json
{
  "category": "spam",
  "score": 8.991
}
```

When no category can be predicted:

```json
{
  "category": null,
  "score": 0
}
```

#### Reset model state

`DELETE /api/model` -> `204 No Content`

```bash
curl -i -X DELETE "http://localhost:5000/api/model"
```

---

### Root text API (raw text body)

Root endpoints use plain text request bodies rather than JSON.

#### Get model summary

`GET /info` -> `200 OK`

```bash
curl -s "http://localhost:5000/info"
```

Example response:

```json
{
  "categories": {
    "ham": {
      "tokenTally": 24,
      "probNotInCat": 0.44,
      "probInCat": 0.56
    },
    "spam": {
      "tokenTally": 19,
      "probNotInCat": 0.56,
      "probInCat": 0.44
    }
  }
}
```

#### Train with raw text

`POST /train/{category}` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/train/spam" \
  -H "Content-Type: text/plain" \
  --data "buy now limited offer click here"
```

Example response:

```json
{
  "success": true,
  "categories": {
    "spam": {
      "tokenTally": 6,
      "probNotInCat": 0,
      "probInCat": 1
    }
  }
}
```

#### Untrain with raw text

`POST /untrain/{category}` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/untrain/spam" \
  -H "Content-Type: text/plain" \
  --data "buy now limited offer click here"
```

#### Classify with raw text

`POST /classify` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/classify" \
  -H "Content-Type: text/plain" \
  --data "limited offer today"
```

Example response:

```json
{
  "category": "spam",
  "score": 8.991
}
```

When no category can be predicted:

```json
{
  "category": "",
  "score": 0
}
```

#### Score with raw text

`POST /score` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/score" \
  -H "Content-Type: text/plain" \
  --data "limited offer today"
```

#### Flush root model state

`POST /flush` -> `200 OK`

```bash
curl -s -X POST "http://localhost:5000/flush" \
  -H "Content-Type: text/plain" \
  --data ""
```

Example response:

```json
{
  "success": true,
  "categories": {}
}
```

#### Probes

`GET /healthz` and `GET /readyz`:

```bash
curl -s "http://localhost:5000/healthz"
curl -i "http://localhost:5000/readyz"
```

Probe response examples:

```json
{ "status": "ok" }
```

```json
{ "status": "ready" }
```

or during drain:

```json
{ "status": "not ready" }
```

## Tokenization and scoring behavior

Default tokenizer pipeline:
- Unicode normalization (NFKC)
- lowercasing
- split on non-letter/non-digit boundaries
- basic English stemming

Scores are ranking values for comparison within the same model.

## Persistence behavior

- Model schema is versioned.
- Load validates category names, token counts, and tally invariants.
- Save uses temp-file + atomic replace.
- Default file path for null/whitespace file args: `/tmp/cscentamint-model.bin` (development fallback).
- For production, prefer an explicit absolute path owned by the service account and restrict file permissions.

## Project layout

- `src/Cscentamint.Core`: classifier logic, tokenizer pipeline, persistence model
- `src/Cscentamint.Api`: HTTP layer, root endpoints, auth/probe middleware
- `tests/Cscentamint.Core.UnitTests`: core behavior, persistence, property tests
- `tests/Cscentamint.Api.IntegrationTests`: end-to-end API behavior

## Development checks

```bash
dotnet test tests/Cscentamint.Core.UnitTests/Cscentamint.Core.UnitTests.csproj --configuration Release
dotnet test tests/Cscentamint.Api.IntegrationTests/Cscentamint.Api.IntegrationTests.csproj --configuration Release
```

Both test projects enforce 100% line/branch/method coverage for production assemblies.

CI (`.github/workflows/tests-and-coverage.yml`) includes:
- coverage-gated tests
- static analysis/build
- scheduled/manual smoke slices
