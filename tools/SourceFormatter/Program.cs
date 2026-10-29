using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
// Reuse the SDK's Roslyn tooling; this project is never referenced by the launcher.
var write = args.Contains("--write");
var repository = Directory.GetCurrentDirectory();
var launcherProject = Path.Combine(repository, "src", "MCUGLauncher", "MCUGLauncher.csproj");
if (!File.Exists(launcherProject))
{
    Console.Error.WriteLine("Run SourceFormatter from the repository root.");
    return 2;
}

using var workspace = new AdhocWorkspace();
var options = workspace.Options
    .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, "\n")
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, true)
    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, true)
    .WithChangedOption(CSharpFormattingOptions.WrappingPreserveSingleLine, false)
    .WithChangedOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false);
var sourceDirectories = new[] {
    "src",
    "tools"
};
var changed = 0;
foreach (var directory in sourceDirectories)
{
    var files = Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
        .OrderBy(path => path, StringComparer.Ordinal);
    foreach (var file in files)
    {
        var original = File.ReadAllText(file);
        var root = CSharpSyntaxTree.ParseText(original).GetRoot();
        var formatted = Formatter.Format(root, workspace, options).ToFullString().TrimEnd() + "\n";
        var formattedRoot = CSharpSyntaxTree.ParseText(formatted).GetRoot();
        var originalTokens = root.DescendantTokens().Select(token => (token.RawKind, token.Text));
        var formattedTokens = formattedRoot.DescendantTokens().Select(token => (token.RawKind, token.Text));
        // Preserve every token, including raw string line endings. Only trivia may change.
        if (!originalTokens.SequenceEqual(formattedTokens))
        {
            throw new InvalidOperationException($"Formatting changed tokens: {file}");
        }

        if (original == formatted)
        {
            continue;
        }

        changed++;
        Console.WriteLine(file);
        if (write)
        {
            File.WriteAllText(file, formatted, new UTF8Encoding(false));
        }
    }
}

Console.WriteLine($"{changed} C# files {(write ? "formatted" : "need formatting")}.");
return !write && changed > 0 ? 1 : 0;
