using System.IO;
using System.Linq;
using System.Numerics;
using Content.Client.DimensionEnv.ObjRes.Content;
using Content.Client.DimensionEnv.ObjRes.MTL;
using Content.Shared.Utils;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Content.Client.DimensionEnv.ObjRes;

public sealed class Mesh
{
    public readonly Vector4[] Vertexes;
    public readonly Vector3[] Normals;
    public readonly Face[] Faces;
    public readonly Vector2[] TextureCoords;
    public readonly Material[] Materials;

    public Mesh(Vector4[] vertexes, Vector3[] normals, Face[] faces, Vector2[] textureCoords, Material[] materials)
    {
        Vertexes = vertexes;
        Normals = normals;
        Faces = faces;
        TextureCoords = textureCoords;
        Materials = materials;
    }

    public Mesh()
    {
        Vertexes = [];
        Normals = [];
        Faces = [];
        TextureCoords = [];
        Materials = [];
    }

    public static Mesh Parse(IDependencyCollection dependencyCollection, TextReader textReader, ResPath path, Matrix4x4? matrix = null)
    {
        var parser = new Objparser(dependencyCollection, textReader, path);
        
        var currMaterialId = -1;
        Dictionary<string, Material> materialsDictionary = default!;
        
        List<Vector4> vertexes = [];
        List<Vector3> normals = [];
        List<Face> faces = [];
        List<Vector2> textureCoords = [];
        List<Material> materials = [];

        foreach (var content in parser.Contents)
        {
            switch (content)
            {
                case VertexContent vertexContent:
                    vertexes.Add(ShiftOrDefault(vertexContent.Vertex, matrix));
                    break;
                case FaceContent faceContent:
                    faceContent.Face.MaterialId = currMaterialId;
                    faces.Add(faceContent.Face);
                    break;
                case TexturePosContent texturePosContent:
                    textureCoords.Add(texturePosContent.TexturePos);
                    break;
                case NormalContent normalContent:
                    normals.Add(ShiftOrDefault(normalContent.Normal, matrix));
                    break;
                case MaterialContent materialContent:
                    materials.Add(materialsDictionary[materialContent.Material]);
                    currMaterialId++;
                    break;
                case MtlLoadContent mtlLoadContent:
                    materialsDictionary = mtlLoadContent.Materials;
                    break;
            }
        }
        
        return new Mesh(
            vertexes.ToArray(), 
            normals.ToArray(),
            faces.ToArray(),
            textureCoords.ToArray(), 
            materials.ToArray());
    }

    private static Vector4 ShiftOrDefault(Vector4 pos, Matrix4x4? matrix)
    {
        if (!matrix.HasValue) 
            return pos;
        return Vector4.Transform(pos, matrix.Value);
    }
    
    private static Vector3 ShiftOrDefault(Vector3 pos, Matrix4x4? matrix)
    {
        if (!matrix.HasValue) 
            return pos;
        return Vector3.Transform(pos, matrix.Value);
    }
}

[TypeSerializer]
public sealed class MeshSerializer : ITypeReader<Mesh, ValueDataNode>, ITypeReader<Mesh, MappingDataNode>, ITypeCopier<Mesh>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<ResPath>(node, context);
    }

    public Mesh Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Mesh>? instanceProvider = null)
    {
        var path = serializationManager.Read<ResPath>(node, hookCtx, context);
        var manager = dependencies.Resolve<IResourceManager>();

        using var reader = manager.ContentFileReadText(path);
        return Mesh.Parse(dependencies, reader, path.Directory);
    }
    
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedMappingNode(new Dictionary<ValidationNode, ValidationNode>()
        {
            { new ValidatedValueNode(new ValueDataNode("path")), serializationManager.ValidateNode<ResPath>(node.Get("path"), context) },
        });
    }

    public Mesh Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Mesh>? instanceProvider = null)
    {
        var path = serializationManager.Read<ResPath>(node.Get("path"), hookCtx, context);
        var offset = Vector3.Zero;
        var scale = Vector3.One;
        
        if(node.Has("offset"))
            offset = serializationManager.Read<Vector3>(node.Get("offset"), hookCtx, context);
        
        if (node.Has("scale"))
            scale = serializationManager.Read<Vector3>(node.Get("scale"), hookCtx, context);
        
        var matrix = Matrix4Helpers.CreateTransform(offset, Quaternion.Identity, scale);
        var manager = dependencies.Resolve<IResourceManager>();

        using var reader = manager.ContentFileReadText(path);
        return Mesh.Parse(dependencies, reader, path.Directory, matrix);
    }

    public void CopyTo(ISerializationManager serializationManager, Mesh source, ref Mesh target,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target = new Mesh(source.Vertexes, source.Normals, source.Faces, source.TextureCoords, source.Materials);
    }
}