using Robust.Shared.Prototypes;

namespace Content.Shared.Car;

[Prototype("carList")]
public sealed class CarListPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public List<EntProtoId> CarPrototypes = new();
}