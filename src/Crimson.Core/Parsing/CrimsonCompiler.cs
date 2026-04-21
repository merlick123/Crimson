using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Crimson.Core.Model;
using Crimson.Core.Parsing.Generated;

namespace Crimson.Core.Parsing;

public sealed class CrimsonCompiler
{
    public CompilationUnitModel ParseFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var input = CharStreams.fromPath(fullPath);
        var lexer = new CrimsonLexer(input);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new Generated.CrimsonParser(tokenStream);
        var errorListener = new ThrowingErrorListener(fullPath);
        lexer.RemoveErrorListeners();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var compilationUnit = parser.compilationUnit();
        var builder = new SemanticModelBuilder(fullPath, tokenStream);
        return builder.Build(compilationUnit);
    }

    public CompilationSetModel ParseFiles(IEnumerable<string> filePaths) =>
        new(filePaths
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .Select(ParseFile)
            .ToArray());

    private sealed class ThrowingErrorListener(string filePath) : BaseErrorListener
    {
        public override void SyntaxError(
            TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new DiagnosticException([
                new Diagnostic(
                    "CRIMSON001",
                    msg,
                    "error",
                    new SourceSpan(filePath, new SourcePoint(line, charPositionInLine + 1), new SourcePoint(line, charPositionInLine + 1)))
            ]);
        }
    }
}
