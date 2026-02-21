# Gobayes Parity Checklist

This checklist is the implementation source-of-truth for bringing `cscentamint` to parity with `gobayes` while staying idiomatic for ASP.NET Core and .NET.

## Core classifier parity

- [x] Unknown tokens are ignored during scoring.
- [x] Score results only include categories with score > 0.
- [x] Empty model classification returns no category.
- [x] Category names are validated with `^[-_A-Za-z0-9]+$` equivalent.
- [ ] Priors are based on category token tally totals (not vocabulary size).
- [ ] Bayesian denominator uses `(P(token|notCat) * P(notCat)) + numerator`.
- [ ] Untrain removes zero-count tokens and deletes empty categories.
- [ ] Deterministic category tie-breaking is lexical.
- [ ] Category summaries parity: `tokenTally`, `probInCat`, `probNotInCat`.

Gobayes references:
- `gobayes/bayes/bayes.go`
- `gobayes/bayes/category/category.go`
- `gobayes/bayes/category/categories.go`

## API parity

- [ ] Compatibility endpoints: `/train`, `/untrain`, `/classify`, `/score`, `/flush`, `/info`, `/healthz`, `/readyz`.
- [ ] Compatibility response payloads mirror gobayes DTOs.
- [ ] Method mismatch returns `405` and `Allow` header.
- [ ] Request size cap parity (`1 MiB`) for compatibility endpoints.
- [ ] Invalid category route handling parity.

Gobayes references:
- `gobayes/gobayes.go`
- `gobayes/responses.go`

## Security and operations parity

- [ ] Optional bearer token auth for API.
- [ ] Probe exemptions for `/healthz` and `/readyz`.
- [ ] `401` includes `WWW-Authenticate: Bearer realm="gobayes"`.
- [ ] Readiness drains to `503` during shutdown.

Gobayes references:
- `gobayes/gobayes.go`

## Persistence parity

- [ ] Versioned model state persisted to disk.
- [ ] Save/load validation invariants match gobayes semantics.
- [ ] Atomic write-and-replace file behavior.

Gobayes references:
- `gobayes/bayes/persistence.go`
- `gobayes/bayes/category/categories.go`

## Test and CI parity

- [ ] Contract coverage for new compatibility endpoints and error behavior.
- [ ] Concurrency and state invariant tests for core classifier.
- [ ] Persistence round-trip and corruption-path tests.
- [ ] Property/fuzz-style test coverage.
- [ ] CI lanes for parity-quality checks beyond current baseline.

Gobayes references:
- `gobayes/.github/workflows/ci.yml`

