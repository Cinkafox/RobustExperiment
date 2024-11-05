namespace Content.Shared.Debug;

public sealed class DebugSystem : EntitySystem
{
    public Queue<DebugBash> DrawQueue = new Queue<DebugBash>();
    
    public void DrawCircle(Vector3 position, float radius, Color color)
    {
        DrawQueue.Enqueue(new DebugBash(position, radius, color));    
    }
}

public record struct DebugBash(Vector3 Position, float Radius, Color Color);