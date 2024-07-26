using Content.Shared.GameTicking;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.GameTicking;

public sealed class ServerGameTicker : SharedGameTicker
{
    [Dependency] private readonly IConGroupController _groupController = default!;

    public ContentGroupController GroupController = new ContentGroupController();
    
    public override void Initialize()
    {
        base.Initialize();
        
        PlayerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        _groupController.Implementation = GroupController;
        Timer.Spawn(0, InitializeMap);
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Timer.Spawn(0, () => PlayerManager.JoinGame(session));
                Logger.Info($"{session.Name} has connected");
                break;
            case SessionStatus.InGame:
                AddSession(session);
                break;
        }
    }
}

public sealed class SetHostCommand : LocalizedCommands
{
    public override string Command { get; } = "sethost";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IoCManager.Resolve<IEntityManager>().System<ServerGameTicker>().GroupController.AdminName = args[1];
        shell.WriteLine("Host is updated!");
    }
}

// Смешное
public sealed class ContentGroupController : IConGroupControllerImplementation
{
    public string AdminName = "localhost@JoeGenero";
    
    public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error)
    {
        error = null;
        return user?.Name == AdminName;
    }

    public bool CanCommand(ICommonSession session, string cmdName)
    {
        Logger.Debug(session.Name + " " + AdminName);
        return session.Name == AdminName;
    }

    public bool CanAdminPlace(ICommonSession session)
    {
        return session.Name == AdminName;
    }

    public bool CanScript(ICommonSession session)
    {
        return session.Name == AdminName;
    }

    public bool CanAdminMenu(ICommonSession session)
    {
        return session.Name == AdminName;
    }

    public bool CanAdminReloadPrototypes(ICommonSession session)
    {
        return session.Name == AdminName;
    }
}