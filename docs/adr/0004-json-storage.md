# ADR 0004: JSON settings and pathless history

Status: Accepted

Schema-versioned settings and the optional action history use
`System.Text.Json` under LocalAppData. Settings are validated and
replaced atomically; corrupt input is backed up before defaults are restored.
History is disabled by default and stores no file names, paths, hashes or
contents. A database would add complexity without V1 value.
