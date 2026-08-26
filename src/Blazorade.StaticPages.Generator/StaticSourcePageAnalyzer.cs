using System.Globalization;
using System.Text;
using Blazorade.StaticPages.StaticGeneration;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blazorade.StaticPages.Generator;

/// <summary>
/// Analyzes Razor source files and produces deterministic static page fragments.
/// </summary>
internal sealed class StaticSourcePageAnalyzer
{
    private readonly string projectDirectory;
    private readonly Dictionary<string, SourceComponent> components;

    /// <summary>
    /// Initializes a source analyzer for a consuming project.
    /// </summary>
    /// <param name="projectDirectory">The consuming project directory.</param>
    public StaticSourcePageAnalyzer(string projectDirectory)
    {
        this.projectDirectory = projectDirectory;
        components = Directory.EnumerateFiles(projectDirectory, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Select(path => new SourceComponent(path, Path.GetFileNameWithoutExtension(path), File.ReadAllText(path)))
            .GroupBy(component => component.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Analyzes all routable Razor pages in the project.
    /// </summary>
    /// <returns>The analyzed static pages.</returns>
    public IReadOnlyList<AnalyzedStaticPage> Analyze()
    {
        var pages = new List<AnalyzedStaticPage>();
        foreach (var component in components.Values.OrderBy(component => component.Path, StringComparer.OrdinalIgnoreCase))
        {
            var routes = ReadDirectives(component.Source, "page");
            if (routes.Count == 0)
            {
                continue;
            }

            var document = Parse(component);
            var staticPages = document.Children.Where(node => node.IsElement("StaticPage")).ToArray();
            if (staticPages.Length == 0)
            {
                continue;
            }

            if (staticPages.Length > 1)
            {
                throw Error(component, routes[0], "A static routable component must contain exactly one <StaticPage> component.");
            }

            foreach (var route in routes)
            {
                if (route.Contains('{'))
                {
                    throw Error(component, route, "Parameterized routes are not supported.");
                }

                var values = ReadConstants(component);
                var staticPage = staticPages[0];
                var metadata = StaticPageMetadataValues.From(staticPage.Attributes, values, component, route);
                var context = new RenderContext(values, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                var pageContent = document.Children.Where(node => node.IsElement("StaticPage") || node.IsElement("StaticContent"));
                var content = RenderChildren(pageContent, context, component, route, pageRoot: true);
                pages.Add(new AnalyzedStaticPage(route, CreateFilePath(route, component.Name), component.Name, content, metadata));
            }
        }

        var duplicate = pages.GroupBy(page => page.Route, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"The route '{duplicate.Key}' is declared by more than one static page.");
        }

        return pages;
    }

    private string RenderChildren(IEnumerable<MarkupNode> children, RenderContext context, SourceComponent owner, string route, bool pageRoot)
    {
        var output = new StringBuilder();
        foreach (var node in children)
        {
            if (node.Kind == MarkupNodeKind.Text)
            {
                output.Append(RenderText(node.Text!, context, owner, route));
                continue;
            }

            if (node.IsElement("InteractiveContent"))
            {
                continue;
            }

            if (node.IsElement("StaticPage") || node.IsElement("StaticContent"))
            {
                output.Append(RenderChildren(node.Children, context, owner, route, pageRoot));
                continue;
            }

            if (IsComponent(node.Name!))
            {
                if (!components.TryGetValue(node.Name!.Split('.').Last(), out var referenced))
                {
                    if (node.Name.Equals("PageTitle", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    throw Error(owner, route, $"The reusable component '{node.Name}' could not be resolved.");
                }

                var source = Parse(referenced);
                var regions = FindStaticContentRegions(source.Children).ToArray();
                if (regions.Length == 0)
                {
                    continue;
                }

                var parameters = new Dictionary<string, string?>(context.Values, StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Name is not null)
                    {
                        parameters[attribute.Name] = Evaluate(attribute.Value, context.Values, owner, route);
                    }
                }

                var nestedContext = new RenderContext(parameters, new HashSet<string>(context.ComponentStack, StringComparer.OrdinalIgnoreCase));
                if (!nestedContext.ComponentStack.Add(referenced.Path))
                {
                    throw Error(owner, route, $"A reusable component cycle was detected at '{referenced.Name}'.");
                }

                foreach (var region in regions)
                {
                    output.Append(RenderChildren(region.Children, nestedContext, referenced, route, pageRoot: false));
                }

                continue;
            }

            output.Append(node.OpenTag);
            output.Append(RenderChildren(node.Children, context, owner, route, pageRoot));
            output.Append(node.CloseTag);
        }

        return output.ToString();
    }

    private static IEnumerable<MarkupNode> FindStaticContentRegions(IEnumerable<MarkupNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.IsElement("InteractiveContent"))
            {
                continue;
            }

            if (node.IsElement("StaticContent"))
            {
                yield return node;
            }
        }
    }

    private static string RenderText(string text, RenderContext context, SourceComponent owner, string route)
    {
        var output = new StringBuilder();
        var position = 0;
        while (position < text.Length)
        {
            var at = text.IndexOf('@', position);
            if (at < 0)
            {
                output.Append(text, position, text.Length - position);
                break;
            }

            output.Append(text, position, at - position);
            if (at + 1 < text.Length && text[at + 1] == '@')
            {
                output.Append('@');
                position = at + 2;
                continue;
            }

            var end = at + 1;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_' || text[end] == '.'))
            {
                end++;
            }

            if (end == at + 1)
            {
                throw Error(owner, route, "Only compile-time constant Razor expressions are supported in static content.");
            }

            var expression = text[(at + 1)..end];
            if (!context.Values.TryGetValue(expression.Split('.').Last(), out var value))
            {
                throw Error(owner, route, $"The Razor expression '@{expression}' is not a known compile-time constant.");
            }

            output.Append(System.Net.WebUtility.HtmlEncode(value ?? string.Empty));
            position = end;
        }

        return RemoveDirectives(output.ToString());
    }

    private static string RemoveDirectives(string text)
    {
        var output = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("@page ", StringComparison.Ordinal) || trimmed.StartsWith("@using ", StringComparison.Ordinal) || trimmed.StartsWith("@inherits ", StringComparison.Ordinal) || trimmed.StartsWith("@namespace ", StringComparison.Ordinal))
            {
                continue;
            }

            output.Append(line);
            output.Append('\n');
        }

        return output.ToString();
    }

