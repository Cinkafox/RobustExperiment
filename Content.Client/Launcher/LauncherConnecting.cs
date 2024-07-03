using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Launcher;

public sealed class LauncherConnecting : Robust.Client.State.State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _clientNetManager = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public string? Address => _gameController.LaunchState.Ss14Address ?? _gameController.LaunchState.ConnectAddress;
        public string? ConnectFailReason { get; private set; }

        public string? LastDisconnectReason => _baseClient.LastDisconnectReason;
        
        public ClientConnectionState ConnectionState => _clientNetManager.ClientConnectState;
        

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
            
            Message("Connecting...");
        }

        protected override void Shutdown()
        {
            _userInterfaceManager.StateRoot.RemoveChild(MessageContainer);
            _clientNetManager.ConnectFailed -= OnConnectFailed;
            _clientNetManager.ClientConnectStateChanged -= OnConnectStateChanged;
        }

        private void Message(string message)
        {
            MessageContainer.AddChild(new Label()
            {
                Text = message
            });
        }

        private void OnConnectFailed(object? _, NetConnectFailArgs args)
        {
            if (args.RedialFlag)
            {
                // We've just *attempted* to connect and we've been told we need to redial, so do it.
                // Result deliberately discarded.
                Redial();
            }
            ConnectFailReason = args.Reason;
            Message("Some shit happen.. " );
            Message(ConnectFailReason);
        }

        private void OnConnectStateChanged(ClientConnectionState state)
        {
            if (state is ClientConnectionState.NotConnecting)
            {
                var btn1 = new Button()
                {
                    Text = "EXIT",
                    Margin = new Thickness(0,0,15,0),
                    HorizontalAlignment = Control.HAlignment.Left
                };
                
                btn1.OnPressed += ExitPressed;
                
                var btn2 = new Button()
                {
                    Text = "RECONNECT",
                    HorizontalAlignment = Control.HAlignment.Right
                };
                
                btn2.OnPressed += (args) => ReconnectPressed(args,btn1,btn2);

                var box = new BoxContainer()
                {
                    HorizontalAlignment = Control.HAlignment.Stretch
                };
                
                box.AddChild(btn1);
                box.AddChild(btn2);
                
                MessageContainer.AddChild(box);
            }
            else
            {
                Message(state.ToString());
            }
        }

        private void ReconnectPressed(BaseButton.ButtonEventArgs obj,Button exit, Button reconnect)
        {
            exit.Disabled = true;
            reconnect.Disabled = true;
            RetryConnect();
        }

        private void ExitPressed(BaseButton.ButtonEventArgs obj)
        {
            Exit();
        }

        public void RetryConnect()
        {
            if (_gameController.LaunchState.ConnectEndpoint != null)
            {
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
            }
        }

        public bool Redial()
        {
            try
            {
                if (_gameController.LaunchState.Ss14Address != null)
                {
                    _gameController.Redial(_gameController.LaunchState.Ss14Address);
                    return true;
                }
                else
                {
                    Logger.InfoS("launcher-ui", $"Redial not possible, no Ss14Address");
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorS("launcher-ui", $"Redial exception: {ex}");
            }
            return false;
        }

        public void Exit()
        {
            _gameController.Shutdown("Exit button pressed");
        }

        public void SetDisconnected()
        {
            
        }
    }