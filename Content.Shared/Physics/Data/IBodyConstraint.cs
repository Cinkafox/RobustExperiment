using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;

namespace Content.Shared.Physics.Data;

[ImplicitDataDefinitionForInheritors]
public partial interface IBodyConstraint
{
    public void Prepare(RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyB, 
        float dt);
    public void Solve(RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyB, 
        float dt);
}

[DataDefinition]
public sealed partial class PointToPointConstraint : IBodyConstraint
{
    [DataField] public Vector3 LocalAnchorA;
    [DataField] public Vector3 LocalAnchorB;
    
    private Vector3 _rA, _rB;
    private Matrix3x3 _effectiveMassMatrix; 
    private Vector3 _bias;
    
    public void Prepare(
        RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyB, 
        float dt)
    {
        _rA = Vector3.Transform(LocalAnchorA, bodyA.Comp2.WorldRotation);
        _rB = Vector3.Transform(LocalAnchorB, bodyB.Comp2.WorldRotation);
        
        var worldA = bodyA.Comp2.WorldPosition + _rA;
        var worldB = bodyB.Comp2.WorldPosition + _rB;
        
        var C = worldB - worldA;
        var beta = 0.2f; // Baumgarte factor
        _bias = (beta / dt) * C;
        
        var K = Matrix3x3.Identity * (bodyA.Comp1.InvMass + (bodyB.Comp1.InvMass));

        K += Matrix3x3.SkewSymmetric(_rA) * bodyA.Comp1.WorldInvInertia
                                * Matrix3x3.Transpose(Matrix3x3.SkewSymmetric(_rA));

        K += Matrix3x3.SkewSymmetric(_rB) * bodyB.Comp1.WorldInvInertia
                                          * Matrix3x3.Transpose(Matrix3x3.SkewSymmetric(_rB));

        _effectiveMassMatrix = Matrix3x3.Invert(K);
    }

    public void Solve(
        RigidBodySystem system, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyA, 
        Entity<RigidBodyComponent, Transform3dComponent> bodyB, 
        float dt)
    {
        var velA = bodyA.Comp1.LinearVelocity + Vector3.Cross(bodyA.Comp1.AngularVelocity, _rA);
        var velB = bodyB.Comp1.LinearVelocity + Vector3.Cross(bodyB.Comp1.AngularVelocity, _rB);

        var Cdot = velB - velA;
        var impulse = Matrix3x3.Transform(-Cdot - _bias, _effectiveMassMatrix);

        bodyA.Comp1.LinearVelocity -= impulse * bodyA.Comp1.InvMass;
        bodyA.Comp1.AngularVelocity -= Matrix3x3.Transform(
            Vector3.Cross(_rA, impulse), bodyA.Comp1.WorldInvInertia);

        bodyB.Comp1.LinearVelocity += impulse * bodyB.Comp1.InvMass;
        bodyB.Comp1.AngularVelocity += Matrix3x3.Transform(
            Vector3.Cross(_rB, impulse), bodyB.Comp1.WorldInvInertia);
    }
}
