# AGENTS.md

## Purpose

This repository contains MAUI/.NET SDKs for Persian ad providers, starting with Tapsell for Android.

## Current priorities

1. Android first
2. Tapsell before Tapsell Plus
3. Tapsell Plus before Adivery
4. Rewarded, Native, and PreRoll before less critical formats

## Project structure

- `PersianAds/`: shared contracts, models, and common abstractions
- `Tapsell/`: Tapsell-specific package for Android
- `docs/`: consumer-facing integration guides

## Coding rules

- Keep the shared `PersianAds` project provider-agnostic.
- Put provider-specific logic inside each provider project.
- Prefer clear service abstractions over UI-coupled APIs.
- Keep Android-first decisions isolated so iOS can be added later without breaking the public API.
- Do not claim a feature is implemented unless the native bridge and runtime flow actually exist.

## MAUI integration rules

- Registration should happen through DI extension methods.
- App-facing APIs should be usable from either code-behind or a ViewModel.
- UI-hosted formats like banner and native ads should expose a MAUI-friendly host pattern instead of forcing app code to talk to Android views directly.

## Documentation rules

- Update `README.md` for high-level status changes.
- Put step-by-step consumer guides in `docs/`.
- When NuGet instructions are documented, clearly separate `planned package usage` from `current local-project usage` if the package is not yet published.

## Delivery expectations

- Prefer runnable, integrated work over placeholder validators.
- If something is still incomplete because of a native SDK dependency, state that explicitly in code comments and docs.
