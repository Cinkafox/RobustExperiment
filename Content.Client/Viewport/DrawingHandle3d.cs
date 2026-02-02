using System.Numerics;
using Content.Client.Utils;
using Content.Shared.Camera;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Client.Graphics;
using Robust.Shared.Profiling;

namespace Content.Client.Viewport;

public sealed class DrawingHandle3d : IDisposable
{
    private bool Disposed { get; set; }
    
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
    private Matrix4x4 ViewProjectionMatrix { get; } 
    
    private Vector3 ToScreenVec(Vector4 clipPos)
    {
        if (MathF.Abs(clipPos.W) < 1e-6f)
            return Vector3.Zero;
        
        var ndcX = clipPos.X / clipPos.W;
        var ndcY = clipPos.Y / clipPos.W;
        var ndcZ = clipPos.Z / clipPos.W;
        
        var screenX = (ndcX + 1.0f) * 0.5f * _width;
        var screenY = (ndcY + 1.0f) * 0.5f * _height; 
        
        return new Vector3(screenX, screenY, ndcZ);
    }

    public void DrawCircle(Vector3 position, float radius, Color color, bool filled = true)
    {
        var camPos = _cameraProperties.Position;
        var diff = camPos - position;
        var distance = diff.Length();
        
        if (distance < 0.1f) return;
        
        radius /= distance;
        
        var clipPos = Vector4.Transform(new Vector4(position, 1.0f), ViewProjectionMatrix);
        
        if (clipPos.Z < -clipPos.W || clipPos.Z > clipPos.W || 
            clipPos.X < -clipPos.W || clipPos.X > clipPos.W ||
            clipPos.Y < -clipPos.W || clipPos.Y > clipPos.W)
            return;
            
        var screenPos = ToScreenVec(clipPos);
        _handleBase.DrawCircle(new Vector2(screenPos.X, screenPos.Y), radius, color, filled);
    }
    
    public void DrawPolygon(TexturedTriangle triangle)
    {
        CheckDisposed();
        
        var normal = Vector3.Normalize(triangle.Triangle.Normal());
        var vCameraRay = Vector3.Normalize(triangle.Triangle.GetP1() - _cameraProperties.Position);
        if (Vector3.Dot(normal, vCameraRay) >= 0f)
            return;
        
        triangle.Triangle.Transform(ViewMatrix);
        
        ClippingInstance.ClipAgainstClip(
            new Vector3(0.0f, 0.0f, 0.1f), 
            new Vector3(0.0f, 0.0f, 1.0f), 
            triangle, 
            DrawingInstance);

        foreach (var clippedTriangle in ClippingInstance.Clipping)
        {
            clippedTriangle.Triangle.Transform(ProjectionMatrix);
            
            clippedTriangle.Triangle.SetP1(ToScreenVec(clippedTriangle.Triangle.p1));
            clippedTriangle.Triangle.SetP2(ToScreenVec(clippedTriangle.Triangle.p2));
            clippedTriangle.Triangle.SetP3(ToScreenVec(clippedTriangle.Triangle.p3));
            
            DrawingInstance.AddTriangleDrawn(clippedTriangle);
        }
    }
    
    public void Flush(ProfManager profManager)
    {
        using(profManager.Group("prepare_frame"))
            DrawingInstance.PrepareFrame();
        
        using(profManager.Group("draw_frame"))
        {
            foreach (var triToRaster in DrawingInstance.EnumerateDrawnTriangles())
            {
                DrawPrimitiveTriangleWithClipping(triToRaster);
            }
        }
        
        using(profManager.Group("flush_frame"))
        {
            DrawingInstance.Flush();
        }
        Dispose();
    }
    
