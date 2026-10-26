using Content.Shared.Physics.Components;
using Content.Shared.Physics.Shapes;
using Content.Shared.Transform;

namespace Content.Shared.Physics.Systems;

public sealed partial class RigidBodySystem
{
    private BodyCacheEntry[] _bodyCache = Array.Empty<BodyCacheEntry>();
    private int _bodyCount;
    private SweepEntry[] _sweepEntries = Array.Empty<SweepEntry>();
    private readonly HashSet<EntityUid> _constraintBodies = new();

    private struct BodyCacheEntry
    {
        public EntityUid Uid;
        public RigidBodyComponent Body;
        public Transform3dComponent Xform;
        public bool Predicted;
        public bool Dynamic;
        public bool HasConstraints;
        public Type ShapeType;
        public float InvMass;
        public float InvInertia;
        public float Restitution;
        public float Friction;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Vector3 WorldScale;
        public Vector3 AabbExtents;
        public Vector3 AabbMin;
        public Vector3 AabbMax;
    }

    private struct SweepEntry : IComparable<SweepEntry>
    {
        public int BodyIndex;
        public float MinX;
        public EntityUid Uid;

        public int CompareTo(SweepEntry other)
        {
            var x = MinX.CompareTo(other.MinX);
            return x != 0 ? x : Uid.CompareTo(other.Uid);
        }
    }

    private void RebuildBodyCache()
    {
        RebuildConstraintBodies();

        _bodyCount = 0;
        var query = EntityQueryEnumerator<RigidBodyComponent, Transform3dComponent>();
        while (query.MoveNext(out var uid, out var body, out var xform))
        {
            EnsureBodyCapacity(_bodyCount + 1);

            ref var entry = ref _bodyCache[_bodyCount++];
            entry.Uid = uid;
            entry.Body = body;
            entry.Xform = xform;
            entry.Predicted = ShouldPredict(uid);
            entry.Dynamic = body.PhysType == PhysType.Dynamic;

            var mass = body.Mass;
            entry.InvMass = entry.Dynamic ? 1f / mass : 0f;
            entry.InvInertia = entry.Dynamic ? body.InvInertia : 0f;
            entry.Restitution = body.Restitution;
            entry.Friction = body.Friction;
            entry.HasConstraints = _constraintBodies.Contains(uid);
            entry.ShapeType = body.Shape.GetType();

            // Dynamic predicted bodies are refreshed again by UpdateBroadphase
            // before the first collision pass.
            if (!entry.Dynamic || !entry.Predicted)
                UpdateBodyWorldPose(ref entry);
        }

        EnsureSweepCapacity(_bodyCount);
    }

    private void RebuildConstraintBodies()
    {
        _constraintBodies.Clear();

        var query = AllEntityQuery<ConstraintComponent>();
        while (query.MoveNext(out var uid, out var constraints))
        {
            _constraintBodies.Add(uid);

            foreach (var (other, _) in constraints.Constraints)
            {
                _constraintBodies.Add(other);
            }
        }
    }

    private void UpdateBroadphase()
    {
        for (var i = 0; i < _bodyCount; i++)
        {
            ref var entry = ref _bodyCache[i];

            // Static and non-predicted bodies do not move during simulation;
            // RebuildBodyCache already refreshed their pose this tick.
            if (entry.Dynamic && entry.Predicted)
                UpdateBodyWorldPose(ref entry);

            _sweepEntries[i] = new SweepEntry
            {
                BodyIndex = i,
                MinX = entry.AabbMin.X,
                Uid = entry.Uid
            };
        }

        Array.Sort(_sweepEntries, 0, _bodyCount);
    }

    private static void UpdateBodyWorldPose(ref BodyCacheEntry entry)
    {
        entry.WorldPosition = entry.Xform.WorldPosition;
        entry.WorldRotation = entry.Xform.WorldRotation;
        entry.WorldScale = entry.Xform.WorldScale;
        entry.AabbExtents = GetAabbExtents(entry.Body.Shape, entry.WorldRotation, entry.WorldScale);
        entry.AabbMin = entry.WorldPosition - entry.AabbExtents;
        entry.AabbMax = entry.WorldPosition + entry.AabbExtents;
    }

