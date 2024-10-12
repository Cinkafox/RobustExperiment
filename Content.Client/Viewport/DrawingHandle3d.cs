using System.Numerics;
using Content.Client.Camera;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Shared.Profiling;
using Robust.Shared.Threading;

namespace Content.Client.Viewport;

public sealed class DrawingHandle3d : IDisposable
{
    public bool Disposed { get; private set; }
    
    private readonly ProfManager _prof;
    private readonly IParallelManager _parallel;

    private readonly DrawingHandleBase _handleBase;
    private readonly DrawingInstance _drawingInstance;

    public readonly float Width;
    public readonly float Height;

    private CameraProperties _cameraProperties;

    public Matrix4x4 ViewMatrix { get; }
    public Matrix4x4 ProjectionMatrix { get; }
    
    public Vector4 ToScreenVec(Vector4 vertex)
    {
        var vertex2D = new Vector2(vertex.X / vertex.Z, vertex.Y / vertex.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new Vector4(screenX,screenY, vertex.Z, vertex.W);
    }

    private void FlushScreen(TexturedTriangle triangle)
    {
        _drawingInstance.DrawVertexBuffer[0] = ToScreen(triangle.Triangle.p1.ToVec3(),triangle.TexturePoint1);
        _drawingInstance.DrawVertexBuffer[1] = ToScreen(triangle.Triangle.p2.ToVec3(),triangle.TexturePoint2);
        _drawingInstance.DrawVertexBuffer[2] = ToScreen(triangle.Triangle.p3.ToVec3(),triangle.TexturePoint3);
    }
    
    public DrawVertexUV2D ToScreen(Vector3 vertex, Vector2 uvPos)
    {
        return new DrawVertexUV2D(new Vector2(vertex.X, vertex.Y),uvPos);
    }

    public void DrawCircle(Vector3 position, float radius, Color color, bool filled = true)
    {
        var camPos = _cameraProperties.Position;

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
        
        position = ToScreenVec(position.ToVec4()).ToVec3();
        
        _handleBase.DrawCircle(new Vector2(position.X, position.Y), radius, color, filled);
    }
    
    public void DrawPolygon(Triangle triangle, Vector2 p1, Vector2 p2, Vector2 p3, int textureId)
    {
        CheckDisposed();
        
        var normal = triangle.Normal();
        normal = Vector3.Normalize(normal);
        var vCameraRay = Vector3.Subtract(triangle.p1.ToVec3(), _cameraProperties.Position);
        if(Vector3.Dot(normal, vCameraRay) >= 1) return;

        triangle.Transform(ViewMatrix);
        
        var trtex = new TexturedTriangle(triangle, p1, p2, p3, textureId);

        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), trtex,
            _drawingInstance);

