using Robust.Client;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.Connection;

public sealed partial class ConnectingState : State
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IClientNetManager _clientNetManager = default!;
    [Dependency] private IGameController _gameController = default!;
    [Dependency] private IBaseClient _baseClient = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private static readonly string LocalAddress = "ss14://localhost:1212/";
    public string? Address => _gameController.LaunchState.Ss14Address 
                              ?? _gameController.LaunchState.ConnectAddress ?? LocalAddress;
    
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

    private Button? _startSingleplayerButton;
    private Button? _connectToServerButton;
    
    public void InvokeStartEntry()
    {
        CleanupButtons();
        var btnGroup = new BoxContainer()
        {
            Margin = new Thickness(12),
            Orientation = BoxContainer.LayoutOrientation.Horizontal
        };

        _startSingleplayerButton = new Button()
        {
            Text = "Start Singleplayer",
        };
        
        _connectToServerButton = new Button()
        {
            Text = "Connect to the server",
        };

        _startSingleplayerButton.OnPressed += OnStartSingleplayerButtonPressed;
        _connectToServerButton.OnPressed += OnConnectToServerButtonPressed;

        btnGroup.AddChild(_startSingleplayerButton);
        btnGroup.AddChild(_connectToServerButton);  
        
        MessageContainer.AddChild(btnGroup);
    }

    private void OnConnectToServerButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        ConnectToServer();
        CleanupButtons();
    }

    private void OnStartSingleplayerButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        StartSingleplayer();
        CleanupButtons();
    }

    private void CleanupButtons()
    {
        _startSingleplayerButton?.OnPressed -= OnStartSingleplayerButtonPressed;
        _connectToServerButton?.OnPressed -= OnConnectToServerButtonPressed;
        _startSingleplayerButton?.Disabled = true;
        _connectToServerButton?.Disabled = true;
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
        InvokeStartEntry();
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

    public void StartSingleplayer()
    {
        Message("Start singleplayer..");
        _baseClient.StartSinglePlayer();
    }

    public void ConnectToServer()
    {
        try
        {
            if (_gameController.LaunchState.ConnectEndpoint != null)
            {
                Message("Connect to " + _gameController.LaunchState.ConnectEndpoint);
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                return;
            }

            Message("Connect to local server..");
            _baseClient.ConnectToServer("127.0.0.1",1212);
        }
        catch (Exception e)
        {
            OnConnectFailed(this, new NetConnectFailArgs(e.Message));
        }
    }
}