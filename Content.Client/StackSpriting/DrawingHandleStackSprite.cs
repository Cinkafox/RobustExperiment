using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Graphics;
using Robust.Shared.Profiling;
using Vector3 = System.Numerics.Vector3;

namespace Content.Client.StackSpriting;

public sealed class DrawingHandleStackSprite : IDisposable
{
    private DrawingHandleBase _baseHandle;
    private StackSpriteAccumulator _accumulator;
    private IEye _currentEye;
    private Box2 _bounds;
    private readonly ProfManager _profManager;

    public DrawingHandleStackSprite(StackSpriteAccumulator accumulator, DrawingHandleBase baseHandle, IEye currentEye, Box2 bounds)
    {
        _accumulator = accumulator;
        _baseHandle = baseHandle;
        _currentEye = currentEye;
        _bounds = bounds;
        _profManager = IoCManager.Resolve<ProfManager>();
    }
    public bool IsFlushed { get; private set; }

    public int AddTexture(Robust.Client.Graphics.Texture texture)
    {
        _accumulator.TexturePool.Add(texture);
        return _accumulator.TexturePool.Length - 1;
    }

    public void DrawSpriteLayer(int textureId,Vector3 drawPos,Vector3? drawCenter, Angle yaw, Angle pitch, Angle roll)
    {
        var currScale = _accumulator.TexturePool[textureId].Size / (float)EyeManager.PixelsPerMeter;
        
        var p1 = drawPos; //LeftTop
        var p3 = drawPos + new Vector3(currScale.X,0,currScale.Y); //RightBottom

        var p2 = new Vector3(p1.X, drawPos.Y, p3.Z);//LeftBottom
        var p4 = new Vector3(p3.X, drawPos.Y, p1.Z);//RightTop

        drawCenter ??= new Vector3(currScale.X, 0, currScale.Y) / 2f;
        
        var center = p1 + drawCenter.Value;
        
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
        
        using var preparing = _profManager.Group("SpriteStackingPrepareLayer");

        var vertexId = _accumulator.Vertexes.Length;

        _accumulator.Vertexes.Add(p1);
        _accumulator.Vertexes.Add(p2);
        _accumulator.Vertexes.Add(p3);
        _accumulator.Vertexes.Add(p4);

        var height = drawPos.Y + (drawPos.X * 0.1f + drawPos.Z * 0.01f);
        while (_accumulator.DrawQueue.ContainsKey(height))
        {
            height += 0.001f;
        }
        
        _accumulator.DrawQueue.Add(height,new DrawQueue(textureId,vertexId));
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
        
        using var flush = _profManager.Group("SpriteStackingFlush");

        foreach (var (_,drawQueue) in _accumulator.DrawQueue)
        {
            var texture = _accumulator.TexturePool[drawQueue.TextureId]; 
            var vertexId = drawQueue.VertexId;

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

    public void Dispose()
    {
        Flush();
        _accumulator.TexturePool.Clear();
        _accumulator.Vertexes.Clear();
        _accumulator.DrawQueue.Clear();
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