        for (int i = 0; i < _drawingInstance.Clipping.Length; i++)
        {
            trtex.Triangle.p1 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p1.ToVec3(), ProjectionMatrix).ToVec4();
            trtex.Triangle.p2 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p2.ToVec3(), ProjectionMatrix).ToVec4();
            trtex.Triangle.p3 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p3.ToVec3(), ProjectionMatrix).ToVec4();
            trtex.TexturePoint1 = _drawingInstance.Clipping[i].TexturePoint1;
            trtex.TexturePoint2 = _drawingInstance.Clipping[i].TexturePoint2;
            trtex.TexturePoint3 = _drawingInstance.Clipping[i].TexturePoint3;

            trtex.TexturePoint1.X /= trtex.Triangle.p1.W;
            trtex.TexturePoint2.X /= trtex.Triangle.p2.W;
            trtex.TexturePoint3.X /= trtex.Triangle.p3.W;
            trtex.TexturePoint1.Y /= trtex.Triangle.p1.W;
            trtex.TexturePoint2.Y /= trtex.Triangle.p2.W;
            trtex.TexturePoint3.Y /= trtex.Triangle.p3.W;
            
            trtex.Triangle.p1.X *= -1;
            trtex.Triangle.p2.X *= -1;
            trtex.Triangle.p3.X *= -1;
            trtex.Triangle.p1.Y *= -1;
            trtex.Triangle.p2.Y *= -1;
            trtex.Triangle.p3.Y *= -1;
            
            trtex.Triangle.p1 = ToScreenVec(trtex.Triangle.p1);
            trtex.Triangle.p2 = ToScreenVec(trtex.Triangle.p2);
            trtex.Triangle.p3 = ToScreenVec(trtex.Triangle.p3);
            
            _drawingInstance.AppendTriangle(trtex);
        }
    }
    
    public void Flush()
    {
        foreach (var (_, triToRaster) in _drawingInstance.TriangleBuffer)
        {
            _drawingInstance.ListTriangles.Clear();
            
            _drawingInstance.ListTriangles.Add(triToRaster);
            int nNewTriangles = 1;

            for (int p = 0; p < 4; p++)
            {
                int nTrisToAdd = 0;
                while (nNewTriangles > 0)
                {
                    var test = _drawingInstance.ListTriangles[0];
                    _drawingInstance.ListTriangles.RemoveAt(0);
                    nNewTriangles--;
                    
                    switch (p)
                    {
                        case 0:
                            Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), test, _drawingInstance);
                            nTrisToAdd = _drawingInstance.Clipping.Length;
                            break;
                        case 1:
                            Triangle.ClipAgainstClip(new Vector3(0.0f, Height - 1, 0.0f), new Vector3(0.0f, -1.0f, 0.0f), test, _drawingInstance);
                            nTrisToAdd = _drawingInstance.Clipping.Length;
                            break;
                        case 2:
                            Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), test, _drawingInstance);
                            nTrisToAdd = _drawingInstance.Clipping.Length;
                            break;
                        case 3:
                            Triangle.ClipAgainstClip(new Vector3(Width - 1, 0.0f, 0.0f), new Vector3(-1.0f, 0.0f, 0.0f), test, _drawingInstance);
                            nTrisToAdd = _drawingInstance.Clipping.Length;
                            break;
                    }

                    // Clipping may yield a variable number of triangles, so
                    // add these new ones to the back of the queue for subsequent
                    // clipping against next planes
                    for (int w = 0; w < nTrisToAdd; w++)
                    {
                        _drawingInstance.ListTriangles.Add(_drawingInstance.Clipping[w]);
                    }
                }
                nNewTriangles = _drawingInstance.ListTriangles.Count;
            }
            
            using (_prof.Group("Handle.Draw"))
            {
                foreach (var triangle in _drawingInstance.ListTriangles)
                {
                    var texture = _drawingInstance.TextureBuffer[triangle.TextureId];
            
                    FlushScreen(triangle);
                    _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture, _drawingInstance.DrawVertexBuffer);
                }
            }
        }
        
        _drawingInstance.Flush();
        Dispose();
    }
    

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, CameraProperties cameraProperties,
        DrawingInstance drawingInstance, ProfManager profManager, IParallelManager parallel)
    {
        _prof = profManager;
        _parallel = parallel;
        _cameraProperties = cameraProperties;
        _drawingInstance = drawingInstance;
        _handleBase = handleBase;
        Width = width;
        Height = height;
            
        float cosPitch = float.Cos((float)_cameraProperties.Angle.Pitch);
        float sinPitch = float.Sin((float)_cameraProperties.Angle.Pitch);
        float cosYaw = float.Cos((float)_cameraProperties.Angle.Yaw);
        float sinYaw = float.Sin((float)_cameraProperties.Angle.Yaw);
        
        Vector3 xaxis = new Vector3(cosYaw, 0, -sinYaw);
        Vector3 yaxis = new Vector3(sinPitch * sinPitch, cosPitch, cosYaw * sinPitch);
        Vector3 zaxis = new Vector3(sinYaw * cosPitch, -sinPitch, cosPitch * cosYaw);
        
        ViewMatrix = new Matrix4x4(xaxis.X,            yaxis.X,            zaxis.X,      0, 
           xaxis.Y,            yaxis.Y,            zaxis.Y,      0,
            xaxis.Z,            yaxis.Z,            zaxis.Z,      0, 
            -Vector3.Dot(xaxis,_cameraProperties.Position),
                -Vector3.Dot(yaxis,_cameraProperties.Position),
                -Vector3.Dot(zaxis,_cameraProperties.Position), 1);

        
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / _cameraProperties.FoV, // Field of view
            Width/Height, // Aspect ratio
            0.1f, // Near plane
            100.0f           // Far plane
        );
    }
    
    private void CheckDisposed()
    {
        if (Disposed)
        {
            throw new Exception("DISPODED");
        }
    }

    public void Dispose()
    {
        Disposed = true;
    }
}