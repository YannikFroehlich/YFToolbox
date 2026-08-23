# ADR 0006: Git-derived semantic versioning

Status: Accepted

Tags are immutable and follow `vMAJOR.MINOR.PATCH`. Conventional
Commits choose patch, minor or major; commits with no recognized bump default
to patch and `[skip release]` is the sole opt-out. Versions are passed
to builds and packages and are never committed back to `main`.
Tag creation is the final publication transaction after all release checks.
