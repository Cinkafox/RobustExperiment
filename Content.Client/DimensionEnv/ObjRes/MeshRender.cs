using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.Utils;
using Content.Client.Viewport;
using Robust.Shared.Threading;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class MeshRender
{
    public Mesh Mesh;
    public Matrix4x4 Transform = Matrix4x4.Identity;
    public bool IsMeshTranslated { get; private set; }
    public int TextureBufferCoord { get; private set; }

    public MeshRender(Mesh mesh, int textureBufferCoord)
    {
        Mesh = mesh;
        TextureBufferCoord = textureBufferCoord;
        _translatedVertexes = new Vector3[mesh.Vertexes.Count];
    }
    
    private TexturedTriangle currTriangle = new TexturedTriangle(new Triangle(Vector3.Zero, Vector3.Zero, Vector3.Zero), Vector2.Zero,Vector2.Zero, Vector2.Zero, 0);

    private readonly Vector3[] _translatedVertexes;

    public Vector3[] TranslatedVertexes
    {
        get
        {
            if(!IsMeshTranslated) TranslateMesh();
            return _translatedVertexes;
        }
    }

    public void TranslateMesh()
    {
        for (int i = 0; i < Mesh.Vertexes.Count; i++)
        {
            _translatedVertexes[i] = Vector3.Transform(Mesh.Vertexes[i], Transform);
        }

        IsMeshTranslated = true;
    }

    public void Draw(DrawingHandle3d handle)
    {
        foreach (var face in Mesh.Faces)
        {
            DrawFace(face, handle);
        }
        
        IsMeshTranslated = false;
    }

    public void DrawParallel(DrawingHandle3d handle3d)
    {
        var task = new DrawParallel(this, handle3d, Logger.GetSawmill("GADEEM"));
        
        handle3d.Parallel.ProcessNow(task, Mesh.Faces.Count);

        IsMeshTranslated = false;
    }

    public void DrawFace(Face face, DrawingHandle3d handle)
    {
        switch (face.Vertices.Length)
        {
            case 3:
                DrawPolygon(face, handle, 0, 1, 2);
                break;
            case > 3:
            {
                for (int i = 1; i < face.Vertices.Length - 1; i++)
                {
                    DrawPolygon(face, handle, 0, i, i + 1);
                }

                break;
            }
        }
    }

    public void PreparePolygon(Face face,DrawingHandle3d handle, int i1, int i2, int i3)
    {
        var vert1 = face.Vertices[i1];
        var vert2 = face.Vertices[i2];
        var vert3 = face.Vertices[i3];
        
        currTriangle.Triangle.p1 = TranslatedVertexes[vert1.VertexId];
        currTriangle.Triangle.p2 = TranslatedVertexes[vert2.VertexId];
        currTriangle.Triangle.p3 = TranslatedVertexes[vert3.VertexId];
        
        if (face.HasTexturePos)
        {
            currTriangle.TextureId = TextureBufferCoord + face.MaterialId;
            currTriangle.TexturePoint1 = Mesh.TextureCoords[vert1.TexPosId - 1];
            currTriangle.TexturePoint2 = Mesh.TextureCoords[vert2.TexPosId - 1];
            currTriangle.TexturePoint3 = Mesh.TextureCoords[vert3.TexPosId - 1];
        }
    }

    private void DrawPolygon(Face face, DrawingHandle3d handle, int i1, int i2, int i3)
    {
        PreparePolygon(face, handle, i1, i2, i3);
        handle.DrawPolygon(currTriangle);
    }
}

public struct DrawParallel : IParallelRobustJob
{
    public int BatchSize => 16;
    public int MinimumBatchParallel => 4;

    private readonly MeshRender _meshRender;
    private readonly DrawingHandle3d _handle;
    private readonly ISawmill _sawmill;

    public DrawParallel(MeshRender meshRender, DrawingHandle3d handle3d, ISawmill sawmill)
    {
        _meshRender = meshRender;
        _handle = handle3d;
        _sawmill = sawmill;
    }

    public void Execute(int index)
    {
        try
        {
            var face = _meshRender.Mesh.Faces[index];
            DrawFace(face);
        }
        catch (Exception e)
        {
            _sawmill.Error("FUCK FUCK FUCK " + e.Message + " " + e.StackTrace);
           // throw;
        }
    }
    
    public void DrawFace(Face face)
    {
        switch (face.Vertices.Length)
        {
            case 3:
                DrawPolygon(face, 0, 1, 2);
                break;
            case > 3:
            {
                for (int i = 1; i < face.Vertices.Length - 1; i++)
                {
                    DrawPolygon(face, 0, i, i + 1);
                }

                break;
            }
        }
    }
    
    private void DrawPolygon(Face face, int i1, int i2, int i3)
    {
        var vert1 = face.Vertices[i1];
        var vert2 = face.Vertices[i2];
        var vert3 = face.Vertices[i3];
        
        var v1 = _meshRender.TranslatedVertexes[vert1.VertexId];
        var v2 = _meshRender.TranslatedVertexes[vert2.VertexId];
        var v3 = _meshRender.TranslatedVertexes[vert3.VertexId];

        var triangle = new Triangle(v1, v2, v3);
        
        var matId = _meshRender.TextureBufferCoord + face.MaterialId;
            
        var t1 = _meshRender.Mesh.TextureCoords[vert1.TexPosId - 1];
        var t2 = _meshRender.Mesh.TextureCoords[vert2.TexPosId - 1];
        var t3 = _meshRender.Mesh.TextureCoords[vert3.TexPosId - 1];
        DrawPolyInternal(new TexturedTriangle(triangle, t1, t2, t3, matId));
    }
    
    private void DrawPolyInternal(TexturedTriangle triangle)
    {
        var normal = triangle.Triangle.Normal();
        normal = Vector3.Normalize(normal);
        var vCameraRay = Vector3.Subtract(triangle.Triangle.p1, _handle.CameraProperties.Position);
        if(Vector3.Dot(normal, vCameraRay) >= 1) return;

        var ci = new ClippingInstance();

        triangle.Triangle.Transform(_handle.ViewMatrix);

        Triangle.ClipAgainstClip(new Vector3(0.0f, 0.0f, 0.1f), new Vector3(0.0f, 0.0f, 1.0f), triangle,
            ci);

        for (int i = 0; i < ci.Clipping.Length; i++)
        {
            triangle.Triangle.p1 = Vector3.Transform(ci.Clipping[i].Triangle.p1, _handle.ProjectionMatrix);
            triangle.Triangle.p2 = Vector3.Transform(ci.Clipping[i].Triangle.p2, _handle.ProjectionMatrix);
            triangle.Triangle.p3 = Vector3.Transform(ci.Clipping[i].Triangle.p3, _handle.ProjectionMatrix);
            triangle.TexturePoint1 = ci.Clipping[i].TexturePoint1;
            triangle.TexturePoint2 = ci.Clipping[i].TexturePoint2;
            triangle.TexturePoint3 = ci.Clipping[i].TexturePoint3;
        
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
            
            triangle.Triangle.p1 = _handle.ToScreenVec(triangle.Triangle.p1);
            triangle.Triangle.p2 = _handle.ToScreenVec(triangle.Triangle.p2);
            triangle.Triangle.p3 = _handle.ToScreenVec(triangle.Triangle.p3);
            
            lock (_handle.DrawingInstance.TriangleBuffer)
            {
                _handle.DrawingInstance.AppendTriangle(triangle);
            }
        }
    }
}