    private void FlushScreen(TexturedTriangle triangle)
    {
        SetVector3Data(ref DrawingInstance.DrawVertex3dBuffer[0], triangle.Triangle.p1.X / _width , triangle.Triangle.p1.Y / _height, triangle.Triangle.p1.Z);
        SetVector3Data(ref DrawingInstance.DrawVertex3dBuffer[1], triangle.Triangle.p2.X / _width , triangle.Triangle.p2.Y / _height, triangle.Triangle.p2.Z);
        SetVector3Data(ref DrawingInstance.DrawVertex3dBuffer[2], triangle.Triangle.p3.X / _width , triangle.Triangle.p3.Y / _height, triangle.Triangle.p3.Z);
        
        SetVector2Data(ref DrawingInstance.DrawVertexUntexturedBuffer[0], triangle.Triangle.p1.X, triangle.Triangle.p1.Y);
        SetVector2Data(ref DrawingInstance.DrawVertexUntexturedBuffer[1], triangle.Triangle.p2.X, triangle.Triangle.p2.Y);
        SetVector2Data(ref DrawingInstance.DrawVertexUntexturedBuffer[2], triangle.Triangle.p3.X, triangle.Triangle.p3.Y);

        DrawingInstance.DrawVertexTexturePointBuffer[0] = triangle.TexturePoint1;
        DrawingInstance.DrawVertexTexturePointBuffer[1] = triangle.TexturePoint2;
        DrawingInstance.DrawVertexTexturePointBuffer[2] = triangle.TexturePoint3;

        SetDrawVertexUV2D(ref DrawingInstance.DrawVertexBuffer[0], DrawingInstance.DrawVertexUntexturedBuffer[0],
            DrawingInstance.DrawVertexTexturePointBuffer[0]);
        SetDrawVertexUV2D(ref DrawingInstance.DrawVertexBuffer[1], DrawingInstance.DrawVertexUntexturedBuffer[1],
            DrawingInstance.DrawVertexTexturePointBuffer[1]);
        SetDrawVertexUV2D(ref DrawingInstance.DrawVertexBuffer[2], DrawingInstance.DrawVertexUntexturedBuffer[2],
            DrawingInstance.DrawVertexTexturePointBuffer[2]);
    }

    private void SetVector3Data(ref Vector3 vector3, float x, float y, float z)
    {
        vector3.X = x;
        vector3.Y = y;
        vector3.Z = z;
    }

    private void SetVector2Data(ref Vector2 vector2, float x, float y)
    {
        vector2.X = x;
        vector2.Y = y;
    }

    private void SetDrawVertexUV2D(ref DrawVertexUV2D vertex, in Vector2 position, in Vector2 uv)
    {
        vertex.UV = uv;
        vertex.Position = position;
    }
    
    private Vector3 _curNormal = Vector3.Zero;

    private void DrawPrimitiveTriangleWithClipping(TexturedTriangle triToRaster)
    {
        DrawingInstance.ListTriangles.Clear();
        
        DrawingInstance.ListTriangles.Enqueue(triToRaster);

        _curNormal = triToRaster.Triangle.Normal();
        
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
                        ClippingInstance.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), test, DrawingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 1:
                        ClippingInstance.ClipAgainstClip(new Vector3(0.0f, _height - 1, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), test, DrawingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 2:
                        ClippingInstance.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), test, DrawingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 3:
                        ClippingInstance.ClipAgainstClip(new Vector3(_width - 1, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f), test, DrawingInstance);
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
            var shaderInst = DrawingInstance.ShadersPool.Take();
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
        
        _handleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop, DrawingInstance.DrawVertexUntexturedBuffer, Color.White);
    }
    

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, Entity<CameraComponent, Transform3dComponent> camera,
        DrawingInstance drawingInstance)
    {
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
        
        ViewMatrix = new Matrix4x4(
            xaxis.X, yaxis.X, zaxis.X, 0,
            xaxis.Y, yaxis.Y, zaxis.Y, 0,
            xaxis.Z, yaxis.Z, zaxis.Z, 0,
            -Vector3.Dot(xaxis, _cameraProperties.Position),
            -Vector3.Dot(yaxis, _cameraProperties.Position),
            -Vector3.Dot(zaxis, _cameraProperties.Position),
            1
        );
        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / _cameraProperties.FoV,
            _width / _height,
            0.1f, 
            100.0f
        );
        
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

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