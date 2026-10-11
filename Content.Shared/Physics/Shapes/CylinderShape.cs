using Content.Shared.Physics.Data;

namespace Content.Shared.Physics.Shapes;

[DataDefinition]
public sealed partial class CylinderShape : IPhysicShape
{
    [DataField] public float Radius = 0.5f;
    [DataField] public float Height = 1f;

    [ViewVariables] public float HalfHeight => Height * 0.5f;

    public float Area => float.Pi * Radius * Radius * Height;

    public Vector3 CalculateLocalInertia(float mass)
    {
        var radiusSquared = Radius * Radius;
        var heightSquared = Height * Height;

        var side = (1f / 12f) * mass * (3f * radiusSquared + heightSquared);
        var axis = 0.5f * mass * radiusSquared;

        return new Vector3(side, axis, side);
    }

    public void DrawShape(DebugDrawingHandle handle, TransformedPhysicShape transformedPhysicShape)
    {
        const int segments = 24;

        var axis = transformedPhysicShape.RotateVector(Vector3.UnitY);
        axis = axis.LengthSquared() > 1e-8f ? Vector3.Normalize(axis) : Vector3.UnitY;

        var radius = Radius * MathF.Max(
            MathF.Abs(transformedPhysicShape.Scale.X),
            MathF.Abs(transformedPhysicShape.Scale.Z));

        var halfHeight = HalfHeight * MathF.Abs(transformedPhysicShape.Scale.Y);

        var top = transformedPhysicShape.Position + axis * halfHeight;
        var bottom = transformedPhysicShape.Position - axis * halfHeight;

        var reference = MathF.Abs(axis.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        var u = Vector3.Normalize(Vector3.Cross(axis, reference));
        var v = Vector3.Cross(axis, u);

        var firstTop = Vector3.Zero;
        var firstBottom = Vector3.Zero;
        var previousTop = Vector3.Zero;
        var previousBottom = Vector3.Zero;

        for (var i = 0; i < segments; i++)
        {
            var angle = MathF.Tau * i / segments;
            var offset = (u * MathF.Cos(angle) + v * MathF.Sin(angle)) * radius;
            var topPoint = top + offset;
            var bottomPoint = bottom + offset;

            handle.DrawLine(topPoint, bottomPoint);

            if (i == 0)
            {
                firstTop = topPoint;
                firstBottom = bottomPoint;
            }
            else
            {
                handle.DrawLine(previousTop, topPoint);
                handle.DrawLine(previousBottom, bottomPoint);
            }

            previousTop = topPoint;
            previousBottom = bottomPoint;
        }

        handle.DrawLine(previousTop, firstTop);
        handle.DrawLine(previousBottom, firstBottom);
    }
}
