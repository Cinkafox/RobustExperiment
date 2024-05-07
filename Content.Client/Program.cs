using Robust.Client;
using Robust.Shared.Utility;

namespace Content.Client;

public sealed class Program
{
    static void Main(string[] args)
    {
        ContentStart.StartLibrary(args, new GameControllerOptions
        {
            Sandboxing = false,

            ContentModulePrefix = "Content.",

            ContentBuildDirectory = "Content.Client",

            DefaultWindowTitle = "SuperRobustGame",

            UserDataDirectoryName = "Content",

            ConfigFileName = "config.toml",

            SplashLogo = new ResPath("/Textures/Logo/logo.png"),
            
            WindowIconSet = new ResPath("/Textures/Logo/icon.png"),
        });
    }
}