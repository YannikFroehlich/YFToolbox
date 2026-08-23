# Contributing

Thank you for helping improve YF Toolbox.

## Workflow

1. Create a focused branch from `main`.
2. Add or update tests for behavior changes.
3. Run restore, build and tests locally.
4. Open a pull request with a Conventional Commit title.
5. Use Squash Merge so the PR title becomes the release commit.

Accepted title types include `feat`, `fix`, `docs`,
`chore`, `refactor`, `test`, `build` and
`ci`. Add `!` or a `BREAKING CHANGE:` footer for a
major change.

Examples:

- `fix(output): never replace the source file`
- `feat(images): add ico conversion`
- `feat!: revise the processing module API`

## Design rules

- Core and Application stay independent of WPF.
- Feature modules do not reference one another.
- ViewModels do not call static file, process, settings or message-box APIs.
- File mutations write temporary output and explicitly finalize it.
- Cancellation is propagated through long-running work.
- User-visible copy must be available in German and English.
- Logs must not contain full user paths at information level.

See `docs/adr` for the architectural decisions behind these rules.
