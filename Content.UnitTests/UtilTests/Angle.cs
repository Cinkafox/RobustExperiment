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
        // Исходные кватернион и углы Эйлера
        var quaternion = new Quaternion(0.0f, 0.0f, 0.7071f, 0.7071f);
        var euler = quaternion.ToEulerAngle();

        // Генерация матриц поворота
        var matrixFromQuaternion = Matrix4Helpers.CreateRotation(quaternion);
        var matrixFromEulerAngles = Matrix4Helpers.CreateRotation(euler);
        
        Assert.That(matrixFromQuaternion.EqualsApprox(matrixFromEulerAngles, 0.001f));
    }
}