namespace BlockLaunch;

/// <summary>Effects are connected in composition, so session coordination can run without WPF or file access.</summary>
internal sealed record RunningGameActions(
    Func<bool> IsClosed,
    Action<string> Log,
    Action<DiagnosticContext> SetDiagnosticContext,
    Func<DiagnosticContext, Task<DiagnosticSnapshot>> CaptureDiagnostic,
    Action<DiagnosticSnapshot> ShowDiagnostic,
    Func<string, Task<string>> ReadLog);
