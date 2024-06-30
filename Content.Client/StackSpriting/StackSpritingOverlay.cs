using System.Numerics;
using Content.Shared.StackSpriting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Graphics.RSI;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.StackSpriting;

public sealed class StackSpritingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    private readonly TransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;
    
    private int _stackByOneLayer = 1;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public StackSpritingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(ContentCVar.CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _stackByOneLayer = stackByOneLayer;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle; 
        var bounds = args.WorldAABB.Enlarged(5f);
        var eye = _eyeManager.CurrentEye;

        var eyePos = new Vector3(eye.Position.Position.X, 20, eye.Position.Y);
        
        var query = _entityManager.EntityQueryEnumerator<StackSpriteComponent,TransformComponent>();
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            //if(!bounds.Contains(drawPos))
             //   continue;
            
            
            var rsi = _spriteSystem.RsiStateLike(stackSpriteComponent.Sprite);
            for (var i = 0; i < rsi.AnimationFrameCount; i++)
            {
                var coolIndex = rsi.AnimationFrameCount - 1 - i;
                var tex = rsi.GetFrame(RsiDirection.South,coolIndex);

                for (var z = 0; z < _stackByOneLayer; z++)
                {
                    var zLevelLayer = z / _stackByOneLayer;
                    var texPos = new Vector3(drawPos.X, i + zLevelLayer, drawPos.Y);
                    
                    DrawLayer(handle,tex,texPos,transformComponent.WorldRotation,0,0);
                }
                
            }
        }
    }

    private void DrawLayer(DrawingHandleWorld handle,Robust.Client.Graphics.Texture tex, Vector3 drawPos,Angle yaw, Angle pitch, Angle roll)
    {
        
        
        
        
        //handle.DrawTexture(tex,flattern,yaw);
    }
    
}

public sealed class StackSpriteAccumulator
{
    public readonly SimpleBuffer<Vector3> Vertexes = new(1024*4);
    public readonly SimpleBuffer<Robust.Client.Graphics.Texture> TexturePool = new(128);
    //public readonly SimpleBuffer<SimpleBuffer<int>> Relations = new(128);

}

public sealed class DrawingHandleStackSprite
{
    private DrawingHandleWorld _baseHandle;
    private StackSpriteAccumulator _accumulator;
    private IEye _currentEye;
    private Box2 _bounds;

    public DrawingHandleStackSprite(StackSpriteAccumulator accumulator, DrawingHandleWorld baseHandle, IEye currentEye, Box2 bounds)
    {
        _accumulator = accumulator;
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;

        var eyePos = _currentEye.Position.Position;

        ViewMatrix = Matrix4x4.CreateLookTo(new Vector3(eyePos.X,20,eyePos.Y), new Vector3(0,-1,0),Vector3.UnitY);

        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4, // Field of view
            _bounds.Width/_bounds.Height, // Aspect ratio
            0.1f, // Near plane
            100.0f           // Far plane
        );
    }

    public Matrix4x4 ProjectionMatrix { get; set; }

    public Matrix4x4 ViewMatrix { get; set; }

    public void DrawSpriteLayer(Robust.Client.Graphics.Texture texture, Vector3 drawPos, Angle yaw, Angle pitch, Angle roll)
    {
        var texId = _accumulator.TexturePool.Add(texture);
        var p1 = drawPos;
        var p3 = drawPos + new Vector3(_currentEye.Scale.X,0,_currentEye.Scale.Y);

        var p2 = new Vector3(p1.X, drawPos.Y, p3.Z);
        var p4 = new Vector3(p3.X, drawPos.Y, p1.Z);

        p1 = Transform(p1);
        p2 = Transform(p2);
        p3 = Transform(p3);
        p4 = Transform(p4);

        _accumulator.Vertexes.Add(p1);
        _accumulator.Vertexes.Add(p2);
        _accumulator.Vertexes.Add(p3);
        _accumulator.Vertexes.Add(p4);
    }

    private Vector3 Transform(Vector3 vector3)
    {
        return vector3;
    }

    private Vector2 Flatter(Vector3 vector3)
    {
        return new Vector2(vector3.X, vector3.Z);
    }

    public void Flush()
    {
        for (var i = 0; i < _accumulator.TexturePool.Length; i++)
        {
            var vertexId = i * 4;

            var p1 = Flatter(_accumulator.Vertexes[vertexId]);
            var p2 = Flatter(_accumulator.Vertexes[vertexId + 1]);
            var p3 = Flatter(_accumulator.Vertexes[vertexId + 2]);
            var p4 = Flatter(_accumulator.Vertexes[vertexId + 3]);
            
            _baseHandle.DrawTextureRect();

        }
        
        _accumulator.TexturePool.Clear();
        _accumulator.Vertexes.Clear();
    }
}

public sealed class SimpleBuffer<T>
{
    private readonly T[] _buffer;
    
    public readonly int Limit;
    public int Length;
    public int Shift;
    public T this[int pos]
    {
        get => Get(pos);
        set => Set(pos,value);
    }

    public SimpleBuffer(int limit)
    {
        _buffer = new T[limit];
        Limit = limit;
    }

    public int Add(T obj)
    {
        _buffer[Shift + Length] = obj;
        return (Length++) - 1;
    }

    public T Pop()
    {
        var obj = _buffer[Shift + Length];

        Shift++;
        Length--;

        return obj;
    }

    public T Get(int pos)
    {
        return _buffer[Shift+pos];
    }

    public void Set(int pos, T obj)
    {
        _buffer[Shift+pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }
}