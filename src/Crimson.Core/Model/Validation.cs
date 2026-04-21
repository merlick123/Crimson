namespace Crimson.Core.Model;

public sealed record Diagnostic(
    string Code,
    string Message,
    string Severity,
    SourceSpan? Source = null);

public sealed class DiagnosticException : Exception
{
    public DiagnosticException(IReadOnlyList<Diagnostic> diagnostics)
        : base(string.Join(Environment.NewLine, diagnostics.Select(x => $"{x.Severity} {x.Code}: {x.Message}")))
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
