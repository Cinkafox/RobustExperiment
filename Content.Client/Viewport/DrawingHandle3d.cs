﻿using System.Linq;
using System.Numerics;
using Content.Client.Utils;
using Robust.Client.Graphics;
using Robust.Shared.Profiling;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.Viewport;

public sealed class DrawingHandle3d : IDisposable
{
    public bool Debug = false;
    public bool Disposed;
    private ProfManager _prof;

    private readonly DrawingHandleBase _handleBase;
    private readonly DrawingInstance _drawingInstance;

    public DrawingHandleBase HandleBase => _handleBase;

    public readonly float Width;
    public readonly float Height;

    private CameraProperties _cameraProperties;

    public Matrix4 ViewMatrix;
    public Matrix4 ProjectionMatrix;
    
    public Vector2 ToScreenVec(Vector3 vertex)
    {
        var vertex2D = new Vector2(vertex.X / vertex.Z, vertex.Y / vertex.Z);
        
        var screenX = (vertex2D.X + 1.0f) * 0.5f * Width;
        var screenY = (1.0f - vertex2D.Y) * 0.5f * Height;
        return new Vector2(screenX,screenY);
    }

    private void FlushScreen(TexturedTriangle triangle)
    {
        _drawingInstance.DrawVertexBuffer[0] = ToScreen(triangle.Triangle.p1.Xyz,triangle.TexturePoint1);
        _drawingInstance.DrawVertexBuffer[1] = ToScreen(triangle.Triangle.p2.Xyz,triangle.TexturePoint2);
        _drawingInstance.DrawVertexBuffer[2] = ToScreen(triangle.Triangle.p3.Xyz,triangle.TexturePoint3);
    }
    
    public DrawVertexUV2D ToScreen(Vector3 vertex, Vector2 uvPos)
    {
        return new DrawVertexUV2D(vertex.Xy,uvPos);
    }
    
    public void DrawPolygon(Triangle triangle, Vector2 p1, Vector2 p2, Vector2 p3, int textureId)
    {
        CheckDisposed();
        
        triangle.Transform(ViewMatrix);
        
        var normal = triangle.Normal();
        normal.Normalize();

        var vCameraRay = Vector3.Subtract(triangle.p1.Xyz, _cameraProperties.Position);

        var dotProduct = Vector3.Dot(normal, vCameraRay);
        
        if(dotProduct >= 1) return;

        var trtex = new TexturedTriangle(triangle, p1, p2, p3, textureId);

        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), trtex,
            _drawingInstance);

        for (int i = 0; i < _drawingInstance.Clipping.Length; i++)
        {
            trtex.Triangle.p1 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p1.Xyz, ProjectionMatrix).ToVec4();
            trtex.Triangle.p2 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p2.Xyz, ProjectionMatrix).ToVec4();
            trtex.Triangle.p3 = Vector3.Transform(_drawingInstance.Clipping[i].Triangle.p3.Xyz, ProjectionMatrix).ToVec4();
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
            
            // Offset verts into visible normalised space
            var vOffsetView = new Vector3(1,1,0);
            trtex.Triangle.p1 = Vector4.Add(trtex.Triangle.p1, new Vector4(vOffsetView, 0));
            trtex.Triangle.p2 = Vector4.Add(trtex.Triangle.p2, new Vector4(vOffsetView, 0));
            trtex.Triangle.p3 = Vector4.Add(trtex.Triangle.p3, new Vector4(vOffsetView, 0));
            trtex.Triangle.p1.X *= 0.5f * Width;
            trtex.Triangle.p1.Y *= 0.5f * Height;
            trtex.Triangle.p2.X *= 0.5f * Width;
            trtex.Triangle.p2.Y *= 0.5f * Height;
            trtex.Triangle.p3.X *= 0.5f * Width;
            trtex.Triangle.p3.Y *= 0.5f * Height;
            
            _drawingInstance.AppendTriangle(trtex);
        }
    }
    
    public void Flush()
    {
        using (_prof.Group("Handle.Sort")) {
             //_drawingInstance.Sort();
        }
        
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
                    // Take triangle from front of queue
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
        }

        using (_prof.Group("Handle.Draw"))
        {
            foreach (var (_, triangle) in _drawingInstance.TriangleBuffer)
            {
                var texture = _drawingInstance.TextureBuffer[triangle.TextureId];
            
                FlushScreen(triangle);
                _handleBase.DrawPrimitives(DrawPrimitiveTopology.TriangleList,texture, _drawingInstance.DrawVertexBuffer);
            }
        }
        
        _drawingInstance.Flush();
        Dispose();
    }
    

    public DrawingHandle3d(DrawingHandleBase handleBase, float width, float height, CameraProperties cameraProperties,DrawingInstance drawingInstance,ProfManager profManager)
    {
        _prof = profManager;
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
        
        ViewMatrix = new Matrix4(new Vector4(xaxis.X,            yaxis.X,            zaxis.X,      0), 
            new Vector4(xaxis.Y,            yaxis.Y,            zaxis.Y,      0),
            new Vector4(xaxis.Z,            yaxis.Z,            zaxis.Z,      0), 
            new Vector4(-Vector3.Dot(xaxis,_cameraProperties.Position),
                -Vector3.Dot(yaxis,_cameraProperties.Position),
                -Vector3.Dot(zaxis,_cameraProperties.Position), 1));

        
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
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