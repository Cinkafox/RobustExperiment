using System.Numerics;
using Content.Client.Utils;
using Content.Shared.Camera;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client.Graphics;
using Robust.Shared.Profiling;
using Robust.Shared.Threading;

namespace Content.Client.Viewport;

public sealed class DrawingHandle3d : IDisposable
{
    private bool Disposed { get; set; }
    
    public readonly IParallelManager Parallel;

    private readonly DrawingHandleBase _handleBase;
    public readonly DrawingInstance DrawingInstance;

    private ClippingInstance ClippingInstance => DrawingInstance.ClippingInstance;

    private readonly float _width;
    private readonly float _height;
    
    public bool DrawDebug = false;
    public bool DrawLighting = true;

    private readonly CameraProperties _cameraProperties;

    private Matrix4x4 ViewMatrix { get; }
    private Matrix4x4 ProjectionMatrix { get; }

    private Vector3 ToScreenVec(Vector3 vertex)
    {
        var vertex2D = new Vector2(vertex.X / vertex.Z, vertex.Y / vertex.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * _width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * _height;
        return new Vector3(screenX,screenY, vertex.Z);
    }

    public void DrawCircle(Vector3 position, float radius, Color color, bool filled = true)
    {
        var camPos = _cameraProperties.Position;

        var diff = camPos - position;
        var distance = diff.Length();

        radius /= distance;
        
        position = Vector3.Transform(position, ViewMatrix * ProjectionMatrix);

        position.X *= -1;
        position.Y *= -1;
        
        if (position.Z > 0)
        {
            return;
        }
        
        position = ToScreenVec(position);
        
        _handleBase.DrawCircle(new Vector2(position.X, position.Y), radius, color, filled);
    }

    public void DrawPolygon(TexturedTriangle triangle)
    {
        DrawPolygon(triangle, ClippingInstance);
    }

    private void DrawPolygon(TexturedTriangle triangle, ClippingInstance clippingInstance)
    {
        CheckDisposed();
        
        var normal = triangle.Triangle.Normal();
        normal = Vector3.Normalize(normal);
        var vCameraRay = Vector3.Normalize(triangle.Triangle.p1 - _cameraProperties.Position);
        
        if (Vector3.Dot(normal, vCameraRay) >= 0f)
            return;

        triangle.Triangle.Transform(ViewMatrix);
        
        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), triangle, 
            DrawingInstance, clippingInstance);

