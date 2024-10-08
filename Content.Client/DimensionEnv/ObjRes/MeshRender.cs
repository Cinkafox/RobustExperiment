using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.Utils;
using Content.Client.Viewport;
using Robust.Client.Graphics;
using Vector3 = Robust.Shared.Maths.Vector3;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class MeshRender
{
    public Mesh Mesh;
    public Matrix4 Transform = Matrix4.Identity;
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
            _translatedVertexes[i] = Mesh.Vertexes[i];
        }

        IsMeshTranslated = true;
    }

    public void Draw(DrawingHandle3d handle)
    {
        foreach (var face in Mesh.Faces)
        {
            foreach (var t in face.Vertex)
            {
                if (t >= 1 && t <= TranslatedVertexes.Length) continue;
                Logger.Error($"Vertex index {t} is out of range!");
            }
            
            if (face.Vertex.Length == 3)
            {
                DrawPolygon(face, handle, 0, 1, 2);
            }
            else if (face.Vertex.Length > 3)
            {
                for (int i = 1; i < face.Vertex.Length - 1; i++)
                {
                    DrawPolygon(face, handle, 0, i, i + 1);
                }
            }
        }
        
        IsMeshTranslated = false;
    }

    private void DrawPolygon(FaceContent face,DrawingHandle3d handle, int i1, int i2, int i3)
    {
        v1 = TranslatedVertexes[face.Vertex[i1] - 1].ToVec4();
        v2 = TranslatedVertexes[face.Vertex[i2] - 1].ToVec4();
        v3 = TranslatedVertexes[face.Vertex[i3] - 1].ToVec4();

        triangle = new Triangle(v1, v2, v3);
        
        if (face.HasTexturePos)
        {
            matId = TextureBufferCoord + face.MaterialId;
            
            t1 = Mesh.TextureCoords[face.TexPos[i1] - 1];
            t2 = Mesh.TextureCoords[face.TexPos[i2] - 1];
            t3 = Mesh.TextureCoords[face.TexPos[i3] - 1];
        }
            
        handle.DrawPolygon(triangle, t1, t2, t3, matId);
    }
}