# Gobayes Parity Checklist

This checklist is the implementation source-of-truth for bringing `cscentamint` to parity with `gobayes` while staying idiomatic for ASP.NET Core and .NET.

## Core classifier parity

- [x] Unknown tokens are ignored during scoring.
- [x] Score results only include categories with score > 0.
- [x] Empty model classification returns no category.
- [x] Category names are validated with `^[-_A-Za-z0-9]+$` equivalent.
- [x] Priors are based on category token tally totals (not vocabulary size).
- [x] Bayesian denominator uses `(P(token|notCat) * P(notCat)) + numerator`.
- [x] Untrain removes zero-count tokens and deletes empty categories.
- [x] Deterministic category tie-breaking is lexical.
- [x] Category summaries parity: `tokenTally`, `probInCat`, `probNotInCat`.

Gobayes references:
- `gobayes/bayes/bayes.go`
- `gobayes/bayes/category/category.go`
- `gobayes/bayes/category/categories.go`

## API parity

- [x] Compatibility endpoints: `/train`, `/untrain`, `/classify`, `/score`, `/flush`, `/info`, `/healthz`, `/readyz`.
- [x] Compatibility response payloads mirror gobayes DTOs.
- [x] Method mismatch returns `405` and `Allow` header.
- [x] Request size cap parity (`1 MiB`) for compatibility endpoints.
- [x] Invalid category route handling parity.

Gobayes references:
- `gobayes/gobayes.go`
- `gobayes/responses.go`

## Security and operations parity

- [x] Optional bearer token auth for API.
- [x] Probe exemptions for `/healthz` and `/readyz`.
- [x] `401` includes `WWW-Authenticate: Bearer realm="gobayes"`.
- [x] Readiness drains to `503` during shutdown.

Gobayes references:
- `gobayes/gobayes.go`

## Persistence parity

- [x] Versioned model state persisted to disk.
- [x] Save/load validation invariants match gobayes semantics.
- [x] Atomic write-and-replace file behavior.

Gobayes references:
- `gobayes/bayes/persistence.go`
- `gobayes/bayes/category/categories.go`

## Test and CI parity

- [x] Contract coverage for new compatibility endpoints and error behavior.
- [x] Concurrency and state invariant tests for core classifier.
- [x] Persistence round-trip and corruption-path tests.
- [x] Property/fuzz-style test coverage.
- [x] CI lanes for parity-quality checks beyond current baseline.

Gobayes references:
- `gobayes/.github/workflows/ci.yml`

