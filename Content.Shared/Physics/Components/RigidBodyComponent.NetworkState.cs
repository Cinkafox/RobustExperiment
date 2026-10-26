using System;
using System.Numerics;

namespace Content.Shared.Physics.Components;

public sealed partial class RigidBodyComponent
{
    [NonSerialized] internal Vector3 LastNetLinearVelocity;
    [NonSerialized] internal Vector3 LastNetAngularVelocity;
    [NonSerialized] internal Vector3 LastNetPosition;
    [NonSerialized] internal Quaternion LastNetRotation;
    [NonSerialized] internal bool HasNetSnapshot;
    [NonSerialized] internal bool WasPredicted;
}
