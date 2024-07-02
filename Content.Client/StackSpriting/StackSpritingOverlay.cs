using System.Collections;
using System.Numerics;
using Content.Shared;
using Content.Shared.StackSpriting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
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
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    
    private readonly TransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;
    
    private int _stackByOneLayer = 1;
    private StackSpriteAccumulator _accumulator = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public StackSpritingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _configurationManager.OnValueChanged(CCVars.StackByOneLayer,OnStackLayerChanged,true);
        _transformSystem = _entityManager.System<TransformSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    private void OnStackLayerChanged(int stackByOneLayer)
    {
        _stackByOneLayer = stackByOneLayer;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = _eyeManager.CurrentEye;
        using var stackHandle = new DrawingHandleStackSprite(_accumulator, args.DrawingHandle, eye, args.WorldAABB.Enlarged(5f));
        var query = _entityManager.EntityQueryEnumerator<RendererStackSpriteComponent, TransformComponent>();
        
        while (query.MoveNext(out var uid, out var stackSpriteComponent, out var transformComponent))
        {
            var drawPos = _transformSystem.GetWorldPosition(uid) - new Vector2(0.5f);;
            var texture = stackSpriteComponent.Texture;
            var count = stackSpriteComponent.Height;
                
            for (var i = 0; i < count; i++)
            {
                var xIndex = i % texture.Width;
                var yIndex = i / texture.Height;
                    
                var tex = new AtlasTexture(texture,
                    UIBox2.FromDimensions(new Vector2(0,i*stackSpriteComponent.Size.X), stackSpriteComponent.Size));

                var textureId = stackHandle.AddTexture(tex);
                
                for (var z = 0; z < _stackByOneLayer; z++)
                {
                    var zLevelLayer = z / (float)_stackByOneLayer;
                    var texPos = new Vector3(drawPos.X, i + zLevelLayer, drawPos.Y);
                    
                    stackHandle.DrawSpriteLayer(textureId, texPos, transformComponent.WorldRotation, 0, 0);
                }
            }
        }
    }
}


public sealed class StackSpriteAccumulator
{
    public readonly SimpleBuffer<Vector3> Vertexes = new(1024*256);
    public readonly SimpleBuffer<Robust.Client.Graphics.Texture> TexturePool = new(1024*256);
    
    public readonly DrawVertexUV2D[] UvVertexes = new DrawVertexUV2D[6];
    public readonly Vector2[] DebugVertexes = new Vector2[4];

    public SortedDictionary<int, List<(int,int)>> DrawQueue = new();
    public int MaxHeight = 0;
}

public sealed class DrawingHandleStackSprite : IDisposable
{
    private DrawingHandleBase _baseHandle;
    private StackSpriteAccumulator _accumulator;
    private IEye _currentEye;
    private Box2 _bounds;

    public DrawingHandleStackSprite(StackSpriteAccumulator accumulator, DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds)
    {
        _accumulator = accumulator;
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;
    }
    public bool IsFlushed { get; private set; }

    public int AddTexture(Robust.Client.Graphics.Texture texture)
    {
        _accumulator.TexturePool.Add(texture);
        return _accumulator.TexturePool.Length - 1;
    }

    public void DrawSpriteLayer(int textureId,Vector3 drawPos, Angle yaw, Angle pitch, Angle roll)
    {
        var currScale = _accumulator.TexturePool[textureId].Size / (float)EyeManager.PixelsPerMeter;
        
        var p1 = drawPos; //LeftTop
        var p3 = drawPos + new Vector3(currScale.X,0,currScale.Y); //RightBottom

        var p2 = new Vector3(p1.X, drawPos.Y, p3.Z);//LeftBottom
        var p4 = new Vector3(p3.X, drawPos.Y, p1.Z);//RightTop

        var center = p3 - p1;
        var rotTrans = Matrix4x4.CreateFromYawPitchRoll((float)yaw, (float)pitch, (float)roll);

        p1 -= center;
        p2 -= center;
        p3 -= center;
        p4 -= center;

        p1 = Vector3.Transform(p1, rotTrans);
        p2 = Vector3.Transform(p2, rotTrans);
        p3 = Vector3.Transform(p3, rotTrans);
        p4 = Vector3.Transform(p4, rotTrans);
        
        p1 += center;
        p2 += center;
        p3 += center;
        p4 += center;

        p1 = Transform(p1);
        p2 = Transform(p2);
        p3 = Transform(p3);
        p4 = Transform(p4);
        
        if(!_bounds.Contains(new Vector2(p1.X,p1.Z)) && 
           !_bounds.Contains(new Vector2(p2.X,p2.Z)) && 
           !_bounds.Contains(new Vector2(p3.X,p3.Z)) && 
           !_bounds.Contains(new Vector2(p4.X,p4.Z))) 
            return;

        var vertexId = _accumulator.Vertexes.Length;

        _accumulator.Vertexes.Add(p1);
        _accumulator.Vertexes.Add(p2);
        _accumulator.Vertexes.Add(p3);
        _accumulator.Vertexes.Add(p4);

        var height = (int)(drawPos.Y * 10);
        
        if(!_accumulator.DrawQueue.TryGetValue(height,out var list))
        {
            list = new List<(int,int)>();
            _accumulator.DrawQueue.Add(height,list);
        }
        
        list.Add((textureId,vertexId));
        _accumulator.MaxHeight = int.Max(_accumulator.MaxHeight, height);
    }

