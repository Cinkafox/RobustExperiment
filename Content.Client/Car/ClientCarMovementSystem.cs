using Content.Shared.Car;
using Robust.Client.GameObjects;

namespace Content.Client.Car;

public sealed partial class ClientCarMovementSystem : EntitySystem
{
    [Dependency] private InputSystem _inputSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerActiveCarRemoteControlComponent, ComponentRemove>(OnRemoteControllerComponentRemoved);
        SubscribeLocalEvent<PlayerActiveCarRemoteControlComponent, ComponentInit>(OnRemoteControllerComponentInit);
    }

    private void OnRemoteControllerComponentInit(EntityUid uid, PlayerActiveCarRemoteControlComponent component, 
        ComponentInit args)
    {
        if(!TryComp<InputComponent>(uid, out var input))
            return;
        
        input.ContextName = "car";
        _inputSystem.SetEntityContextActive();
    }
    
    private void OnRemoteControllerComponentRemoved(EntityUid uid, PlayerActiveCarRemoteControlComponent component,
        ComponentRemove args)
    {
        if (!TryComp<InputComponent>(uid, out var input))
            return;
        
        input.ContextName = "human";
        _inputSystem.SetEntityContextActive();
    }
}