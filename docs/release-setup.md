# Release setup

YF Toolbox releases are deliberately independent of Azure, paid signing
services, certificate subscriptions and commercial license keys.

## Repository policy

- Protect `main` and require Build, Unit Tests, Integration Tests,
  Conventional PR title and Dependency review.
- Disable direct pushes and use Squash Merge by default.
- Keep the default Actions token read-only. The reusable release job requests
  `contents: write` only for its tag and GitHub Release transaction.

## Release behavior

Every non-skipped push to `main` is serialized in the
`yftoolbox-version-release` concurrency group. The first automatic release is
`v0.1.0`. A manual dispatch can force patch, minor or major; use a major
dispatch after V1 acceptance to create `v1.0.0`.

Each release contains a self-contained `YFToolbox-<version>-win-x64.zip`.
Users extract the folder and start `YFToolbox.App.exe`; a separately installed
.NET runtime, installer, administrator access and online account are not
required. The release also contains checksums, an SPDX SBOM, a release manifest
and third-party notices.

The tag is still created only after restore, Release build, all tests,
dependency checks, self-contained publish, application startup smoke test and
artifact generation succeed. Existing tags remain immutable and incomplete
releases can be recovered idempotently.

## Optional local MSIX

The scripts `build/Package-Msix.ps1` and `build/Sign-DevMsix.ps1` remain solely
for local development experiments. Their temporary self-signed certificate is
not part of public releases and requires no subscription. Public distribution
uses the portable ZIP so users and maintainers do not depend on a signing
provider.
