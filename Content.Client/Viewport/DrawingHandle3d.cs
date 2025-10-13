using System.Numerics;
using Content.Client.Camera;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Shared.Threading;

namespace Content.Client.Viewport;

public sealed class DrawingHandle3d : IDisposable
{
    public bool Disposed { get; private set; }
    
    public readonly IParallelManager Parallel;

    public readonly DrawingHandleBase HandleBase;
    public readonly DrawingInstance DrawingInstance;

    public ClippingInstance ClippingInstance => DrawingInstance.ClippingInstance;

    public readonly float Width;
    public readonly float Height;
    
    public bool DrawDebug = false;
    public bool DrawLighting = true;

    public readonly CameraProperties CameraProperties;

    public Matrix4x4 ViewMatrix { get; }
    public Matrix4x4 ProjectionMatrix { get; }
    
    public Vector3 ToScreenVec(Vector3 vertex)
    {
        var vertex2D = new Vector2(vertex.X / vertex.Z, vertex.Y / vertex.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new Vector3(screenX,screenY, vertex.Z);
    }

    public void DrawCircle(Vector3 position, float radius, Color color, bool filled = true)
    {
        var camPos = CameraProperties.Position;

        var distance = Math.Sqrt(Math.Pow(camPos.X - position.X, 2) + Math.Pow(camPos.Y - position.Y, 2) +
                                 Math.Pow(camPos.Z - position.Z, 2));

        radius = (float)(radius / distance);
        
        position = Vector3.Transform(position, ViewMatrix);
        position = Vector3.Transform(position, ProjectionMatrix);

        position.X *= -1;
        position.Y *= -1;
        
        if (position.Z > 0)
        {
            return;
        }
        
        position = ToScreenVec(position);
        
        HandleBase.DrawCircle(new Vector2(position.X, position.Y), radius, color, filled);
    }

    public void DrawPolygon(TexturedTriangle triangle)
    {
        DrawPolygon(triangle, ClippingInstance);
    }

    public void DrawPolygon(TexturedTriangle triangle, ClippingInstance clippingInstance)
    {
        CheckDisposed();
        
        var normal = triangle.Triangle.Normal();
        normal = Vector3.Normalize(normal);
        var vCameraRay = Vector3.Subtract(triangle.Triangle.p1, CameraProperties.Position);
        if(Vector3.Dot(normal, vCameraRay) >= 1) return;

        triangle.Triangle.Transform(ViewMatrix);
        

        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), triangle,
            clippingInstance);

