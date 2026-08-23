# Release setup

The workflows are complete but intentionally fail closed until the external
trust configuration has been supplied.

## Repository policy

- Protect `main` and require Build, Unit Tests, Integration Tests,
  Conventional PR title and Dependency review.
- Disable direct pushes and use Squash Merge by default.
- Create the protected GitHub Environment `release`.
- Limit environment access to trusted maintainers and the main branch.
- Enable GitHub Pages from the `gh-pages` branch, or provide another
  stable HTTPS host for the channel files.

## Environment secrets

- `SIXLABORS_LICENSE_KEY`: key issued by Six Labors for the applicable
  open-source or commercial license.
- `AZURE_CLIENT_ID`: OIDC-enabled application/client identifier.
- `AZURE_TENANT_ID`: Azure tenant identifier.
- `AZURE_SUBSCRIPTION_ID`: Azure subscription identifier.

The Azure identity needs the Artifact Signing Certificate Profile Signer role.
Fork pull requests never receive these secrets and only execute Debug CI.

## Environment variables

- `MSIX_PACKAGE_IDENTITY`: permanently reserved package identity.
- `MSIX_PUBLISHER`: exact publisher distinguished name from the
  signing identity.
- `APPINSTALLER_BASE_URI`: permanent HTTPS origin, for example
  `https://yfroe.github.io/YFToolbox`.
- `ARTIFACT_SIGNING_ENDPOINT`: regional Artifact Signing endpoint.
- `ARTIFACT_SIGNING_ACCOUNT`: Artifact Signing account name.
- `ARTIFACT_SIGNING_PROFILE`: public-trust certificate profile name.

The MSIX identity and publisher must be fixed before the first public beta.

## Local development package

Use `build/Package-Msix.ps1` with publisher
`CN=YF Toolbox Development`, then run
`build/Sign-DevMsix.ps1`. The latter creates a non-exportable,
short-lived code-signing certificate, signs the package, exports only the
public certificate and removes the private key from the user store. Trust the
exported public certificate only on development machines. Public releases never
use this certificate.

## Release behavior

Every non-skipped push to `main` is serialized in the
`yftoolbox-version-release` concurrency group. The first automatic
release is `v0.1.0`. A manual dispatch can force patch, minor or major;
use a major dispatch after V1 acceptance to create `v1.0.0`.

Preview releases update `channels/YFToolbox.Preview.appinstaller`.
Stable releases update `channels/YFToolbox.appinstaller`. The
channel update is an idempotent commit to `gh-pages` and contains
`[skip release]`.