    private Vector3 Transform(Vector3 vector3)
    {
        var rot = (new Vector2(vector3.X,vector3.Z) - _currentEye.Position.Position) * 0.005f + _currentEye.Rotation.ToVec() * _currentEye.Zoom * 0.01f;
        rot *= vector3.Y*0.5f;
        return new Vector3(vector3.X + rot.X , vector3.Y, vector3.Z + rot.Y);
    }

    private Vector2 Flatter(Vector3 vector3)
    {
        return new Vector2(vector3.X, vector3.Z);
    }

    public void Flush()
    {
        if (IsFlushed) throw new Exception();

        foreach (var (_, keyList) in _accumulator.DrawQueue)
        {
            foreach (var (textureIndex,vertexIndex) in keyList)
            {
                var texture = _accumulator.TexturePool[textureIndex]; 
                var vertexId = vertexIndex;

                var p1 = Flatter(_accumulator.Vertexes[vertexId]);
                var p2 = Flatter(_accumulator.Vertexes[vertexId + 1]);
                var p3 = Flatter(_accumulator.Vertexes[vertexId + 2]);
                var p4 = Flatter(_accumulator.Vertexes[vertexId + 3]);
            
                if(!_bounds.Contains(p1) && 
                   !_bounds.Contains(p2) && 
                   !_bounds.Contains(p3) && 
                   !_bounds.Contains(p4))
                    continue;

                texture = ExtractTexture(texture, null, out var sr);

                var hw = new Vector2(texture.Width,texture.Height);
                var t1 = sr.TopLeft / hw;
                var t2 = sr.BottomLeft / hw;
                var t3 = sr.BottomRight / hw;
                var t4 = sr.TopRight / hw;

                _accumulator.UvVertexes[0] = new DrawVertexUV2D(p1, t1);
                _accumulator.UvVertexes[1] = new DrawVertexUV2D(p2, t2);
                _accumulator.UvVertexes[2] = new DrawVertexUV2D(p3, t3);
            
                _accumulator.UvVertexes[3] = new DrawVertexUV2D(p1, t1);
                _accumulator.UvVertexes[4] = new DrawVertexUV2D(p3, t3);
                _accumulator.UvVertexes[5] = new DrawVertexUV2D(p4, t4);

                _accumulator.DebugVertexes[0] = p1;
                _accumulator.DebugVertexes[1] = p2;
                _accumulator.DebugVertexes[2] = p3;
                _accumulator.DebugVertexes[3] = p4;
            
                _baseHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture,_accumulator.UvVertexes); 
                //_baseHandle.DrawPrimitives(DrawPrimitiveTopology.LineLoop,_accumulator.DebugVertexes,Color.Wheat);
            }
        }
    }

    public void Dispose()
    {
        Flush();
        _accumulator.TexturePool.Clear();
        _accumulator.Vertexes.Clear();
        _accumulator.DrawQueue.Clear();
        _accumulator.MaxHeight = 0;
        IsFlushed = true;
    }
    
    public static Robust.Client.Graphics.Texture ExtractTexture(Robust.Client.Graphics.Texture texture, in UIBox2? subRegion, out UIBox2 sr)
    {
        if (texture is AtlasTexture atlas)
        {
            texture = atlas.SourceTexture;
            if (subRegion.HasValue)
            {
                var offset = atlas.SubRegion.TopLeft;
                sr = new UIBox2(
                    subRegion.Value.TopLeft + offset,
                    subRegion.Value.BottomRight + offset);
            }
            else
            {
                sr = atlas.SubRegion;
            }
        }
        else
        {
            sr = subRegion ?? new UIBox2(0, 0, texture.Width, texture.Height);
        }

        var clydeTexture = texture;
        return clydeTexture;
    }
}

public sealed class SimpleBuffer<T> : IEnumerable<T>
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

    public void Add(T obj)
    {
        if (Shift + Length >= Limit)
            throw new Exception($"{Shift + Length} reached the limit {Limit}");
        
        _buffer[Shift + Length] = obj;
        Length++;
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
        if (Shift + pos >= Limit)
            throw new Exception($"{Shift + pos} reached the limit {Limit}");
        return _buffer[Shift+pos];
    }

    public void Set(int pos, T obj)
    {
        if (Shift + pos >= Limit)
            throw new Exception($"{Shift + pos} reached the limit {Limit}");
        _buffer[Shift+pos] = obj;
    }

    public void Clear()
    {
        Length = 0;
        Shift = 0;
    }

    public T[] ToArray()
    {
        return _buffer;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var data in _buffer)
        {
            yield return data;
        } 
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _buffer.GetEnumerator();
    }
}