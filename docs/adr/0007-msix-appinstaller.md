# ADR 0007: Portable distribution replaces mandatory MSIX

Status: Accepted

The supported distribution is a self-contained portable Windows 11 x64 ZIP.
MSIX scripts remain only for optional local experiments with a temporary
self-signed certificate. Public releases do not require Azure, code-signing
subscriptions or AppInstaller infrastructure. GitHub Release assets hold the
immutable versioned packages, manifest, SBOM and checksums.
