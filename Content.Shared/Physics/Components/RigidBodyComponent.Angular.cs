namespace Content.Shared.Physics.Components;

public sealed partial class RigidBodyComponent
{
    [DataField] public bool EnableAngularVelocity = true;
    
    [DataField] public Vector3 AngularVelocity = Vector3.Zero;
    [ViewVariables(VVAccess.ReadOnly)] public Vector3 AngularForce => AngularVelocity * Mass;
    
    [DataField] public float InertiaScale = 0.7f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Inertia
    {
        get
        {
            if (PhysType != PhysType.Dynamic)
                return float.PositiveInfinity;

            return Shape switch
            {
                Shapes.SphereShape sphere =>
                    0.4f * Mass * sphere.Radius * sphere.Radius,

                Shapes.BoxShape box =>
                    (1f / 12f) * Mass *
                    (box.Size.X * box.Size.X +
                     box.Size.Y * box.Size.Y +
                     box.Size.Z * box.Size.Z),

                _ => Mass
            } * InertiaScale;
        }
    }

    [ViewVariables(VVAccess.ReadOnly)]
    public float InvInertia =>
        PhysType == PhysType.Dynamic && Inertia > 0f
            ? 1f / Inertia
            : 0f;
}