    private static void UpdateBodyPosition(ref BodyCacheEntry entry)
    {
        entry.WorldPosition = entry.Xform.WorldPosition;
        entry.AabbMin = entry.WorldPosition - entry.AabbExtents;
        entry.AabbMax = entry.WorldPosition + entry.AabbExtents;
    }

    private static bool AabbOverlaps(in BodyCacheEntry a, in BodyCacheEntry b)
    {
        return a.AabbMax.X >= b.AabbMin.X &&
               b.AabbMax.X >= a.AabbMin.X &&
               a.AabbMax.Y >= b.AabbMin.Y &&
               b.AabbMax.Y >= a.AabbMin.Y &&
               a.AabbMax.Z >= b.AabbMin.Z &&
               b.AabbMax.Z >= a.AabbMin.Z;
    }

    private static Vector3 GetAabbExtents(IPhysicShape shape, Quaternion rotation, Vector3 scale)
    {
        var absX = MathF.Abs(scale.X);
        var absY = MathF.Abs(scale.Y);
        var absZ = MathF.Abs(scale.Z);

        return shape switch
        {
            SphereShape sphere => new Vector3(sphere.Radius * MathF.Max(absX, MathF.Max(absY, absZ))),
            BoxShape box => GetBoxAabbExtents(box, rotation, absX, absY, absZ),
            CylinderShape cylinder => GetCylinderAabbExtents(cylinder, rotation, absX, absY, absZ),
            PlaneShape => new Vector3(float.PositiveInfinity),
            _ => Vector3.Zero
        };
    }

    private static Vector3 GetBoxAabbExtents(BoxShape box, Quaternion rotation, float absX, float absY, float absZ)
    {
        var halfX = box.HalfExtents.X * absX;
        var halfY = box.HalfExtents.Y * absY;
        var halfZ = box.HalfExtents.Z * absZ;

        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        return new Vector3(
            MathF.Abs(axisX.X) * halfX + MathF.Abs(axisY.X) * halfY + MathF.Abs(axisZ.X) * halfZ,
            MathF.Abs(axisX.Y) * halfX + MathF.Abs(axisY.Y) * halfY + MathF.Abs(axisZ.Y) * halfZ,
            MathF.Abs(axisX.Z) * halfX + MathF.Abs(axisY.Z) * halfY + MathF.Abs(axisZ.Z) * halfZ);
    }

    private static Vector3 GetCylinderAabbExtents(
        CylinderShape cylinder,
        Quaternion rotation,
        float absX,
        float absY,
        float absZ)
    {
        var axis = Vector3.Transform(Vector3.UnitY, rotation);
        var radius = cylinder.Radius * MathF.Max(absX, absZ);
        var halfHeight = cylinder.HalfHeight * absY;

        return new Vector3(
            radius * MathF.Sqrt(MathF.Max(0f, 1f - axis.X * axis.X)) + halfHeight * MathF.Abs(axis.X),
            radius * MathF.Sqrt(MathF.Max(0f, 1f - axis.Y * axis.Y)) + halfHeight * MathF.Abs(axis.Y),
            radius * MathF.Sqrt(MathF.Max(0f, 1f - axis.Z * axis.Z)) + halfHeight * MathF.Abs(axis.Z));
    }

    private void EnsureBodyCapacity(int capacity)
    {
        if (capacity <= _bodyCache.Length)
            return;

        var newSize = Math.Max(capacity, Math.Max(16, _bodyCache.Length * 2));
        Array.Resize(ref _bodyCache, newSize);
    }

    private void EnsureSweepCapacity(int capacity)
    {
        if (capacity <= _sweepEntries.Length)
            return;

        var newSize = Math.Max(capacity, Math.Max(16, _sweepEntries.Length * 2));
        Array.Resize(ref _sweepEntries, newSize);
    }
}