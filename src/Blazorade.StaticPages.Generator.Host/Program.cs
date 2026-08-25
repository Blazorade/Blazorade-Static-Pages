using Blazorade.StaticPages.Generator;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: Blazorade.StaticPages.Generator.Host <application-assembly> <output-directory> <project-directory> [bootstrapper] [configuration]");
    return 2;
}

try
{
    var generator = new StaticPageGenerator();
    var count = await generator.GenerateAsync(new StaticPageGeneratorOptions(
        Path.GetFullPath(args[0].Replace("\"", string.Empty)),
        Path.GetFullPath(args[1].Replace("\"", string.Empty)),
        Path.GetFullPath(args[2].Replace("\"", string.Empty)),
        args.Length > 3 ? args[3].Replace("\"", string.Empty) : null,
        args.Length > 4 ? args[4].Replace("\"", string.Empty) : null));

    Console.WriteLine($"Generated {count} static page(s).");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}