using System.Linq;
using Content.Shared.Animations.Data;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Animations;

[TypeSerializer]
public sealed class BodyAnimationKeyContainerSerializer : 
    ITypeReader<BodyAnimationKeyContainer, MappingDataNode>, 
    ITypeReader<BodyAnimationKeyContainer, SequenceDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }
    
    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return new ValidatedValueNode(node);
    }

    public BodyAnimationKeyContainer Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<BodyAnimationKeyContainer>? instanceProvider = null)
    {
        var propertyType = serializationManager.Read<Type?>(node["type"]) ?? typeof(Vector3);

        var sequenceNode = (node["keyFrames"] as SequenceDataNode)!;
        
        return new BodyAnimationKeyContainer()
        {
            KeyFrames = ReadKeys(serializationManager, sequenceNode, propertyType),
        };
    }
    
    public BodyAnimationKeyContainer Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, ISerializationManager.InstantiationDelegate<BodyAnimationKeyContainer>? instanceProvider = null)
    {
        return new BodyAnimationKeyContainer()
        {
            KeyFrames = ReadKeys(serializationManager, node, typeof(Vector3)),
        };
    }

    private List<BodyAnimationKey> ReadKeys(ISerializationManager serializationManager, SequenceDataNode node,
        Type valueType)
    {
        return node
            .Select(node1 => (node1 as MappingDataNode)!)
            .Select(node1 => new BodyAnimationKey()
            {
                KeyTime = serializationManager.Read<float>(node1["keyTime"]),
                Value = serializationManager.Read(valueType, node1["value"], notNullableOverride: true)!
            })
            .ToList();
    }
}