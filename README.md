# YF Toolbox

YF Toolbox is a local-first, privacy-friendly Windows 11 desktop application
built with C# 14, .NET 10 and WPF. Version 1 provides safe image conversion,
batch processing, batch rename, file inspection and streaming hashes in German
and English.

Original files are not overwritten by default. Mutating operations use a
temporary output followed by an explicit finalize step, and the application
does not contain telemetry.

## V1 capabilities

- PNG, JPEG, WebP, BMP and multi-size ICO conversion
- Resize, rotation, horizontal or vertical flip, quality and metadata controls
- Single-file and fault-tolerant batch jobs with cancellation
- Extension, signature and decoder-backed file inspection
- Two-phase batch rename with validation and best-effort rollback
- Streaming SHA-256 and MD5 calculation
- System, light and dark themes; German and English resources
- Self-contained portable Windows x64 releases as ordinary ZIP archives

## Development

- Windows 11 x64
- .NET SDK 10.0.300 or newer patch release
- Visual Studio 2026 or another WPF-capable .NET development environment

```powershell
dotnet restore --configfile NuGet.Config
dotnet build --configuration Debug
dotnet test --configuration Debug
dotnet run --project src/YFToolbox.App
```

Local Debug builds use the visible version `0.1.0-dev+local`. All required
runtime and build dependencies work without paid subscriptions, cloud accounts
or license keys.

## Releases

Pushes to `main` use Conventional Commits to calculate the next semantic
version. `fix:` creates a patch, `feat:` a minor release and a
breaking change a major release. All other commits default to a patch, matching
YFRemote. Add `[skip release]` to the head commit to skip publication.

The release workflow checks out the immutable source SHA, builds and tests,
publishes self-contained x64 output, starts it in a smoke test and packages it
as a portable ZIP, then generates an SBOM, manifest and checksums. Only after
those checks succeed may it create the tag and GitHub Release.

Repository and protected-environment setup is documented in
`docs/release-setup.md`.

## Privacy

All processing is local. YF Toolbox contains no telemetry and does not upload
file names, paths, hashes or contents.

## License

YF Toolbox is released under the MIT License.
