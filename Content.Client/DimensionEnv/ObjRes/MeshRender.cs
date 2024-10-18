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
    
    private TexturedTriangle _currTriangle = new(
        new Triangle(Vector3.Zero, Vector3.Zero, Vector3.Zero),
        Vector2.Zero,
        Vector2.Zero,
        Vector2.Zero,
        0);

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
        var task = new DrawParallel(this, handle3d);
        
        handle3d.Parallel.ProcessNow(task, Mesh.Faces.Count);

        IsMeshTranslated = false;
    }

    public void DrawFace(Face face, DrawingHandle3d handle)
    {
        for (int i = 1; i < face.Vertices.Length - 1; i++)
        {
            DrawPolygon(face, handle, 0, i, i + 1);
        }
    }

    public void PreparePolygon(Face face, int i1, int i2, int i3)
    {
        var vert1 = face.Vertices[i1];
        var vert2 = face.Vertices[i2];
        var vert3 = face.Vertices[i3];
        
        _currTriangle.Triangle.p1 = TranslatedVertexes[vert1.VertexId];
        _currTriangle.Triangle.p2 = TranslatedVertexes[vert2.VertexId];
        _currTriangle.Triangle.p3 = TranslatedVertexes[vert3.VertexId];
        
        if (face.HasTexturePos)
        {
            _currTriangle.TextureId = TextureBufferCoord + face.MaterialId;
            _currTriangle.TexturePoint1 = Mesh.TextureCoords[vert1.TexPosId - 1];
            _currTriangle.TexturePoint2 = Mesh.TextureCoords[vert2.TexPosId - 1];
            _currTriangle.TexturePoint3 = Mesh.TextureCoords[vert3.TexPosId - 1];
        }
    }

    private void DrawPolygon(Face face, DrawingHandle3d handle, int i1, int i2, int i3)
    {
        PreparePolygon(face, i1, i2, i3);
        handle.DrawPolygon(_currTriangle);
    }
}

public struct DrawParallel : IParallelRobustJob
{
    public int BatchSize => 16;
    public int MinimumBatchParallel => 8;

    private readonly MeshRender _meshRender;
    private readonly DrawingHandle3d _handle;

    public DrawParallel(MeshRender meshRender, DrawingHandle3d handle3d)
    {
        _meshRender = meshRender;
        _handle = handle3d;
    }

    public void Execute(int index)
    {
        DrawFace(_meshRender.Mesh.Faces[index]);
    }
    
    public void DrawFace(Face face)
    {
        for (int i = 1; i < face.Vertices.Length - 1; i++)
        {
            DrawPolygon(face, 0, i, i + 1);
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
        
        using var threadPoolItem = _handle.DrawingInstance.AsyncClippingInstances.Take();
        _handle.DrawPolygon(new TexturedTriangle(triangle, t1, t2, t3, matId), threadPoolItem.Value);
    }
}