using Blazorade.StaticPages.Generator;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: Blazorade.StaticPages.Generator.Host <application-assembly> <output-directory> <project-directory> [bootstrapper]");
    return 2;
}

try
{
    var generator = new StaticPageGenerator();
    var count = await generator.GenerateAsync(new StaticPageGeneratorOptions(
        Path.GetFullPath(args[0]),
        Path.GetFullPath(args[1]),
        Path.GetFullPath(args[2]),
        args.Length > 3 ? args[3] : null));

    Console.WriteLine($"Generated {count} static page(s).");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}