namespace Content.Shared.Physics.Shapes;

[ImplicitDataDefinitionForInheritors]
public partial interface IPhysicShape
{
    [ViewVariables(VVAccess.ReadOnly)] public float Area { get; }
}