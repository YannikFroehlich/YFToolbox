# ADR 0007: MSIX and AppInstaller distribution

Status: Accepted

The supported distribution is a self-contained Windows 11 x64 MSIX. Semantic
versions map to four-part numeric package versions by appending `.0`.
Trusted signing is mandatory for public artifacts. Stable and preview
AppInstaller files live at permanent HTTPS channel URLs; GitHub Release assets
hold immutable versioned packages.
