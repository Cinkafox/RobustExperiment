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
            Message("Fuck! " + ConnectFailReason);
        }

        private void OnConnectStateChanged(ClientConnectionState state)
        {
            Message(state.ToString());
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