    private static Dictionary<string, string?> ReadConstants(SourceComponent component)
    {
        var expressions = new Dictionary<string, ExpressionSyntax>(StringComparer.OrdinalIgnoreCase);
        var codeBehind = Path.ChangeExtension(component.Path, ".razor.cs");
        var sources = File.Exists(codeBehind)
            ? new[] { component.Source, File.ReadAllText(codeBehind) }
            : new[] { component.Source };

        foreach (var source in sources)
        {
            foreach (var line in source.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.Contains('='))
                {
                    continue;
                }

                var equals = trimmed.IndexOf('=');
                if (equals < 0)
                {
                    continue;
                }

                var left = trimmed[..equals].Trim();
                var right = trimmed[(equals + 1)..].Trim();
                // Trim trailing semicolon if present
                if (right.EndsWith(';'))
                {
                    right = right[..^1].Trim();
                }

                var tokens = left.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                {
                    continue;
                }

                var name = tokens.Last();
                if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
                {
                    continue;
                }

                var expression = SyntaxFactory.ParseExpression(right);
                if (!expression.ContainsDiagnostics)
                {
                    expressions[name] = expression;
                }
            }
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in expressions.Keys)
        {
            var resolving = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (TryEvaluateString(expressions, name, values, resolving, out var value))
            {
                values[name] = value;
            }
        }

