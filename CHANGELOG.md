# CScentamint Changelog

All notable changes to this project are documented in this file.

## v2.0.0 - 2026-02-21

### Added

- .NET 10 rewrite and repository modernization.
- **API surfaces (both styles)**
  - Native JSON API under `/api/*` for: train, untrain, score, classify, and model reset.
  - Root text API under `/` for: `/info`, `/train/{category}`, `/untrain/{category}`, `/classify`, `/score`, `/flush`.
  - Probe endpoints: `/healthz` and `/readyz`.
- **Security and request handling**
  - Optional bearer-token auth for all non-probe endpoints.
  - Root endpoint body-size limit at 1 MiB.
  - Unknown-length payload handling (for requests without `Content-Length`).
  - Explicit payload-too-large fallback handling to keep `413` responses consistent.
- **Core library capabilities**
  - Pluggable tokenizer abstraction.
  - Default tokenizer pipeline: NFKC normalization, lowercasing, non-letter/non-digit splitting, and stemming.
  - Versioned model persistence with:
    - stream save/load support
    - file save/load support
    - atomic file replacement
    - model-load validation checks
- **Testing and parity**
  - Gobayes parity-focused checks and verification scenarios.
  - Property-based tests for classifier invariants.
  - Concurrency stress tests for endpoint behavior.

### Changed

- Core scoring and prior calculations were updated to better match gobayes-style behavior.
- Classification responses were standardized to include score metadata across core and API.
- Untraining behavior now fully removes empty category/token state.
- Route validation and defensive request-body behavior were tightened for edge cases.
- Auth-token comparison and readiness-state handling were hardened for consistency.

### Quality and CI

- Core and API test suites enforce 100% line, branch, and method coverage.
- CI was updated for the solution-less repo layout.
- CI now includes:
  - coverage-gated test lane
  - static-analysis/build lane
  - extended smoke lane (manual + scheduled)

### Documentation

- README and contributing guidance were refreshed for v2 workflows.
- API and library docs were expanded with persistence and operational guidance.

## v1.0.0 - 2015-06-17

### Added

- Initial project structure and ASP.NET service foundation.
- Early training support, including a train endpoint.
- Untraining support with probability post-processing.
- Flush/reset support for clearing learned state.
- Classification and scoring endpoint support.
- Initial documentation and baseline tests.
