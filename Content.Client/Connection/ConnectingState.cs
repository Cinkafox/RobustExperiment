using Robust.Client;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Connection;

public sealed partial class ConnectingState : State
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IClientNetManager _clientNetManager = default!;
    [Dependency] private IGameController _gameController = default!;
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    
    public const string LOCAL_ADDRESS = "ss14://localhost:1212/";
    public string? Address => _gameController.LaunchState.Ss14Address 
                              ?? _gameController.LaunchState.ConnectAddress ?? LOCAL_ADDRESS;
    
    public BoxContainer MessageContainer = new BoxContainer()
    {
        Margin = new Thickness(12),
        Orientation = BoxContainer.LayoutOrientation.Vertical
    };
    
    protected override void Startup()
    {
        _userInterfaceManager.StateRoot.AddChild(MessageContainer);
        
        _clientNetManager.ConnectFailed += OnConnectFailed;
        _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;
    }

    protected override void Shutdown()
    {
        _clientNetManager.ConnectFailed -= OnConnectFailed;
        _clientNetManager.ClientConnectStateChanged -= OnConnectStateChanged;
        
        _userInterfaceManager.StateRoot.RemoveChild(MessageContainer);
    }
    
    private void OnConnectStateChanged(ClientConnectionState state)
    {
        switch (state)
        {
            case ClientConnectionState.NotConnecting:
                break;
            case ClientConnectionState.ResolvingHost:
                break;
            case ClientConnectionState.EstablishingConnection:
                break;
            case ClientConnectionState.Handshake:
                break;
            case ClientConnectionState.Connected:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
        
        Message(Loc.GetString(state.ToString()));
    }

    private void OnConnectFailed(object? sender, NetConnectFailArgs args)
    {
        if (args.RedialFlag)
        {
            Redial();
        }
        Message($"Error while connecting to the server. Reason: {args.Reason}" );
    }
    
    public void Message(string message)
    {
        MessageContainer.AddChild(new Label()
        {
            Text = message
        });
    }
    
    public bool Redial()
    {
        Message("Trying to redial...");
        try
        {
            if (_gameController.LaunchState.Ss14Address != null)
            {
                _gameController.Redial(_gameController.LaunchState.Ss14Address);
                return true;
            }
            else
            {
                Message($"Redial not possible, no Ss14Address");
            }
        }
        catch (Exception ex)
        {
            Message($"Redial exception: {ex}");
        }
        return false;
    }

    public void ConnectToLocal()
    {
        try
        {
            Message("Connect to local server..");
            _baseClient.ConnectToServer("127.0.0.1",1212);
        }
        catch (Exception e)
        {
            OnConnectFailed(this, new NetConnectFailArgs(e.Message));
        }
    }
}