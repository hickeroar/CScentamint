# CScentamint Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Added

- **CLI options** for the API server: `--host`, `--port`, `--auth-token`, `--language`, `--remove-stop-words`, `--verbose`, `--help` / `-h`. Pass after `--` when using `dotnet run`.
- **Environment variables** with `CSCENTAMINT_` prefix: `CSCENTAMINT_HOST`, `CSCENTAMINT_PORT`, `CSCENTAMINT_AUTH_TOKEN`, `CSCENTAMINT_LANGUAGE`, `CSCENTAMINT_REMOVE_STOP_WORDS`, `CSCENTAMINT_VERBOSE`. Booleans accept `1`, `true`, or `yes` (case-insensitive).
- **Verbose logging**: when `--verbose` or `CSCENTAMINT_VERBOSE` is enabled, request method/path and body preview and response status/body preview are logged to stderr.
- **Help option**: `--help` or `-h` prints CLI and env documentation and exits without starting the server.
- Server binding defaults to `http://0.0.0.0:8000`; configurable via `--host` / `--port` or env.

### Changed

- Coverage thresholds enforced at 100% for line, branch, and method in both test projects; see CONTRIBUTING.
- **Verbose integration test** (`Integration_WhenVerboseEnabled_AppResponds`) now uses `CSCENTAMINT_VERBOSE` env var; `ConfigureAppConfiguration` runs too late for Program startup, so config override does not reach `ServerOptionsBuilder` before the host is built.
- **ServerOptionsBuilder.IsTruthy**: removed unreachable `value.Length > 0` branch; `GetBool` only invokes `IsTruthy` with `Trim()` of non-whitespace values, so empty string is never passed.

## v2.2.1 - 2026-02-23

### Changed

- Terminology updated from "Bayes" to "Bayesian" in documentation and NuGet package metadata (description, package tags, Swagger description, and XML docs).

## v2.2.0 - 2026-02-23

### Added

- **Multi-language stemming** via `StemmerFactory` and `DefaultTextTokenizer`, aligned with stemmer-supported languages.
- **Stopwords module** (`Stopwords.Get`, `Supported`, `SupportedLanguages`) with built-in stopword lists for supported languages.
- **Tokenizer persistence**: model save/load now persists tokenizer config (language, `removeStopWords`).
- `InMemoryNaiveBayesClassifier` overloads for language, `removeStopWords`, and custom tokenizer.
- Swagger/OpenAPI metadata and XML comment documentation for the API.
- Null-safety for JSON body parameters in API controllers (`ArgumentNullException.ThrowIfNull`).
- `ITextTokenizer` DI wiring with `Tokenization:Language` and `Tokenization:RemoveStopWords` configuration.

### Changed

- `PersistedModelState` extended with tokenizer state for round-trip persistence.
- `DefaultTextTokenizer` supports configurable language and stopword removal.

## v2.1.0 - 2026-02-22

### Added

- Core NuGet package metadata for `CScentamint`, including package tags, license expression, repository links, and packaged readme support.
- SourceLink integration for `Cscentamint.Core` via `Microsoft.SourceLink.GitHub`.
- Dedicated Core package documentation at `src/Cscentamint.Core/README.md` covering:
  - training, untraining, classify, score, summaries, and reset
  - stream and file persistence
  - tokenizer behavior and customization
  - validation and model-loading constraints
- Release workflow `.github/workflows/release.yml` for CI-only NuGet trusted publishing with:
  - tag and manual dispatch triggers
  - build/test/pack pipeline
  - OIDC login (`NuGet/login@v1`) and package publish to nuget.org
  - release artifact upload for `.nupkg` and `.snupkg`

### Changed

- `Cscentamint.Api` is now explicitly non-packable (`IsPackable=false`) to prevent accidental API package publishing.
- Repository release docs were updated for Core-only package publishing (`CScentamint`) with trusted publishing and environment `nuget-release`.
- CI build defaults now enable `ContinuousIntegrationBuild` on GitHub Actions.
- Version references were updated to `v2.1.0` for release examples and package versioning.

### Maintenance

- Added `*.snupkg` to `.gitignore`.
- Updated central package version management with `Microsoft.SourceLink.GitHub`.

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
