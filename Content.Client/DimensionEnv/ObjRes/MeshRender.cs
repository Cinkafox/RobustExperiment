using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.Viewport;

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
        _translatedVertexes = new Vector4[mesh.Vertexes.Count];
    }

    private readonly Vector4[] _translatedVertexes;

    public Vector4[] TranslatedVertexes
    {
        get
        {
            if(!IsMeshTranslated) TranslateMesh();
            return _translatedVertexes;
        }
    }

    private void TranslateMesh()
    {
        for (var i = 0; i < Mesh.Vertexes.Count; i++)
        {
            _translatedVertexes[i] = Vector4.Transform(Mesh.Vertexes[i], Transform);
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

    private void DrawFace(Face face, DrawingHandle3d handle)
    {
        for (int i = 1; i < face.Vertices.Length - 1; i++)
        {
            DrawPolygon(face, handle, 0, i, i + 1);
        }
    }

    private void DrawPolygon(Face face, DrawingHandle3d handle, int i1, int i2, int i3)
    {
        var vert1 = face.Vertices[i1];
        var vert2 = face.Vertices[i2];
        var vert3 = face.Vertices[i3];

        var currTriangle = handle.DrawingInstance.TriangleBuffer.Take();

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
        
        handle.DrawPolygon(currTriangle);
    }
}
