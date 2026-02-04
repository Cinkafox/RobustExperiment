namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class SphereShape : IPhysicShape
{
    [DataField] public float Radius = 1f;
    public float Area => 4 * float.Pi * Radius * Radius;
}