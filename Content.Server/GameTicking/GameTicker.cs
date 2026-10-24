using Content.Shared.GameTicking;
using Robust.Server.Console;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker : SharedGameTicker
{
    [Dependency] private IConGroupController _groupController = default!;
    public ContentGroupController GroupController = new ContentGroupController();
    
    public override void Initialize()
    {
        base.Initialize();
        PlayerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _groupController.Implementation = GroupController;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                Timer.Spawn(0, () => PlayerManager.JoinGame(session));
                Log.Info($"{session.Name} was connected");
                break;
            case SessionStatus.InGame:
                AttachSession(session);
                break;
        }
    }
}

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