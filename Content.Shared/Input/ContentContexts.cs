using Robust.Shared.Input;

namespace Content.Shared.Input;

public sealed class ContentContexts
{
    public static void SetupContexts(IInputContextContainer contexts)
    {
        var human = contexts.GetContext("human"); 
        human.AddFunction(EngineKeyFunctions.MoveUp);
        human.AddFunction(EngineKeyFunctions.MoveDown);
        human.AddFunction(EngineKeyFunctions.MoveLeft);
        human.AddFunction(EngineKeyFunctions.MoveRight);
        human.AddFunction(EngineKeyFunctions.Walk);
        human.AddFunction(ContentKeyFunctions.PlayerJumpAction);
        human.AddFunction(ContentKeyFunctions.PlayerDropItemAction);
        human.AddFunction(EngineKeyFunctions.Use);
        human.AddFunction(EngineKeyFunctions.UseSecondary);
        
        var car = contexts.New("car", "common");
        car.AddFunction(ContentKeyFunctions.CarMoveForwardAction);
        car.AddFunction(ContentKeyFunctions.CarMoveBackwardAction);
        car.AddFunction(ContentKeyFunctions.CarMoveLeftAction);
        car.AddFunction(ContentKeyFunctions.CarMoveRightAction);
        car.AddFunction(ContentKeyFunctions.CarBreak);
    }
} 