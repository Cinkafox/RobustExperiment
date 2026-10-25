using Content.Client.ConfigurationUI;
using Content.Client.Connection;
using Content.Shared.Input;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.ContentPack;
using Content.StyleSheetify.Client.StyleSheet;
using Robust.Client.Input;
using Robust.Client.State;

namespace Content.Client.Entry;

public sealed partial class EntryPoint : GameClient
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IContentStyleSheetManager _styleSheetManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IGameController _gameController = default!;
    [Dependency] private ConfigurationUIManager _configurationUiManager = default!;
    
    public bool IsSingleplayer = true;
    
    public override void PreInit()
    {
        ClientIoC.Register(Dependencies);
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }

    public override void Init()
    {
        _configurationUiManager.Initialize();
    }
    
    public override void PostInit()
    {
        _userInterfaceManager.SetDefaultTheme("DefaultTheme");
        _styleSheetManager.ApplyStyleSheet("default");
        ContentContexts.SetupContexts(_inputManager.Contexts);
        
        _baseClient.RunLevelChanged += (_, args) =>
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                SwitchState(args.OldLevel is ClientRunLevel.Connected or ClientRunLevel.InGame);
            }
        };
       
        SwitchState();
    }
    
    private void SwitchState(bool disconnected = false)
    {
        _stateManager.RequestStateChange<ConnectingState>();
        var state = (ConnectingState)_stateManager.CurrentState;
        
        if(disconnected)
        {
            state.Message("Disconnected..");
            return;
        }

        if (_gameController.LaunchState.FromLauncher) 
            return;

        if (IsSingleplayer)
        {
            state.Message("Start singleplayer..");
            _baseClient.StartSinglePlayer();
        }
        else
        {
            state.ConnectToLocal();
        }
    }
}