        for (int i = 0; i < clippingInstance.Clipping.Length; i++)
        {
            triangle.Triangle.p1 = Vector3.Transform(clippingInstance.Clipping[i].Triangle.p1, ProjectionMatrix);
            triangle.Triangle.p2 = Vector3.Transform(clippingInstance.Clipping[i].Triangle.p2, ProjectionMatrix);
            triangle.Triangle.p3 = Vector3.Transform(clippingInstance.Clipping[i].Triangle.p3, ProjectionMatrix);
            triangle.TexturePoint1 = clippingInstance.Clipping[i].TexturePoint1;
            triangle.TexturePoint2 = clippingInstance.Clipping[i].TexturePoint2;
            triangle.TexturePoint3 = clippingInstance.Clipping[i].TexturePoint3;
        
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
            
            lock (DrawingInstance.TriangleBuffer)
            {
                DrawingInstance.AppendTriangle(triangle);
            }
        }
    }
    
    public void Flush()
    {
        foreach (var (_, triToRaster) in DrawingInstance.TriangleBuffer)
        {
            DrawPrimitiveTriangleWithClipping(triToRaster);
        }
        
        DrawingInstance.Flush();
        DrawingInstance.ShadersPool.Clear();
        Dispose();
    }
    
    private void FlushScreen(TexturedTriangle triangle)
    {
        DrawingInstance.DrawVertex3dBuffer[0] = new Vector3(triangle.Triangle.p1.X / Width , triangle.Triangle.p1.Y / Height, triangle.Triangle.p1.Z);
        DrawingInstance.DrawVertex3dBuffer[1] = new Vector3(triangle.Triangle.p2.X / Width , triangle.Triangle.p2.Y / Height, triangle.Triangle.p2.Z);
        DrawingInstance.DrawVertex3dBuffer[2] = new Vector3(triangle.Triangle.p3.X / Width , triangle.Triangle.p3.Y / Height, triangle.Triangle.p3.Z);
        
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

        float nx = u.Y * v.Z - u.Z * v.Y;
        float ny = u.Z * v.X - u.X * v.Z;
        float nz = u.X * v.Y - u.Y * v.X;

        return new Vector3(nx, ny, nz);
    }
    
    private Vector3 CurNormal = Vector3.Zero;

    private void DrawPrimitiveTriangleWithClipping(TexturedTriangle triToRaster)
    {
        DrawingInstance.ListTriangles.Clear();
        
        DrawingInstance.ListTriangles.Add(triToRaster);
        FlushScreen(triToRaster);
        
        CurNormal = Normal(DrawingInstance.DrawVertex3dBuffer[0],DrawingInstance.DrawVertex3dBuffer[1],DrawingInstance.DrawVertex3dBuffer[2]);
        CurNormal = Vector3.Normalize(CurNormal);
        
        int nNewTriangles = 1;

        for (int p = 0; p < 4; p++)
        {
            int nTrisToAdd = 0;
            while (nNewTriangles > 0)
            {
                var test = DrawingInstance.ListTriangles[0];
                DrawingInstance.ListTriangles.RemoveAt(0);
                nNewTriangles--;
                
                switch (p)
                {
                    case 0:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), test, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 1:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, Height - 1, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), test, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 2:
                        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), test, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                    case 3:
                        Triangle.ClipAgainstClip(new Vector3(Width - 1, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f), test, ClippingInstance);
                        nTrisToAdd = ClippingInstance.Clipping.Length;
                        break;
                }
                
                for (int w = 0; w < nTrisToAdd; w++)
                {
                    DrawingInstance.ListTriangles.Add(ClippingInstance.Clipping[w]);
                }
            }
            nNewTriangles = DrawingInstance.ListTriangles.Count;
        }
        
        foreach (var triangle in DrawingInstance.ListTriangles)
        {
            DrawPrimitiveTriangle(triangle);
        }
    }

    public void DrawPrimitiveTriangle(TexturedTriangle triangle)
    {
        var texture = DrawingInstance.TextureBuffer[triangle.TextureId];
        FlushScreen(triangle);
        
        if(DrawLighting)
        {
            var shaderInst = DrawingInstance.ShadersPool.Pop();
            shaderInst.SetParameter("normal", CurNormal);
            shaderInst.SetParameter("p1", DrawingInstance.DrawVertex3dBuffer[0]);
            
            HandleBase.UseShader(shaderInst);
            HandleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList, texture, DrawingInstance.DrawVertexBuffer);
            HandleBase.UseShader(null);
        }else
        {
            HandleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList, texture, DrawingInstance.DrawVertexBuffer);
        }
        
        
        if(!DrawDebug) 
            return;
        
        HandleBase.DrawPrimitives(DrawPrimitiveTopology.LineLoop, DrawingInstance.DrawVertexUntexturedBuffer, Color.Blue);
    }
    

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, CameraProperties cameraProperties,
        DrawingInstance drawingInstance, IParallelManager parallel)
    {
        Parallel = parallel;
        CameraProperties = cameraProperties;
        DrawingInstance = drawingInstance;
        HandleBase = handleBase;
        Width = width;
        Height = height;
            
        float cosPitch = float.Cos((float)CameraProperties.Angle.Pitch);
        float sinPitch = float.Sin((float)CameraProperties.Angle.Pitch);
        float cosYaw = float.Cos((float)CameraProperties.Angle.Yaw);
        float sinYaw = float.Sin((float)CameraProperties.Angle.Yaw);
        
        Vector3 xaxis = new Vector3(cosYaw, 0, -sinYaw);
        Vector3 yaxis = new Vector3(sinPitch * sinPitch, cosPitch, cosYaw * sinPitch);
        Vector3 zaxis = new Vector3(sinYaw * cosPitch, -sinPitch, cosPitch * cosYaw);
        
        ViewMatrix = new Matrix4x4(xaxis.X,            yaxis.X,            zaxis.X,      0, 
           xaxis.Y,            yaxis.Y,            zaxis.Y,      0,
            xaxis.Z,            yaxis.Z,            zaxis.Z,      0, 
            -Vector3.Dot(xaxis,CameraProperties.Position),
                -Vector3.Dot(yaxis,CameraProperties.Position),
                -Vector3.Dot(zaxis,CameraProperties.Position), 1);
        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / CameraProperties.FoV,
            Width/Height,
            0.1f, 
            100.0f
        );

        DrawingInstance.ShaderInstance.SetParameter("cameraPos", cameraProperties.Position);
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