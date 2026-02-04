namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class PlaneShape : IPhysicShape
{
    [DataField] public float Distance = 1f;
    [DataField] public Vector3 Normal = Vector3.UnitY;
    public float Area => 1f;
}