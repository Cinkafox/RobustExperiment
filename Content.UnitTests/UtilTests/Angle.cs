using System;
using System.Numerics;
using Content.Shared.Utils;
using NUnit.Framework;
using Quaternion = System.Numerics.Quaternion;

namespace Content.UnitTests.UtilTests;

public sealed class Angle
{
    
    [Test]
    public void TestQuaternionToEulerAnglesAndBack()
    {
        var originalQuaternion = new Quaternion(0.0f, 0.0f, 0.7071f, 0.7071f);
        
        var eulerAngles = originalQuaternion.ToEulerAngle();
        
        var resultQuaternion = eulerAngles.ToQuaternion();
        
        Assert.That(resultQuaternion.X, Is.EqualTo(originalQuaternion.X).Within(0.001f));
        Assert.That(resultQuaternion.Y, Is.EqualTo(originalQuaternion.Y).Within(0.001f));
        Assert.That(resultQuaternion.Z, Is.EqualTo(originalQuaternion.Z).Within(0.001f));
        Assert.That(resultQuaternion.W, Is.EqualTo(originalQuaternion.W).Within(0.001f));
    }

    [Test]
    public void TestEulerAnglesToQuaternionAndBack()
    {
        var originalPitch = Robust.Shared.Maths.Angle.FromDegrees(45.0f);
        var originalYaw =  Robust.Shared.Maths.Angle.FromDegrees(30.0f);
        var originalRoll =  Robust.Shared.Maths.Angle.FromDegrees(60.0f);

        var euler = new EulerAngles(originalPitch, originalYaw, originalRoll);
        
        var quaternion = euler.ToQuaternion();
        
        var eulerAngles = quaternion.ToEulerAngle();
        
        Assert.That(eulerAngles.Pitch.Degrees, Is.EqualTo(euler.Pitch.Degrees).Within(0.1f));
        Assert.That(eulerAngles.Yaw.Degrees, Is.EqualTo(euler.Yaw.Degrees).Within(0.1f));
        Assert.That(eulerAngles.Roll.Degrees, Is.EqualTo(euler.Roll.Degrees).Within(0.1f));
    }
    
    [Test]
    public void TestRotationMatrixFromQuaternionAndEulerAngles()
    {
        var eulerAngle = new EulerAngles(0, Robust.Shared.Maths.Angle.FromDegrees(90), 0);
        var quaternion = eulerAngle.ToQuaternion();
        
        var eulerMatrix = Matrix4Helpers.CreateRotation(eulerAngle);
        var quaternionMatrix = Matrix4Helpers.CreateRotation(quaternion);

        Console.WriteLine("QUATERNION MATRIX--");
        Console.WriteLine(quaternionMatrix.ToDebugString());
        Console.WriteLine("EULER MATRIX-------");
        Console.WriteLine(eulerMatrix.ToDebugString());
        
        Assert.That(eulerMatrix.EqualsApprox(quaternionMatrix, 0.1f));
    }

    [Test]
    public void TestVectorRotation()
    {
        var eulerAngle = new EulerAngles(-Robust.Shared.Maths.Angle.FromDegrees(25), Robust.Shared.Maths.Angle.FromDegrees(45), Robust.Shared.Maths.Angle.FromDegrees(35));
        var quaternion = eulerAngle.ToQuaternion();

        var vector = Vector3.UnitY;

        var rotatedAngle = Matrix4Helpers.TransformVector(vector, eulerAngle);
        var rotatedQuat = Matrix4Helpers.TransformVector(vector, quaternion);
        
        Console.WriteLine("ANGLE ROTATION " + rotatedAngle);
        Console.WriteLine("ANGLE QUATERNI " + rotatedQuat);
        
        Assert.That(rotatedAngle, Is.EqualTo(rotatedQuat));
    }
}