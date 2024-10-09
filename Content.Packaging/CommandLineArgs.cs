using System.Diagnostics.CodeAnalysis;

namespace Content.Packaging;

public sealed class CommandLineArgs
{
    /// <summary>
    /// Should we also build the relevant project.
    /// </summary>
    public bool SkipBuild { get; set; }

    /// <summary>
    /// Should we wipe the release folder or ignore it.
    /// </summary>
    public bool WipeRelease { get; set; }

    /// <summary>
    /// Platforms for server packaging.
    /// </summary>
    public List<string>? Platforms { get; set; }

    /// <summary>
    /// Use HybridACZ for server packaging.
    /// </summary>
    public bool HybridAcz { get; set; }

    /// <summary>
    /// Configuration used for when packaging the server. (Release, Debug, Tools)
    /// </summary>
    public string Configuration { get; set; }

    // CommandLineArgs, 3rd of her name.
    public static bool TryParse(IReadOnlyList<string> args, [NotNullWhen(true)] out CommandLineArgs? parsed)
    {
        parsed = null;
        var skipBuild = false;
        var wipeRelease = true;
        var hybridAcz = false;
        var configuration = "Release";
        List<string>? platforms = null;

        using var enumerator = args.GetEnumerator();
        var i = -1;

        while (enumerator.MoveNext())
        {
            i++;
            var arg = enumerator.Current;

            if (arg == "--skip-build")
            {
                skipBuild = true;
            }
            else if (arg == "--no-wipe-release")
            {
                wipeRelease = false;
            }
            else if (arg == "--hybrid-acz")
            {
                hybridAcz = true;
            }
            else if (arg == "--platform")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No platform provided");
                    return false;
                }

                platforms ??= new List<string>();
                platforms.Add(enumerator.Current);
            }
            else if (arg == "--configuration")
            {
                if (!enumerator.MoveNext())
                {
                    Console.WriteLine("No configuration provided");
                    return false;
                }

                configuration = enumerator.Current;
            }
            else if (arg == "--help")
            {
                PrintHelp();
                return false;
            }
            else
            {
                Console.WriteLine("Unknown argument: {0}", arg);
            }
        }

        parsed = new CommandLineArgs(skipBuild, wipeRelease, hybridAcz, platforms, configuration);
        return true;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Usage: Content.Packaging [client/server] [options]

Options:
  --skip-build          Should we skip building the project and use what's already there.
  --no-wipe-release     Don't wipe the release folder before creating files.
  --hybrid-acz          Use HybridACZ for server builds.
  --platform            Platform for server builds. Default will output several x64 targets.
  --configuration       Configuration to use for building the server (Release, Debug, Tools). Default is Release.
");
    }

    private CommandLineArgs(
        bool skipBuild,
        bool wipeRelease,
        bool hybridAcz,
        List<string>? platforms,
        string configuration)
    {
        SkipBuild = skipBuild;
        WipeRelease = wipeRelease;
        HybridAcz = hybridAcz;
        Platforms = platforms;
        Configuration = configuration;
    }
}
