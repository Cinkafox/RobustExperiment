﻿using Content.Shared.IoC;
using Content.Shared.Transform;

namespace Content.Client.Camera;

[IoCRegister]
public sealed class CameraManager
{
    public (Transform3dComponent, CameraComponent)? Camera;
    
    public CameraProperties? CameraProperties {get {
        if (Camera is null) return null;
        return new CameraProperties(Camera.Value.Item1.LocalPosition, Camera.Value.Item1.LocalRotation,
            Camera.Value.Item2.FoV);
    }}
}