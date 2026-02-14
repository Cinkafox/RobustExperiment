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
    public List<Vector4> Vertexes = new();
    public List<Vector3> Normals = new();
    public List<Face> Faces = new();
    public List<Vector2> TextureCoords = new();
    public List<Material> Materials = new();

    public static Mesh Parse(IDependencyCollection dependencyCollection, TextReader textReader, ResPath path, Matrix4x4? matrix = null)
    {
        var mesh = new Mesh();
        var parser = new Objparser(dependencyCollection, textReader, path);
        
        var currMaterialId = -1;
        Dictionary<string, Material> materials = default!;

        foreach (var content in parser.Contents)
        {
            switch (content)
            {
                case VertexContent vertexContent:
                    mesh.Vertexes.Add(ShiftOrDefault(vertexContent.Vertex, matrix));
                    break;
                case FaceContent faceContent:
                    faceContent.Face.MaterialId = currMaterialId;
                    mesh.Faces.Add(faceContent.Face);
                    break;
                case TexturePosContent texturePosContent:
                    mesh.TextureCoords.Add(texturePosContent.TexturePos);
                    break;
                case NormalContent normalContent:
                    mesh.Normals.Add(ShiftOrDefault(normalContent.Normal, matrix));
                    break;
                case MaterialContent materialContent:
                    mesh.Materials.Add(materials[materialContent.Material]);
                    currMaterialId++;
                    break;
                case MtlLoadContent mtlLoadContent:
                    materials = mtlLoadContent.Materials;
                    break;
            }
        }
        
        return mesh;
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
            { new ValidatedValueNode(new ValueDataNode("offset")), serializationManager.ValidateNode<Vector3>(node.Get("offset"), context) }
        });
    }

    public Mesh Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<Mesh>? instanceProvider = null)
    {
        var path = serializationManager.Read<ResPath>(node.Get("path"), hookCtx, context);
        var offset = serializationManager.Read<Vector3>(node.Get("offset"), hookCtx, context);
        
        var matrix = Matrix4Helpers.CreateTranslation(offset);
        var manager = dependencies.Resolve<IResourceManager>();

        using var reader = manager.ContentFileReadText(path);
        return Mesh.Parse(dependencies, reader, path.Directory, matrix);
    }

    public void CopyTo(ISerializationManager serializationManager, Mesh source, ref Mesh target,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target.Materials = source.Materials.ToList();
        target.Normals = source.Normals.ToList();
        target.TextureCoords = source.TextureCoords.ToList();
        target.Faces = source.Faces.ToList();
        target.Vertexes = source.Vertexes.ToList();
    }
}