        return values;
    }

    private static bool TryEvaluateString(
        IReadOnlyDictionary<string, ExpressionSyntax> expressions,
        string name,
        IDictionary<string, string?> values,
        ISet<string> resolving,
        out string? value)
    {
        if (values.TryGetValue(name, out value))
        {
            return true;
        }

        if (!expressions.TryGetValue(name, out var expression) || !resolving.Add(name))
        {
            value = null;
            return false;
        }

        var evaluated = TryEvaluateString(expression, expressions, values, resolving, out value);
        resolving.Remove(name);
        if (evaluated)
        {
            values[name] = value;
        }

        return evaluated;
    }

    private static bool TryEvaluateString(
        ExpressionSyntax expression,
        IReadOnlyDictionary<string, ExpressionSyntax> expressions,
        IDictionary<string, string?> values,
        ISet<string> resolving,
        out string? value)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                value = literal.Token.ValueText;
                return true;

            case IdentifierNameSyntax identifier:
                return TryEvaluateString(expressions, identifier.Identifier.ValueText, values, resolving, out value);

            case ParenthesizedExpressionSyntax parenthesized:
                return TryEvaluateString(parenthesized.Expression, expressions, values, resolving, out value);

            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression):
                if (TryEvaluateString(binary.Left, expressions, values, resolving, out var left)
                    && TryEvaluateString(binary.Right, expressions, values, resolving, out var right))
                {
                    value = left + right;
                    return true;
                }

                break;
        }

        value = null;
        return false;
    }

    private static string? Evaluate(string? value, IReadOnlyDictionary<string, string?> constants, SourceComponent owner, string route)
    {
        if (value is null)
        {
            return null;
        }

        if (value.StartsWith("@", StringComparison.Ordinal))
        {
            var name = value[1..].Split('.').Last();
            if (!constants.TryGetValue(name, out var resolved))
            {
                throw Error(owner, route, $"The component parameter expression '{value}' is not a known compile-time constant.");
            }

            return resolved;
        }

        return value;
    }

    private static RazorDocument Parse(SourceComponent component)
    {
        var source = RazorSourceDocument.Create(component.Source, component.Path);
        var syntaxTree = RazorSyntaxTree.Parse(source);
        if (syntaxTree.Diagnostics.Any(diagnostic => diagnostic.Severity == RazorDiagnosticSeverity.Error))
        {
            throw new InvalidOperationException($"Razor syntax errors were found in '{component.Path}'.");
        }

        return MarkupParser.Parse(component.Source);
    }

    private static List<string> ReadDirectives(string source, string directive)
    {
        var routes = new List<string>();
        foreach (var line in source.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("@" + directive + " ", StringComparison.Ordinal))
            {
                var value = trimmed[(directive.Length + 2)..].Trim();
                routes.Add(value.Length >= 2 && value[0] == '"' && value[^1] == '"'
                    ? value[1..^1]
                    : value);
            }
        }

        return routes;
    }

    private static bool IsComponent(string name) => char.IsUpper(name[0]) || name.Contains('.');

    private static InvalidOperationException Error(SourceComponent component, string route, string message) =>
        new($"{component.Path} ({route}): {message}");

    private static string CreateFilePath(string route, string pageName) => route == "/" ? $"{pageName}.html" : route.Trim('/').TrimEnd('/') + ".html";

    internal sealed record SourceComponent(string Path, string Name, string Source);

    private sealed record RenderContext(Dictionary<string, string?> Values, HashSet<string> ComponentStack);

    /// <summary>
    /// Represents a statically analyzed page.
    /// </summary>
    /// <param name="Route">The page route.</param>
    /// <param name="FilePath">The generated relative file path.</param>
    /// <param name="PageName">The page component name.</param>
    /// <param name="Content">The generated static fragment.</param>
    /// <param name="Metadata">The page metadata.</param>
    internal sealed record AnalyzedStaticPage(string Route, string FilePath, string PageName, string Content, StaticPageMetadataValues Metadata);

    /// <summary>
    /// Represents statically evaluable page metadata.
    /// </summary>
    internal sealed record StaticPageMetadataValues(string Title, string? Description, string? Image, string? Locale, DateTimeOffset? Date, bool IncludeInSitemap)
    {
        internal static StaticPageMetadataValues From(IReadOnlyList<MarkupAttribute> attributes, IReadOnlyDictionary<string, string?> constants, SourceComponent owner, string route)
        {
            string? Get(string name) => attributes.FirstOrDefault(attribute => attribute.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } attribute
                ? Evaluate(attribute.Value, constants, owner, route)
                : null;

            var title = Get("Title") ?? throw Error(owner, route, "StaticPage requires a compile-time constant Title.");
            var dateText = Get("Date");
            DateTimeOffset? date = null;
            if (!string.IsNullOrWhiteSpace(dateText) && !StaticPageDateParser.TryParse(dateText, out date))
            {
                Console.Error.WriteLine($"warning BLZ001: {owner.Path} ({route}): The StaticPage Date value '{dateText}' could not be parsed as a DateTimeOffset.");
            }

            var include = Get("IncludeInSitemap");
            return new(title, Get("Description"), Get("Image"), Get("Locale"), date, !string.Equals(include, "false", StringComparison.OrdinalIgnoreCase));
        }
    }

    private enum MarkupNodeKind { Text, Element }

    private sealed class MarkupNode
    {
        public MarkupNodeKind Kind { get; init; }
        public string? Text { get; init; }
        public string? Name { get; init; }
        public string OpenTag { get; init; } = string.Empty;
        public string CloseTag { get; set; } = string.Empty;
        public List<MarkupNode> Children { get; } = [];
        public List<MarkupAttribute> Attributes { get; } = [];
        public bool SelfClosing { get; init; }
        public bool IsElement(string name) => Kind == MarkupNodeKind.Element && string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
    }

    internal sealed record MarkupAttribute(string Name, string? Value);

    private sealed record RazorDocument(List<MarkupNode> Children);

    private static class MarkupParser
    {
        public static RazorDocument Parse(string source)
        {
            var root = new MarkupNode { Kind = MarkupNodeKind.Element, Name = "__root" };
            var stack = new Stack<MarkupNode>();
            stack.Push(root);
            var position = 0;
            while (position < source.Length)
            {
                var open = source.IndexOf('<', position);
                if (open < 0)
                {
                    AddText(stack.Peek(), source[position..]);
                    break;
                }

                if (open > position)
                {
                    AddText(stack.Peek(), source[position..open]);
                }

                if (source.AsSpan(open).StartsWith("<!--"))
                {
                    var endComment = source.IndexOf("-->", open + 4, StringComparison.Ordinal);
                    var length = endComment < 0 ? source.Length - open : endComment + 3 - open;
                    AddText(stack.Peek(), source.Substring(open, length));
                    position = open + length;
                    continue;
                }

                var close = source[open..].IndexOf('>');
                if (close < 0)
                {
                    AddText(stack.Peek(), source[open..]);
                    break;
                }

                close += open;
                var tag = source.Substring(open, close - open + 1);
                if (tag.StartsWith("</", StringComparison.Ordinal))
                {
                    var name = ReadName(tag, 2);
                    if (stack.Count > 1 && stack.Peek().Name!.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Pop().CloseTag = tag;
                    }
                    position = close + 1;
                    continue;
                }

                if (tag.StartsWith("<!", StringComparison.Ordinal) || tag.StartsWith("<?", StringComparison.Ordinal))
                {
                    AddText(stack.Peek(), tag);
                    position = close + 1;
                    continue;
                }

                var nameStart = tag.StartsWith("<@", StringComparison.Ordinal) ? 2 : 1;
                var elementName = ReadName(tag, nameStart);
                if (string.IsNullOrEmpty(elementName))
                {
                    AddText(stack.Peek(), tag);
                    position = close + 1;
                    continue;
                }

                var selfClosing = tag.TrimEnd().EndsWith("/>", StringComparison.Ordinal);
                var node = new MarkupNode
                {
                    Kind = MarkupNodeKind.Element,
                    Name = elementName,
                    OpenTag = tag,
                    SelfClosing = selfClosing
                };
                node.Attributes.AddRange(ReadAttributes(tag, nameStart + elementName.Length));
                stack.Peek().Children.Add(node);
                if (!selfClosing && !IsVoidElement(elementName))
                {
                    stack.Push(node);
                }

                position = close + 1;
            }

            return new RazorDocument(root.Children);
        }

        private static void AddText(MarkupNode parent, string text)
        {
            if (text.Length > 0)
            {
                parent.Children.Add(new MarkupNode { Kind = MarkupNodeKind.Text, Text = text });
            }
        }

        private static string ReadName(string tag, int start)
        {
            var end = start;
            while (end < tag.Length && (char.IsLetterOrDigit(tag[end]) || tag[end] is ':' or '.' or '-' or '_'))
            {
                end++;
            }

            return tag[start..end];
        }

        private static IEnumerable<MarkupAttribute> ReadAttributes(string tag, int start)
        {
            var position = start;
            while (position < tag.Length - 1)
            {
                while (position < tag.Length - 1 && char.IsWhiteSpace(tag[position])) position++;
                if (position >= tag.Length - 1 || tag[position] == '/') yield break;
                var nameStart = position;
                while (position < tag.Length - 1 && !char.IsWhiteSpace(tag[position]) && tag[position] is not ('=' or '>')) position++;
                var name = tag[nameStart..position];
                while (position < tag.Length - 1 && char.IsWhiteSpace(tag[position])) position++;
                string? value = null;
                if (position < tag.Length - 1 && tag[position] == '=')
                {
                    position++;
                    while (position < tag.Length - 1 && char.IsWhiteSpace(tag[position])) position++;
                    if (position < tag.Length - 1 && tag[position] is '"' or '\'')
                    {
                        var quote = tag[position++];
                        var valueStart = position;
                        while (position < tag.Length - 1 && tag[position] != quote) position++;
                        value = tag[valueStart..position];
                        if (position < tag.Length - 1) position++;
                    }
                    else
                    {
                        var valueStart = position;
                        while (position < tag.Length - 1 && !char.IsWhiteSpace(tag[position]) && tag[position] != '>') position++;
                        value = tag[valueStart..position];
                    }
                }

                if (name.Length > 0) yield return new MarkupAttribute(name, value);
            }
        }

        private static bool IsVoidElement(string name) => name.Equals("area", StringComparison.OrdinalIgnoreCase) || name.Equals("base", StringComparison.OrdinalIgnoreCase) || name.Equals("br", StringComparison.OrdinalIgnoreCase) || name.Equals("col", StringComparison.OrdinalIgnoreCase) || name.Equals("embed", StringComparison.OrdinalIgnoreCase) || name.Equals("hr", StringComparison.OrdinalIgnoreCase) || name.Equals("img", StringComparison.OrdinalIgnoreCase) || name.Equals("input", StringComparison.OrdinalIgnoreCase) || name.Equals("link", StringComparison.OrdinalIgnoreCase) || name.Equals("meta", StringComparison.OrdinalIgnoreCase) || name.Equals("param", StringComparison.OrdinalIgnoreCase) || name.Equals("source", StringComparison.OrdinalIgnoreCase) || name.Equals("track", StringComparison.OrdinalIgnoreCase) || name.Equals("wbr", StringComparison.OrdinalIgnoreCase);
    }
}
