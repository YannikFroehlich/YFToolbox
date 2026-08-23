# ADR 0001: WPF and presentation isolation

Status: Accepted

YF Toolbox targets Windows 11 x64 with .NET 10 WPF. WPF UI supplies Fluent
navigation, Mica, themes and icons, but is restricted to the App and View
layers. Core and Application expose no WPF types. This preserves headless
testing and keeps a future move to native WPF Fluent theming feasible.
