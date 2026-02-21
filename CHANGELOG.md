# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Added

- Gobayes-compatible endpoint surface:
  - `/info`, `/train/{category}`, `/untrain/{category}`, `/classify`, `/score`, `/flush`
- Probe endpoints:
  - `/healthz`, `/readyz`
- Optional bearer-token auth with probe exemptions.
- Compatibility request-size limit handling for 1 MiB payloads.
- Pluggable tokenizer abstraction and default tokenizer pipeline:
  - NFKC normalization, lowercasing, non-letter/non-digit split, stemming.
- Versioned core-model persistence APIs with validated load and atomic file replacement.
- Property-based classifier invariant tests and compatibility concurrency stress tests.
- Expanded CI lanes for static analysis and parity smoke checks.

### Changed

- Core classifier now matches gobayes-style tally-based priors and Bayesian denominator math.
- Classification now includes score metadata in both core and API responses.
- Category lifecycle behavior now removes empty token/category state after untraining.

### Quality

- Core and API test projects continue to enforce 100% line/branch/method coverage.
