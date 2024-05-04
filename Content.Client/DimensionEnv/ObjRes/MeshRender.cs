using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.Utils;
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
        _translatedVertexes = new Vector3[mesh.Vertexes.Count];
    }

    private Vector4 v1;
    private Vector4 v2;
    private Vector4 v3;

    private Vector2 t1;
    private Vector2 t2;
    private Vector2 t3;

    private int matId;
    private Triangle triangle;

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
            foreach (var t in face.Vertices)
            {
                if (t.VertexId >= 1 && t.VertexId <= TranslatedVertexes.Length) continue;
                Logger.Error($"Vertex index {t} is out of range!");
            }

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
        
        IsMeshTranslated = false;
    }

    private void DrawPolygon(Face face,DrawingHandle3d handle, int i1, int i2, int i3)
    {
        var vert1 = face.Vertices[i1];
        var vert2 = face.Vertices[i2];
        var vert3 = face.Vertices[i3];
        
        v1 = TranslatedVertexes[vert1.VertexId - 1].ToVec4();
        v2 = TranslatedVertexes[vert2.VertexId - 1].ToVec4();
        v3 = TranslatedVertexes[vert3.VertexId - 1].ToVec4();

        triangle = new Triangle(v1, v2, v3);
        
        if (face.HasTexturePos)
        {
            matId = TextureBufferCoord + face.MaterialId;
            
            t1 = Mesh.TextureCoords[vert1.TexPosId - 1];
            t2 = Mesh.TextureCoords[vert2.TexPosId - 1];
            t3 = Mesh.TextureCoords[vert3.TexPosId - 1];
        }
            
        handle.DrawPolygon(triangle, t1, t2, t3, matId);
    }
}