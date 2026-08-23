# ADR 0005: Atomic and non-destructive output

Status: Accepted

Originals are not overwritten by default. Handlers write a unique temporary
file in the destination directory, validate it, then finalize with a same-volume
move. Explicit overwrite still uses temporary output. Cleanup runs for success,
failure and cancellation; startup only removes old YF Toolbox temporary data.
