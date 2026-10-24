using Content.Shared.Physics.Components;
using Content.Shared.Physics.Systems;
using Content.Shared.Transform;
using Content.Shared.Utils;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics.Constraints;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HingeConstraint : IBodyConstraint
{
    [DataField] public Vector3 LocalAnchorA;
    [DataField] public Vector3 LocalAnchorB;
    [DataField] public Vector3 Axis = Vector3.UnitX;
    [DataField] public Vector3 AxisB = Vector3.UnitX;

    [NonSerialized] private const float Beta = 0.2f; // Baumgarte factor

    [NonSerialized] private Vector3 _rA, _rB;
    [NonSerialized] private Vector3 _u, _v;
    [NonSerialized] private Vector3 _worldA, _worldB;
    [NonSerialized] private Vector3 _axisA, _axisB;

    [NonSerialized] private readonly float[,] _invK = new float[5, 5];
    [NonSerialized] private readonly float[] _bias = new float[5];

    public void Prepare(
        RigidBodySystem system,
        Entity<RigidBodyComponent, Transform3dComponent> bodyA,
        Entity<RigidBodyComponent?, Transform3dComponent> bodyB,
        float dt)
    {
        if (bodyA.Comp1 == null || bodyA.Comp2 == null)
            return;
        if (bodyB.Comp2 == null)
            return;
        if (bodyA.Comp1.WorldInvInertia.Equals(Matrix3x3.Zero))
            bodyA.Comp1.UpdateWorldInertia(bodyA.Comp2.WorldRotation);
        if (bodyB.Comp1 != null && bodyB.Comp1.WorldInvInertia.Equals(Matrix3x3.Zero))
            bodyB.Comp1.UpdateWorldInertia(bodyB.Comp2.WorldRotation);

        if (dt <= 0f || _invK == null)
            return;

        var xformB = bodyB.Comp2;
        var rotB = xformB.WorldRotation;
        _rA = Vector3.Transform(LocalAnchorA, bodyA.Comp2.WorldRotation);
        _rB = Vector3.Transform(LocalAnchorB, rotB);

        _worldA = bodyA.Comp2.WorldPosition + _rA;
        _worldB = xformB.WorldPosition + _rB;

        var axisA = Vector3.Transform(Axis, bodyA.Comp2.WorldRotation);
        _axisA = axisA.LengthSquared() > 1e-6f ? Vector3.Normalize(axisA) : Vector3.UnitX;

        var axisB = Vector3.Transform(AxisB, rotB);
        _axisB = axisB.LengthSquared() > 1e-6f ? Vector3.Normalize(axisB) : _axisA;
        
        var reference = MathF.Abs(_axisA.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        _u = Vector3.Normalize(Vector3.Cross(_axisA, reference));
        _v = Vector3.Normalize(Vector3.Cross(_axisA, _u));

        var invMassSum = bodyA.Comp1.InvMass + (bodyB.Comp1?.InvMass ?? 0f);
        var iA = bodyA.Comp1.WorldInvInertia;
        var iB = bodyB.Comp1?.WorldInvInertia ?? Matrix3x3.Zero;

        var skewRA = Matrix3x3.SkewSymmetric(_rA);
        var skewRB = Matrix3x3.SkewSymmetric(_rB);
        
        var kLL = Matrix3x3.Identity * invMassSum +
                  skewRA * iA * Matrix3x3.Transpose(skewRA) +
                  skewRB * iB * Matrix3x3.Transpose(skewRB);
        
        var uTerm = Matrix3x3.Transform(_u, iA) + Matrix3x3.Transform(_u, iB);
        var vTerm = Matrix3x3.Transform(_v, iA) + Matrix3x3.Transform(_v, iB);
        var kRR00 = Vector3.Dot(_u, uTerm);
        var kRR01 = Vector3.Dot(_u, vTerm);
        var kRR10 = Vector3.Dot(_v, uTerm);
        var kRR11 = Vector3.Dot(_v, vTerm);
        
        var colU = -(Vector3.Cross(_rA, Matrix3x3.Transform(_u, iA)) +
                     Vector3.Cross(_rB, Matrix3x3.Transform(_u, iB)));
        var colV = -(Vector3.Cross(_rA, Matrix3x3.Transform(_v, iA)) +
                     Vector3.Cross(_rB, Matrix3x3.Transform(_v, iB)));

        var k = new float[5, 5];
        k[0, 0] = kLL.M11; k[0, 1] = kLL.M12; k[0, 2] = kLL.M13;
        k[1, 0] = kLL.M21; k[1, 1] = kLL.M22; k[1, 2] = kLL.M23;
        k[2, 0] = kLL.M31; k[2, 1] = kLL.M32; k[2, 2] = kLL.M33;
        k[0, 3] = k[3, 0] = colU.X;
        k[1, 3] = k[3, 1] = colU.Y;
        k[2, 3] = k[3, 2] = colU.Z;
        k[0, 4] = k[4, 0] = colV.X;
        k[1, 4] = k[4, 1] = colV.Y;
        k[2, 4] = k[4, 2] = colV.Z;
        k[3, 3] = kRR00; k[3, 4] = kRR01;
        k[4, 3] = kRR10; k[4, 4] = kRR11;

        if (_invK == null || !Invert5x5(k, _invK))
        {
            if (_invK != null)
            {
                for (var i = 0; i < 5; i++)
                for (var j = 0; j < 5; j++)
                    _invK[i, j] = 0f;
            }
        }

        var betaDt = Beta / dt;
        var c = _worldB - _worldA;
        _bias[0] = betaDt * c.X;
        _bias[1] = betaDt * c.Y;
        _bias[2] = betaDt * c.Z;

        var error = Vector3.Cross(_axisA, _axisB);
        _bias[3] = betaDt * Vector3.Dot(error, _u);
        _bias[4] = betaDt * Vector3.Dot(error, _v);
    }

    public void Solve(
        RigidBodySystem system,
        Entity<RigidBodyComponent, Transform3dComponent> bodyA,
        Entity<RigidBodyComponent?, Transform3dComponent> bodyB,
        float dt)
    {
        if (_invK == null)
            return;
        if (_invK[0,0] == 0f && _invK[1,1] == 0f && _invK[2,2] == 0f)
            return;

        var vA = bodyA.Comp1.LinearVelocity;
        var wA = bodyA.Comp1.AngularVelocity;
        var vB = bodyB.Comp1?.LinearVelocity ?? Vector3.Zero;
        var wB = bodyB.Comp1?.AngularVelocity ?? Vector3.Zero;

        var cdotLin = (vB + Vector3.Cross(wB, _rB)) - (vA + Vector3.Cross(wA, _rA));
        var wRel = wB - wA;

        var rhs = new float[5];
        rhs[0] = -cdotLin.X - _bias[0];
        rhs[1] = -cdotLin.Y - _bias[1];
        rhs[2] = -cdotLin.Z - _bias[2];
        rhs[3] = -Vector3.Dot(wRel, _u) - _bias[3];
        rhs[4] = -Vector3.Dot(wRel, _v) - _bias[4];

        var lambda = new float[5];
        for (var i = 0; i < 5; i++)
        {
            var sum = 0f;
            for (var j = 0; j < 5; j++)
                sum += _invK[i, j] * rhs[j];
            lambda[i] = sum;
        }

        var lambdaLin = new Vector3(lambda[0], lambda[1], lambda[2]);
        var angImpulse = _u * lambda[3] + _v * lambda[4];

        bodyA.Comp1.LinearVelocity -= lambdaLin * bodyA.Comp1.InvMass;
        bodyA.Comp1.AngularVelocity -= Matrix3x3.Transform(Vector3.Cross(_rA, lambdaLin), bodyA.Comp1.WorldInvInertia);
        bodyA.Comp1.AngularVelocity -= Matrix3x3.Transform(angImpulse, bodyA.Comp1.WorldInvInertia);

        if (bodyB.Comp1 is not null)
        {
            bodyB.Comp1.LinearVelocity += lambdaLin * bodyB.Comp1.InvMass;
            bodyB.Comp1.AngularVelocity += Matrix3x3.Transform(Vector3.Cross(_rB, lambdaLin), bodyB.Comp1.WorldInvInertia);
            bodyB.Comp1.AngularVelocity += Matrix3x3.Transform(angImpulse, bodyB.Comp1.WorldInvInertia);
        }
    }

    public void DrawDebug(DebugDrawingHandle handle)
    {
        handle.DrawSphere(_worldA, LocalAnchorA.LengthSquared() / 4);
        handle.DrawSphere(_worldB, LocalAnchorB.LengthSquared() / 4);
        handle.DrawLine(_worldA, _worldA + _axisA);
        handle.DrawLine(_worldB, _worldB + _axisB);
    }

    private static bool Invert5x5(float[,] m, float[,] inv)
    {
        if (m == null || inv == null)
            return false;

        const int n = 5;
        var a = new float[n, 2 * n];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
                a[i, j] = m[i, j];
            a[i, n + i] = 1f;
        }

        for (var col = 0; col < n; col++)
        {
            var pivot = col;
            var best = MathF.Abs(a[col, col]);
            for (var r = col + 1; r < n; r++)
            {
                var v = MathF.Abs(a[r, col]);
                if (v > best)
                {
                    best = v;
                    pivot = r;
                }
            }

            if (best < 1e-9f)
                return false;

            if (pivot != col)
                for (var k = 0; k < 2 * n; k++)
                    (a[col, k], a[pivot, k]) = (a[pivot, k], a[col, k]);

            var diag = a[col, col];
            for (var k = 0; k < 2 * n; k++)
                a[col, k] /= diag;

            for (var r = 0; r < n; r++)
            {
                if (r == col)
                    continue;
                var factor = a[r, col];
                if (factor == 0f)
                    continue;
                for (var k = 0; k < 2 * n; k++)
                    a[r, k] -= factor * a[col, k];
            }
        }

        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
            inv[i, j] = a[i, n + j];

        return true;
    }
}