        foreach (var texturedTriangle in clippingInstance.Clipping)
        {
            triangle.Triangle.p1 = Vector3.Transform(texturedTriangle.Triangle.p1, ProjectionMatrix);
            triangle.Triangle.p2 = Vector3.Transform(texturedTriangle.Triangle.p2, ProjectionMatrix);
            triangle.Triangle.p3 = Vector3.Transform(texturedTriangle.Triangle.p3, ProjectionMatrix);
            triangle.TexturePoint1 = texturedTriangle.TexturePoint1;
            triangle.TexturePoint2 = texturedTriangle.TexturePoint2;
            triangle.TexturePoint3 = texturedTriangle.TexturePoint3;
        
            triangle.TexturePoint1.X /= triangle.Triangle.p1w;
            triangle.TexturePoint2.X /= triangle.Triangle.p2w;
            triangle.TexturePoint3.X /= triangle.Triangle.p3w;
            triangle.TexturePoint1.Y /= triangle.Triangle.p1w;
            triangle.TexturePoint2.Y /= triangle.Triangle.p2w;
            triangle.TexturePoint3.Y /= triangle.Triangle.p3w;
            
            triangle.Triangle.p1.X *= -1;
            triangle.Triangle.p2.X *= -1;
            triangle.Triangle.p3.X *= -1;
            triangle.Triangle.p1.Y *= -1;
            triangle.Triangle.p2.Y *= -1;
            triangle.Triangle.p3.Y *= -1;
            
            triangle.Triangle.p1 = ToScreenVec(triangle.Triangle.p1);
            triangle.Triangle.p2 = ToScreenVec(triangle.Triangle.p2);
            triangle.Triangle.p3 = ToScreenVec(triangle.Triangle.p3);
        }
    }
    
    public void Flush(ProfManager profManager)
    {
        using(profManager.Group("prepare_frame"))
            DrawingInstance.PrepareFrame();
        
        using(profManager.Group("draw_frame"))
        {
            foreach (var triToRaster in DrawingInstance.TriangleBuffer)
            {
                DrawPrimitiveTriangleWithClipping(triToRaster);
            }
        }
        
        using(profManager.Group("flush_frame"))
        {
            DrawingInstance.Flush();
            DrawingInstance.ShadersPool.Clear();
        }
        Dispose();
    }
    
    private void FlushScreen(TexturedTriangle triangle)
    {
        DrawingInstance.DrawVertex3dBuffer[0] = new Vector3(triangle.Triangle.p1.X / _width , triangle.Triangle.p1.Y / _height, triangle.Triangle.p1.Z);
        DrawingInstance.DrawVertex3dBuffer[1] = new Vector3(triangle.Triangle.p2.X / _width , triangle.Triangle.p2.Y / _height, triangle.Triangle.p2.Z);
        DrawingInstance.DrawVertex3dBuffer[2] = new Vector3(triangle.Triangle.p3.X / _width , triangle.Triangle.p3.Y / _height, triangle.Triangle.p3.Z);
        
        DrawingInstance.DrawVertexUntexturedBuffer[0] = new Vector2(triangle.Triangle.p1.X, triangle.Triangle.p1.Y);
        DrawingInstance.DrawVertexUntexturedBuffer[1] = new Vector2(triangle.Triangle.p2.X, triangle.Triangle.p2.Y);
        DrawingInstance.DrawVertexUntexturedBuffer[2] = new Vector2(triangle.Triangle.p3.X, triangle.Triangle.p3.Y);

        DrawingInstance.DrawVertexTexturePointBuffer[0] = triangle.TexturePoint1;
        DrawingInstance.DrawVertexTexturePointBuffer[1] = triangle.TexturePoint2;
        DrawingInstance.DrawVertexTexturePointBuffer[2] = triangle.TexturePoint3;
        
        DrawingInstance.DrawVertexBuffer[0] = new DrawVertexUV2D(DrawingInstance.DrawVertexUntexturedBuffer[0], DrawingInstance.DrawVertexTexturePointBuffer[0]);
        DrawingInstance.DrawVertexBuffer[1] = new DrawVertexUV2D(DrawingInstance.DrawVertexUntexturedBuffer[1], DrawingInstance.DrawVertexTexturePointBuffer[1]);
        DrawingInstance.DrawVertexBuffer[2] = new DrawVertexUV2D(DrawingInstance.DrawVertexUntexturedBuffer[2],  DrawingInstance.DrawVertexTexturePointBuffer[2]);
    }
    
    private Vector3 Normal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var u = new Vector3(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        var v = new Vector3(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);

        var nx = u.Y * v.Z - u.Z * v.Y;
        var ny = u.Z * v.X - u.X * v.Z;
        var nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }
    
    private Vector3 _curNormal = Vector3.Zero;

    private void DrawPrimitiveTriangleWithClipping(TexturedTriangle triToRaster)
    {
        DrawingInstance.ListTriangles.Clear();
        
        DrawingInstance.ListTriangles.Enqueue(triToRaster);
        FlushScreen(triToRaster);
        
        _curNormal = Normal(DrawingInstance.DrawVertex3dBuffer[0],DrawingInstance.DrawVertex3dBuffer[1],DrawingInstance.DrawVertex3dBuffer[2]);
        _curNormal = Vector3.Normalize(_curNormal);
        
        var nNewTriangles = 1;

        for (var p = 0; p < 4; p++)
        {
            var nTrisToAdd = 0;
            while (nNewTriangles > 0)
            {
                var test = DrawingInstance.ListTriangles.Dequeue();
                nNewTriangles--;
                
                switch (p)
                {
                    case 0:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), test, DrawingInstance, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 1:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, _height - 1, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), test, DrawingInstance, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 2:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), test, DrawingInstance, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 3:
                        Triangle.ClipAgainstClip(new Vector3(_width - 1, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f), test, DrawingInstance, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                }
                
                for (var w = 0; w < nTrisToAdd; w++)
                {
                    DrawingInstance.ListTriangles.Enqueue(ClippingInstance.Clipping[w]);
                }
            }
            nNewTriangles = DrawingInstance.ListTriangles.Count;
        }
        
        foreach (var triangle in DrawingInstance.ListTriangles)
        {
            DrawPrimitiveTriangle(triangle);
        }
    }

    private void DrawPrimitiveTriangle(TexturedTriangle triangle)
    {
        var texture = DrawingInstance.TextureBuffer[triangle.TextureId];
        FlushScreen(triangle);
        
        if(DrawLighting)
        {
            var shaderInst = DrawingInstance.ShadersPool.Pop();
            shaderInst.SetParameter("normal", _curNormal);
            shaderInst.SetParameter("p1", DrawingInstance.DrawVertex3dBuffer[0]);
            
            _handleBase.UseShader(shaderInst);
            _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList, texture, DrawingInstance.DrawVertexBuffer);
            _handleBase.UseShader(null);
        }else
        {
            _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList, texture, DrawingInstance.DrawVertexBuffer);
        }
        
        if(!DrawDebug) 
            return;
        
        _handleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop, DrawingInstance.DrawVertexUntexturedBuffer, Color.Blue);
    }
    

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, Entity<CameraComponent, Transform3dComponent> camera,
        DrawingInstance drawingInstance, IParallelManager parallel)
    {
        Parallel = parallel;
        _cameraProperties = new CameraProperties(camera.Comp2.WorldPosition, camera.Comp2.WorldAngle, camera.Comp1.FoV);
        DrawingInstance = drawingInstance;
        _handleBase = handleBase;
        _width = width;
        _height = height;
            
        var cosPitch = float.Cos((float)_cameraProperties.Angle.Pitch);
        var sinPitch = float.Sin((float)_cameraProperties.Angle.Pitch);
        var cosYaw = float.Cos((float)_cameraProperties.Angle.Yaw);
        var sinYaw = float.Sin((float)_cameraProperties.Angle.Yaw);
        
        var xaxis = new Vector3(cosYaw, 0, -sinYaw);
        var yaxis = new Vector3(sinPitch * sinPitch, cosPitch, cosYaw * sinPitch);
        var zaxis = new Vector3(sinYaw * cosPitch, -sinPitch, cosPitch * cosYaw);
        
        ViewMatrix = new Matrix4x4(xaxis.X,            yaxis.X,            zaxis.X,      0, 
           xaxis.Y,            yaxis.Y,            zaxis.Y,      0,
            xaxis.Z,            yaxis.Z,            zaxis.Z,      0, 
            -Vector3.Dot(xaxis,_cameraProperties.Position),
                -Vector3.Dot(yaxis,_cameraProperties.Position),
                -Vector3.Dot(zaxis,_cameraProperties.Position), 1);
        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / _cameraProperties.FoV,
            _width/_height,
            0.1f, 
            100.0f
        );

        DrawingInstance.ShaderInstance.SetParameter("cameraPos", _cameraProperties.Position);
    }
    
    private void CheckDisposed()
    {
        if (Disposed)
        {
            throw new Exception("DISPOSED");
        }
    }

    public void Dispose()
    {
        Disposed = true;
    }
}

public struct CameraProperties(Vector3 position, EulerAngles angle, float foV)
{
    public Vector3 Position = position;
    public EulerAngles Angle = angle;
    public float FoV = foV;
}