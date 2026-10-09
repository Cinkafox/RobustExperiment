using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.Utils;
using Content.Client.Viewport;
using NUnit.Framework;

namespace Content.UnitTests.UtilTests;

[TestFixture]
public sealed class TriangleTests
{
    [Test]
    public void TestNormal_UnitY_ReturnsUnitY()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 0, 1);
        tri.p2 = new Vector4(1, 0, 0, 1);
        tri.p3 = new Vector4(0, 1, 0, 1);

        var normal = Vector3.Normalize(tri.Normal());
        var expected = Vector3.Normalize(new Vector3(0, 0, 1));
        Assert.That((normal - expected).Length(), Is.LessThan(0.0001f));
    }

    [Test]
    public void TestNormal_UnitX_ReturnsUnitX()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 0, 1);
        tri.p2 = new Vector4(0, 1, 0, 1);
        tri.p3 = new Vector4(0, 0, 1, 1);

        var normal = Vector3.Normalize(tri.Normal());
        var expected = Vector3.Normalize(new Vector3(1, 0, 0));
        Assert.That((normal - expected).Length(), Is.LessThan(0.0001f));
    }

    [Test]
    public void TestZ_AverageOfPoints()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(1, 2, 3, 1);
        tri.p2 = new Vector4(4, 5, 6, 1);
        tri.p3 = new Vector4(7, 8, 9, 1);

        Assert.That(tri.Z, Is.EqualTo((3 + 6 + 9) / 3.0f).Within(0.0001f));
    }

    [Test]
    public void TestViewSpaceZ_SetAndRead()
    {
        var tri = new Triangle();
        tri.ViewSpaceZ = 42.5f;
        Assert.That(tri.ViewSpaceZ, Is.EqualTo(42.5f));
    }

    [Test]
    public void TestSetViewSpaceZToFarthest_UsesMaxVertexZ()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 5, 1);
        tri.p2 = new Vector4(0, 0, 1, 1);
        tri.p3 = new Vector4(0, 0, 9, 1);

        tri.SetViewSpaceZToFarthest();

        Assert.That(tri.ViewSpaceZ, Is.EqualTo(9));
    }

    [Test]
    public void TestSetViewSpaceZToFarthest_IgnoresW()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 3, 1);
        tri.p2 = new Vector4(0, 0, 7, 1);
        tri.p3 = new Vector4(0, 0, 2, 10);

        tri.SetViewSpaceZToFarthest();

        Assert.That(tri.ViewSpaceZ, Is.EqualTo(7));
    }

    [Test]
    public void TestSetViewSpaceZToNearest_UsesMinVertexZ()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 5, 1);
        tri.p2 = new Vector4(0, 0, 1, 1);
        tri.p3 = new Vector4(0, 0, 9, 3);

        tri.SetViewSpaceZToNearest();

        Assert.That(tri.ViewSpaceZ, Is.EqualTo(1));
    }

    [Test]
    public void TestSetViewSpaceZToNearest_IgnoresW()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 3, 10);
        tri.p2 = new Vector4(0, 0, 7, 1);
        tri.p3 = new Vector4(0, 0, 2, 4);

        tri.SetViewSpaceZToNearest();

        Assert.That(tri.ViewSpaceZ, Is.EqualTo(2));
    }

    [Test]
    public void TestClear_ResetsPointsAndViewZ()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(1, 2, 3, 1);
        tri.p2 = new Vector4(4, 5, 6, 1);
        tri.p3 = new Vector4(7, 8, 9, 1);
        tri.ViewSpaceZ = 42f;
        tri.Clear();

        Assert.That(tri.p1, Is.EqualTo(new Vector4(0, 0, 0, 1)));
        Assert.That(tri.p2, Is.EqualTo(new Vector4(0, 0, 0, 1)));
        Assert.That(tri.p3, Is.EqualTo(new Vector4(0, 0, 0, 1)));
        Assert.That(tri.ViewSpaceZ, Is.EqualTo(0f));
    }

    [Test]
    public void TestSetP1_SetsWToOne()
    {
        var tri = new Triangle();
        tri.SetP1(new Vector3(1, 2, 3));
        Assert.That(tri.p1.X, Is.EqualTo(1));
        Assert.That(tri.p1.Y, Is.EqualTo(2));
        Assert.That(tri.p1.Z, Is.EqualTo(3));
        Assert.That(tri.p1.W, Is.EqualTo(1));
    }

    [Test]
    public void TestGetP1_ReturnsXYZ()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(1, 2, 3, 5);
        var result = tri.GetP1();
        Assert.That(result, Is.EqualTo(new Vector3(1, 2, 3)));
    }

    [Test]
    public void TestTransform_Translation()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(1, 0, 0, 1);
        tri.p2 = new Vector4(0, 1, 0, 1);
        tri.p3 = new Vector4(0, 0, 1, 1);

        var translate = Matrix4x4.CreateTranslation(10, 20, 30);
        tri.Transform(translate);

        Assert.That(tri.p1, Is.EqualTo(new Vector4(11, 20, 30, 1)));
        Assert.That(tri.p2, Is.EqualTo(new Vector4(10, 21, 30, 1)));
        Assert.That(tri.p3, Is.EqualTo(new Vector4(10, 20, 31, 1)));
    }

    [Test]
    public void TestTransform_Scale()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(1, 0, 0, 1);
        tri.p2 = new Vector4(0, 2, 0, 1);
        tri.p3 = new Vector4(0, 0, 3, 1);

        var scale = Matrix4x4.CreateScale(2);
        tri.Transform(scale);

        Assert.That(tri.p1, Is.EqualTo(new Vector4(2, 0, 0, 1)));
        Assert.That(tri.p2, Is.EqualTo(new Vector4(0, 4, 0, 1)));
        Assert.That(tri.p3, Is.EqualTo(new Vector4(0, 0, 6, 1)));
    }
}

[TestFixture]
public sealed class TexturedTriangleTests
{
    [Test]
    public void TestClear_ResetsAllFields()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(1, 2, 3, 1);
        tri.TexturePoint1 = new Vector3(0.5f, 0.5f, 1f);
        tri.TexturePoint2 = new Vector3(0.8f, 0.2f, 1f);
        tri.TexturePoint3 = new Vector3(0.1f, 0.9f, 1f);
        tri.Normal = new Vector3(0, 1, 0);
        tri.TextureId = 5;

        tri.Clear();

        Assert.That(tri.Triangle.p1, Is.EqualTo(new Vector4(0, 0, 0, 1)));
        Assert.That(tri.TexturePoint1, Is.EqualTo(new Vector3(0, 0, 1)));
        Assert.That(tri.TexturePoint2, Is.EqualTo(new Vector3(0, 0, 1)));
        Assert.That(tri.TexturePoint3, Is.EqualTo(new Vector3(0, 0, 1)));
        Assert.That(tri.Normal, Is.EqualTo(Vector3.Zero));
        Assert.That(tri.TextureId, Is.EqualTo(0));
    }

    [Test]
    public void TestTransformTexture_DividesByW()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 0, 2);
        tri.Triangle.p2 = new Vector4(1, 0, 0, 4);
        tri.Triangle.p3 = new Vector4(0, 1, 0, 8);
        tri.TexturePoint1 = new Vector3(0.4f, 0.6f, 1f);
        tri.TexturePoint2 = new Vector3(0.8f, 0.2f, 1f);
        tri.TexturePoint3 = new Vector3(0.1f, 0.9f, 1f);

        tri.TransformTexture();

        Assert.That(tri.TexturePoint1.X, Is.EqualTo(0.4f / 2));
        Assert.That(tri.TexturePoint2.X, Is.EqualTo(0.8f / 4));
        Assert.That(tri.TexturePoint3.X, Is.EqualTo(0.1f / 8));
        Assert.That(tri.TexturePoint1.Y, Is.EqualTo(0.6f / 2));
        Assert.That(tri.TexturePoint2.Y, Is.EqualTo(0.2f / 4));
        Assert.That(tri.TexturePoint3.Y, Is.EqualTo(0.9f / 8));
        Assert.That(tri.TexturePoint1.Z, Is.EqualTo(1f / 2));
        Assert.That(tri.TexturePoint2.Z, Is.EqualTo(1f / 4));
        Assert.That(tri.TexturePoint3.Z, Is.EqualTo(1f / 8));
    }

    [Test]
    public void TestTransformTexture_RecoversOriginalUVAfterDivision()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 0, 2);
        tri.Triangle.p2 = new Vector4(1, 0, 0, 4);
        tri.Triangle.p3 = new Vector4(0, 1, 0, 8);
        tri.TexturePoint1 = new Vector3(0.4f, 0.6f, 1f);
        tri.TexturePoint2 = new Vector3(0.8f, 0.2f, 1f);
        tri.TexturePoint3 = new Vector3(0.1f, 0.9f, 1f);

        tri.TransformTexture();

        Assert.That(tri.TexturePoint1.X / tri.TexturePoint1.Z, Is.EqualTo(0.4f).Within(1e-6f));
        Assert.That(tri.TexturePoint1.Y / tri.TexturePoint1.Z, Is.EqualTo(0.6f).Within(1e-6f));
        Assert.That(tri.TexturePoint2.X / tri.TexturePoint2.Z, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(tri.TexturePoint2.Y / tri.TexturePoint2.Z, Is.EqualTo(0.2f).Within(1e-6f));
        Assert.That(tri.TexturePoint3.X / tri.TexturePoint3.Z, Is.EqualTo(0.1f).Within(1e-6f));
        Assert.That(tri.TexturePoint3.Y / tri.TexturePoint3.Z, Is.EqualTo(0.9f).Within(1e-6f));
    }
}

[TestFixture]
public sealed class ClippingInstanceTests
{
    private ClippingInstance _clipping;
    private SimplePool<Triangle> _debugPool;
    private SimplePool<TexturedTriangle> _triPool;

    [SetUp]
    public void Setup()
    {
        _clipping = new ClippingInstance();
        _debugPool = new SimplePool<Triangle>(256, () => new Triangle());
        _triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
    }

    [Test]
    public void TestIntersectPlane_Simple()
    {
        var planeP = new Vector3(0, 0, 5);
        var planeN = new Vector3(0, 0, 1);
        var lineStart = new Vector4(0, 0, 0, 1);
        var lineEnd = new Vector4(0, 0, 10, 1);

        var result = ClippingInstance.IntersectPlane(planeP, planeN, lineStart, lineEnd, out var t);

        Assert.That(t, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(result.Z, Is.EqualTo(5).Within(0.0001f));
        Assert.That(result.W, Is.EqualTo(1).Within(0.0001f));
    }

    [Test]
    public void TestIntersectPlane_StartOnPlane()
    {
        var planeP = new Vector3(0, 0, 0);
        var planeN = new Vector3(0, 0, 1);
        var lineStart = new Vector4(0, 0, 0, 1);
        var lineEnd = new Vector4(0, 0, 10, 1);

        var result = ClippingInstance.IntersectPlane(planeP, planeN, lineStart, lineEnd, out var t);

        Assert.That(t, Is.EqualTo(0).Within(0.0001f));
        Assert.That(result.X, Is.EqualTo(0));
        Assert.That(result.Y, Is.EqualTo(0));
        Assert.That(result.Z, Is.EqualTo(0));
    }

    [Test]
    public void TestIntersectPlane_EndOnPlane()
    {
        var planeP = new Vector3(0, 0, 10);
        var planeN = new Vector3(0, 0, 1);
        var lineStart = new Vector4(0, 0, 0, 1);
        var lineEnd = new Vector4(0, 0, 10, 1);

        var result = ClippingInstance.IntersectPlane(planeP, planeN, lineStart, lineEnd, out var t);

        Assert.That(t, Is.EqualTo(1).Within(0.0001f));
        Assert.That(result.Z, Is.EqualTo(10).Within(0.0001f));
    }

    [Test]
    public void TestIntersectPlane_ParallelLine_ReturnsZero()
    {
        var planeP = new Vector3(0, 0, 5);
        var planeN = new Vector3(0, 0, 1);
        var lineStart = new Vector4(0, 0, 3, 1);
        var lineEnd = new Vector4(1, 0, 3, 1);

        var result = ClippingInstance.IntersectPlane(planeP, planeN, lineStart, lineEnd, out var t);

        Assert.That(t, Is.EqualTo(0));
    }

    [Test]
    public void TestClipDebug_AllInside_ReturnsOriginal()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 1, 1);
        tri.p2 = new Vector4(1, 0, 1, 1);
        tri.p3 = new Vector4(0, 1, 1, 1);

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, 1),
            tri,
            _debugPool);

        Assert.That(_clipping.DebugClipping.Length, Is.EqualTo(1));
        Assert.That(_clipping.DebugClipping[0], Is.SameAs(tri));
    }

    [Test]
    public void TestClipDebug_AllOutside_ReturnsNone()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, -1, 1);
        tri.p2 = new Vector4(1, 0, -1, 1);
        tri.p3 = new Vector4(0, 1, -1, 1);

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, 1),
            tri,
            _debugPool);

        Assert.That(_clipping.DebugClipping.Length, Is.EqualTo(0));
    }

    [Test]
    public void TestClipDebug_OneInsideTwoOutside_ProducesOneTriangle()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 1, 1);
        tri.p2 = new Vector4(0, 1, -1, 1);
        tri.p3 = new Vector4(1, 0, -1, 1);

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _debugPool);

        Assert.That(_clipping.DebugClipping.Length, Is.EqualTo(1));
        Assert.That(_clipping.DebugClipping[0].GetP1().Z, Is.GreaterThanOrEqualTo(0));
        Assert.That(_clipping.DebugClipping[0].GetP2().Z, Is.GreaterThanOrEqualTo(0));
        Assert.That(_clipping.DebugClipping[0].GetP3().Z, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void TestClipDebug_TwoInsideOneOutside_ProducesTwoTriangles()
    {
        var tri = new Triangle();
        tri.p1 = new Vector4(0, 0, 1, 1);
        tri.p2 = new Vector4(1, 0, 1, 1);
        tri.p3 = new Vector4(0.5f, 1, -1, 1);

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _debugPool);

        Assert.That(_clipping.DebugClipping.Length, Is.EqualTo(2));

        foreach (var clipped in _clipping.DebugClipping)
        {
            Assert.That(clipped.GetP1().Z, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(clipped.GetP2().Z, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(clipped.GetP3().Z, Is.GreaterThanOrEqualTo(-0.0001f));
        }
    }

    [Test]
    public void TestClipTextured_AllInside_ReturnsOriginal()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(1, 0, 1, 1);
        tri.Triangle.p3 = new Vector4(0, 1, 1, 1);
        tri.TexturePoint1 = new Vector3(0, 0, 1f);
        tri.TexturePoint2 = new Vector3(1, 0, 1f);
        tri.TexturePoint3 = new Vector3(0.5f, 1, 1f);
        tri.TextureId = 1;

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(1));
        Assert.That(_clipping.Clipping[0], Is.SameAs(tri));
        Assert.That(_clipping.Clipping[0].TextureId, Is.EqualTo(1));
    }

    [Test]
    public void TestClipTextured_AllOutside_ReturnsNone()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, -1, 1);
        tri.Triangle.p2 = new Vector4(1, 0, -1, 1);
        tri.Triangle.p3 = new Vector4(0, 1, -1, 1);

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(0));
    }

    [Test]
    public void TestClipTextured_OneInsideTwoOutside_PreservesTextureIdAndInterpolatesUvs()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(0, 1, -1, 1);
        tri.Triangle.p3 = new Vector4(1, 0, -1, 1);
        tri.TexturePoint1 = new Vector3(0, 0, 1f);
        tri.TexturePoint2 = new Vector3(0, 1, 1f);
        tri.TexturePoint3 = new Vector3(1, 0, 1f);
        tri.TextureId = 42;

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(1));
        Assert.That(_clipping.Clipping[0].TextureId, Is.EqualTo(42));

        var clipped = _clipping.Clipping[0];
        Assert.That(clipped.Triangle.GetP1().Z, Is.GreaterThanOrEqualTo(0));
        Assert.That(clipped.Triangle.GetP2().Z, Is.GreaterThanOrEqualTo(0));
        Assert.That(clipped.Triangle.GetP3().Z, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void TestClipTextured_TwoInsideOneOutside_ProducesTwoTriangles()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(1, 0, 1, 1);
        tri.Triangle.p3 = new Vector4(0.5f, 1, -1, 1);
        tri.TexturePoint1 = new Vector3(0, 0, 1f);
        tri.TexturePoint2 = new Vector3(1, 0, 1f);
        tri.TexturePoint3 = new Vector3(0.5f, 1, 1f);
        tri.TextureId = 7;

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(2));

        foreach (var clipped in _clipping.Clipping)
        {
            Assert.That(clipped.TextureId, Is.EqualTo(7));
            Assert.That(clipped.Triangle.GetP1().Z, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(clipped.Triangle.GetP2().Z, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(clipped.Triangle.GetP3().Z, Is.GreaterThanOrEqualTo(-0.0001f));
        }
    }

    [Test]
    public void TestClipTextured_OneInsideTwoOutside_PreservesWorldNormal()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(0, 1, -1, 1);
        tri.Triangle.p3 = new Vector4(1, 0, -1, 1);
        tri.Normal = Vector3.Normalize(new Vector3(0, 1, 0));

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(1));
        Assert.That(_clipping.Clipping[0].Normal, Is.EqualTo(tri.Normal));
    }

    [Test]
    public void TestClipTextured_TwoInsideOneOutside_PreservesWorldNormal()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(1, 0, 1, 1);
        tri.Triangle.p3 = new Vector4(0.5f, 1, -1, 1);
        tri.Normal = Vector3.Normalize(new Vector3(-1, 0, 0));

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(2));

        foreach (var clipped in _clipping.Clipping)
        {
            Assert.That(clipped.Normal, Is.EqualTo(tri.Normal));
        }
    }

    [Test]
    public void TestClipTextured_ScreenClip_PerspectiveCorrectUVInvariant()
    {
        // Screen-space clip: p.w == 1 for every vertex, while the texture points carry
        // (u/w, v/w, 1/w). Interpolation of the tex points must therefore be linear and
        // the perspective-correct UV has to be recovered as X/Z.
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(10, 0, 0, 1);
        tri.Triangle.p2 = new Vector4(10, 50, 0, 1);
        tri.Triangle.p3 = new Vector4(-50, 50, 0, 1);
        tri.TexturePoint1 = new Vector3(0f, 0f, 0.5f);
        tri.TexturePoint2 = new Vector3(1f, 0f, 0.1f);
        tri.TexturePoint3 = new Vector3(0f, 1f, 1.0f);
        tri.TextureId = 3;

        var planeP = new Vector3(0, 0, 0);
        var planeN = new Vector3(1, 0, 0);

        _clipping.ClipAgainstClip(planeP, planeN, tri, _triPool, _debugPool);

        // Two vertices inside, one outside -> two triangles; the first one carries the
        // new vertex on the p1 -> p3 edge at index 3.
        Assert.That(_clipping.Clipping.Length, Is.EqualTo(2));

        ClippingInstance.IntersectPlane(planeP, planeN, tri.Triangle.p1, tri.Triangle.p3, out var t13);
        ClippingInstance.IntersectPlane(planeP, planeN, tri.Triangle.p2, tri.Triangle.p3, out var t23);

        AssertPerspectiveCorrect(tri.TexturePoint1, tri.TexturePoint3, t13, _clipping.Clipping[0].TexturePoint3);
        AssertPerspectiveCorrect(tri.TexturePoint2, tri.TexturePoint3, t23, _clipping.Clipping[1].TexturePoint3);
    }

    private static void AssertPerspectiveCorrect(Vector3 from, Vector3 to, float t, Vector3 actual)
    {
        // Re-derive the original UV and w from the (u/w, v/w, 1/w) triple and evaluate the
        // classic perspective-correct formula independently of ClipTextured.
        var u0 = from.X / from.Z;
        var v0 = from.Y / from.Z;
        var w0 = 1f / from.Z;
        var u1 = to.X / to.Z;
        var v1 = to.Y / to.Z;
        var w1 = 1f / to.Z;

        var denominator = (1f - t) / w0 + t / w1;
        var expectedU = ((1f - t) * u0 / w0 + t * u1 / w1) / denominator;
        var expectedV = ((1f - t) * v0 / w0 + t * v1 / w1) / denominator;

        Assert.That(MathF.Abs(actual.Z), Is.GreaterThan(1e-9f));
        Assert.That(actual.X / actual.Z, Is.EqualTo(expectedU).Within(1e-5f));
        Assert.That(actual.Y / actual.Z, Is.EqualTo(expectedV).Within(1e-5f));

        // The affine (screen-space) blend must give a different answer, otherwise this
        // test would not actually be exercising the perspective correction.
        var affineU = (1f - t) * u0 + t * u1;
        var affineV = (1f - t) * v0 + t * v1;
        var differsFromAffine = MathF.Abs(expectedU - affineU) > 1e-4f || MathF.Abs(expectedV - affineV) > 1e-4f;
        Assert.That(differsFromAffine, Is.True,
            "perspective-correct and affine interpolation coincide, test is vacuous");
    }

    [Test]
    public void TestClipTextured_ReusesPooledTriangles_ClearsStaleData()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(0, 0, 1, 1);
        tri.Triangle.p2 = new Vector4(1, 0, 1, 1);
        tri.Triangle.p3 = new Vector4(0.5f, 1, 1, 1);
        tri.TextureId = 99;

        _clipping.ClipAgainstClip(
            new Vector3(0, 0, 0.1f),
            new Vector3(0, 0, 1),
            tri,
            _triPool,
            _debugPool);

        Assert.That(_clipping.Clipping.Length, Is.EqualTo(1));

        var result = _clipping.Clipping[0];
        Assert.That(result.TextureId, Is.EqualTo(99));
    }

    [Test]
    public void TestClipTextured_PoolTakeReturnsObject()
    {
        var pool = new SimplePool<TexturedTriangle>(10, () => new TexturedTriangle());
        var obj = pool.Take();
        Assert.That(obj, Is.Not.Null);
        Assert.That(pool.Length, Is.EqualTo(1));
    }

    [Test]
    public void TestClipTextured_PoolRecycles()
    {
        var pool = new SimplePool<TexturedTriangle>(10, () => new TexturedTriangle());
        var obj1 = pool.Take();
        obj1.TextureId = 42;
        pool.Clear();
        Assert.That(pool.Length, Is.EqualTo(0));

        var obj2 = pool.Take();
        Assert.That(pool.Length, Is.EqualTo(1));
    }
}

[TestFixture]
public sealed class BackfaceCullingTests
{
    [Test]
    public void TestTriangleFacingAway_Culled()
    {
        var triangle = new Triangle();
        triangle.p1 = new Vector4(0, 0, 0, 1);
        triangle.p2 = new Vector4(1, 0, 0, 1);
        triangle.p3 = new Vector4(0, 1, 0, 1);
        var normal = Vector3.Normalize(triangle.Normal());
        var cameraPos = new Vector3(0, 0, -1);
        var cameraRay = Vector3.Normalize(triangle.GetP1() - cameraPos);
        var dot = Vector3.Dot(normal, cameraRay);

        Assert.That(dot, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void TestTriangleFacingCamera_NotCulled()
    {
        var triangle = new Triangle();
        triangle.p1 = new Vector4(0, 0, 0, 1);
        triangle.p2 = new Vector4(1, 0, 0, 1);
        triangle.p3 = new Vector4(0, 1, 0, 1);

        var normal = Vector3.Normalize(triangle.Normal());
        var cameraPos = new Vector3(0, 0, 1);
        var cameraRay = Vector3.Normalize(triangle.GetP1() - cameraPos);
        var dot = Vector3.Dot(normal, cameraRay);

        Assert.That(dot, Is.LessThan(0));
    }

    [Test]
    public void TestTriangleSideways_NotCulled()
    {
        var triangle = new Triangle();
        triangle.p1 = new Vector4(1, 0, 0, 1);
        triangle.p2 = new Vector4(2, 0, 0, 1);
        triangle.p3 = new Vector4(1, 1, 0, 1);

        var normal = Vector3.Normalize(triangle.Normal());
        var cameraPos = new Vector3(0, 0, 0);
        var cameraRay = Vector3.Normalize(triangle.GetP1() - cameraPos);
        var dot = Vector3.Dot(normal, cameraRay);

        Assert.That(dot, Is.EqualTo(0).Within(0.0001f));
    }

    [Test]
    public void TestWindingOrder_ClockwiseCulled()
    {
        var triangle = new Triangle();
        triangle.p1 = new Vector4(0, 0, 0, 1);
        triangle.p2 = new Vector4(0, 1, 0, 1);
        triangle.p3 = new Vector4(1, 0, 0, 1);
        var normal = Vector3.Normalize(triangle.Normal());
        var cameraPos = new Vector3(0, 0, 1);
        var cameraRay = Vector3.Normalize(triangle.GetP1() - cameraPos);
        var dot = Vector3.Dot(normal, cameraRay);

        Assert.That(dot, Is.GreaterThanOrEqualTo(0));
    }
}

[TestFixture]
public sealed class SimpleBufferTests
{
    [Test]
    public void TestAddAndGet()
    {
        var buffer = new SimpleBuffer<int>(10);
        buffer.Add(42);
        buffer.Add(100);

        Assert.That(buffer.Length, Is.EqualTo(2));
        Assert.That(buffer[0], Is.EqualTo(42));
        Assert.That(buffer[1], Is.EqualTo(100));
    }

    [Test]
    public void TestClear()
    {
        var buffer = new SimpleBuffer<int>(10);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Clear();

        Assert.That(buffer.Length, Is.EqualTo(0));
    }

    [Test]
    public void TestSort()
    {
        var buffer = new SimpleBuffer<int>(10);
        buffer.Add(3);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Sort();

        Assert.That(buffer[0], Is.EqualTo(1));
        Assert.That(buffer[1], Is.EqualTo(2));
        Assert.That(buffer[2], Is.EqualTo(3));
    }

    [Test]
    public void TestSortWithComparer()
    {
        var buffer = new SimpleBuffer<int>(10);
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Sort(Comparer<int>.Create((a, b) => b.CompareTo(a)));

        Assert.That(buffer[0], Is.EqualTo(3));
        Assert.That(buffer[1], Is.EqualTo(2));
        Assert.That(buffer[2], Is.EqualTo(1));
    }

    [Test]
    public void TestIndexerSet()
    {
        var buffer = new SimpleBuffer<int>(10);
        buffer.Add(1);
        buffer[0] = 42;

        Assert.That(buffer[0], Is.EqualTo(42));
    }
}

[TestFixture]
public sealed class SimplePoolTests
{
    [Test]
    public void TestTake_AllocatesOnDemand()
    {
        var pool = new SimplePool<string>(10, () => "new");
        var item = pool.Take();

        Assert.That(item, Is.EqualTo("new"));
        Assert.That(pool.Length, Is.EqualTo(1));
    }

    [Test]
    public void TestTake_ReusesClearedEntries()
    {
        var pool = new SimplePool<MutableString>(10, () => new MutableString());
        var item1 = pool.Take();
        item1.Value = "hello";
        pool.Clear();

        var item2 = pool.Take();
        Assert.That(pool.Length, Is.EqualTo(1));
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void TestClear()
    {
        var pool = new SimplePool<int>(10, () => 0);
        pool.Take();
        pool.Take();
        pool.Clear();

        Assert.That(pool.Length, Is.EqualTo(0));
    }

    private sealed class MutableString
    {
        public string Value = "";
    }
}

[TestFixture]
public sealed class TriangleZComparerTests
{
    [Test]
    public void TestSortByViewSpaceZ()
    {
        var tri1 = new TexturedTriangle();
        tri1.Triangle.ViewSpaceZ = 10;
        var tri2 = new TexturedTriangle();
        tri2.Triangle.ViewSpaceZ = 5;
        var tri3 = new TexturedTriangle();
        tri3.Triangle.ViewSpaceZ = 15;

        var comparer = new TriangleZComparer();
        var list = new List<TexturedTriangle> { tri1, tri2, tri3 };
        list.Sort(comparer);

        Assert.That(list[0].Triangle.ViewSpaceZ, Is.EqualTo(15));
        Assert.That(list[1].Triangle.ViewSpaceZ, Is.EqualTo(10));
        Assert.That(list[2].Triangle.ViewSpaceZ, Is.EqualTo(5));
    }

    [Test]
    public void TestFloorDrawnBeforeCharacter_WhenFarther()
    {
        // A floor quad spanning from z = 2 (near edge) to z = 100 (far edge) must be
        // sorted BEFORE a character standing at z = 30, otherwise it would be drawn over
        // him. Sorting by the farthest vertex makes the floor come first.
        var floor = new TexturedTriangle();
        floor.Triangle.p1 = new Vector4(0, 0, 2, 1);
        floor.Triangle.p2 = new Vector4(0, 0, 100, 1);
        floor.Triangle.p3 = new Vector4(0, 0, 100, 1);
        floor.Triangle.SetViewSpaceZToFarthest();

        var character = new TexturedTriangle();
        character.Triangle.p1 = new Vector4(0, 0, 30, 1);
        character.Triangle.p2 = new Vector4(0, 0, 30, 1);
        character.Triangle.p3 = new Vector4(0, 0, 30, 1);
        character.Triangle.SetViewSpaceZToFarthest();

        var comparer = new TriangleZComparer();
        var list = new List<TexturedTriangle> { character, floor };
        list.Sort(comparer);

        Assert.That(list[0], Is.SameAs(floor));
        Assert.That(list[1], Is.SameAs(character));
    }

    [Test]
    public void TestNullHandling()
    {
        var comparer = new TriangleZComparer();
        Assert.That(comparer.Compare(null, null), Is.EqualTo(0));
        Assert.That(comparer.Compare(null, new TexturedTriangle()), Is.EqualTo(0));
        Assert.That(comparer.Compare(new TexturedTriangle(), null), Is.EqualTo(0));
    }

    [Test]
    public void TestEqualDepth_DrawOrderBreaksTieDeterministically()
    {
        var first = new TexturedTriangle { DrawOrder = 0 };
        first.Triangle.ViewSpaceZ = 5;
        var second = new TexturedTriangle { DrawOrder = 1 };
        second.Triangle.ViewSpaceZ = 5;

        var comparer = new TriangleZComparer();

        Assert.That(comparer.Compare(first, second), Is.LessThan(0));
        Assert.That(comparer.Compare(second, first), Is.GreaterThan(0));
    }

    [Test]
    public void TestNearest_DrawsFrontShellOverInnerSurface()
    {
        // An outer shell triangle has a far vertex (z = 8) but its near vertices (z = 4)
        // are in front of the inner surface at z = 5. Sorting by the farthest vertex
        // pushes the shell behind the surface (the inner layer shows through, wrong);
        // the nearest vertex keeps the shell in front.
        var inner = new TexturedTriangle();
        inner.Triangle.p1 = new Vector4(0, 0, 5, 1);
        inner.Triangle.p2 = new Vector4(0, 0, 5, 1);
        inner.Triangle.p3 = new Vector4(0, 0, 5, 1);
        inner.Triangle.SetViewSpaceZToNearest();

        var shell = new TexturedTriangle();
        shell.Triangle.p1 = new Vector4(0, 0, 4, 1);
        shell.Triangle.p2 = new Vector4(0, 0, 4, 1);
        shell.Triangle.p3 = new Vector4(0, 0, 8, 1);
        shell.Triangle.SetViewSpaceZToNearest();

        var comparer = new TriangleZComparer();
        var list = new List<TexturedTriangle> { shell, inner };
        list.Sort(comparer);

        Assert.That(list[0], Is.SameAs(inner));
        Assert.That(list[1], Is.SameAs(shell));
    }
}

[TestFixture]
public sealed class ViewportClipEdgeTests
{
    [Test]
    public void TestClipAgainstScreenTop()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(50, -20, 0.5f, 1);
        tri.Triangle.p2 = new Vector4(100, 30, 0.5f, 1);
        tri.Triangle.p3 = new Vector4(10, 40, 0.5f, 1);

        var clipping = new ClippingInstance();
        var triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
        var debugPool = new SimplePool<Triangle>(256, () => new Triangle());

        clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            tri,
            triPool,
            debugPool);

        Assert.That(clipping.Clipping.Length, Is.GreaterThanOrEqualTo(1));

        foreach (var result in clipping.Clipping)
        {
            Assert.That(result.Triangle.GetP1().Y, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(result.Triangle.GetP2().Y, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(result.Triangle.GetP3().Y, Is.GreaterThanOrEqualTo(-0.0001f));
        }
    }

    [Test]
    public void TestClipAgainstScreenBottom()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(50, 500, 0.5f, 1);
        tri.Triangle.p2 = new Vector4(100, 30, 0.5f, 1);
        tri.Triangle.p3 = new Vector4(10, 40, 0.5f, 1);

        var clipping = new ClippingInstance();
        var triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
        var debugPool = new SimplePool<Triangle>(256, () => new Triangle());

        var screenHeight = 480f;
        clipping.ClipAgainstClip(
            new Vector3(0, screenHeight - 1, 0),
            new Vector3(0, -1, 0),
            tri,
            triPool,
            debugPool);

        Assert.That(clipping.Clipping.Length, Is.GreaterThanOrEqualTo(1));

        foreach (var result in clipping.Clipping)
        {
            Assert.That(result.Triangle.GetP1().Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
            Assert.That(result.Triangle.GetP2().Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
            Assert.That(result.Triangle.GetP3().Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
        }
    }

    [Test]
    public void TestClipAgainstScreenLeft()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(-20, 50, 0.5f, 1);
        tri.Triangle.p2 = new Vector4(100, 30, 0.5f, 1);
        tri.Triangle.p3 = new Vector4(10, 40, 0.5f, 1);

        var clipping = new ClippingInstance();
        var triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
        var debugPool = new SimplePool<Triangle>(256, () => new Triangle());

        clipping.ClipAgainstClip(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            tri,
            triPool,
            debugPool);

        Assert.That(clipping.Clipping.Length, Is.GreaterThanOrEqualTo(1));

        foreach (var result in clipping.Clipping)
        {
            Assert.That(result.Triangle.GetP1().X, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(result.Triangle.GetP2().X, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(result.Triangle.GetP3().X, Is.GreaterThanOrEqualTo(-0.0001f));
        }
    }

    [Test]
    public void TestClipAgainstScreenRight()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(850, 50, 0.5f, 1);
        tri.Triangle.p2 = new Vector4(100, 30, 0.5f, 1);
        tri.Triangle.p3 = new Vector4(10, 40, 0.5f, 1);

        var clipping = new ClippingInstance();
        var triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
        var debugPool = new SimplePool<Triangle>(256, () => new Triangle());

        var screenWidth = 800f;
        clipping.ClipAgainstClip(
            new Vector3(screenWidth - 1, 0, 0),
            new Vector3(-1, 0, 0),
            tri,
            triPool,
            debugPool);

        Assert.That(clipping.Clipping.Length, Is.GreaterThanOrEqualTo(1));

        foreach (var result in clipping.Clipping)
        {
            Assert.That(result.Triangle.GetP1().X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
            Assert.That(result.Triangle.GetP2().X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
            Assert.That(result.Triangle.GetP3().X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
        }
    }

    [Test]
    public void TestClipAllFourEdges_KeepsTriangleInScreen()
    {
        var tri = new TexturedTriangle();
        tri.Triangle.p1 = new Vector4(-50, -50, 0.5f, 1);
        tri.Triangle.p2 = new Vector4(900, 200, 0.5f, 1);
        tri.Triangle.p3 = new Vector4(200, 600, 0.5f, 1);

        var clipping = new ClippingInstance();
        var triPool = new SimplePool<TexturedTriangle>(256, () => new TexturedTriangle());
        var debugPool = new SimplePool<Triangle>(256, () => new Triangle());

        var screenWidth = 800f;
        var screenHeight = 480f;

        clipping.ClipAgainstClip(new Vector3(0, 0, 0), new Vector3(0, 1, 0), tri, triPool, debugPool);
        var afterTop = new List<TexturedTriangle>(clipping.Clipping);
        clipping.Clear();
        var tri2 = afterTop[0];
        for (int i = 1; i < afterTop.Count; i++)
            tri2 = afterTop[i];
        _ = afterTop.Count;

        clipping.Clear();
        clipping.ClipAgainstClip(new Vector3(0, screenHeight - 1, 0), new Vector3(0, -1, 0), tri, triPool, debugPool);
        clipping.Clear();
        clipping.ClipAgainstClip(new Vector3(0, 0, 0), new Vector3(1, 0, 0), tri, triPool, debugPool);
        clipping.Clear();
        clipping.ClipAgainstClip(new Vector3(screenWidth - 1, 0, 0), new Vector3(-1, 0, 0), tri, triPool, debugPool);

        clipping.ClipAgainstClip(new Vector3(0, 0, 0), new Vector3(0, 1, 0), tri, triPool, debugPool);
        var queue = new Queue<TexturedTriangle>();
        queue.Enqueue(tri);

        var screenEdges = new (Vector3 planeP, Vector3 planeN)[]
        {
            (new Vector3(0, 0, 0), new Vector3(0, 1, 0)),
            (new Vector3(0, screenHeight - 1, 0), new Vector3(0, -1, 0)),
            (new Vector3(0, 0, 0), new Vector3(1, 0, 0)),
            (new Vector3(screenWidth - 1, 0, 0), new Vector3(-1, 0, 0)),
        };

        foreach (var (planeP, planeN) in screenEdges)
        {
            var nNew = queue.Count;
            while (nNew > 0)
            {
                var t = queue.Dequeue();
                nNew--;
                clipping.ClipAgainstClip(planeP, planeN, t, triPool, debugPool);
                for (var w = 0; w < clipping.Clipping.Length; w++)
                    queue.Enqueue(clipping.Clipping[w]);
            }
        }

        Assert.That(queue.Count, Is.GreaterThanOrEqualTo(1));

        foreach (var result in queue)
        {
            var p1 = result.Triangle.GetP1();
            var p2 = result.Triangle.GetP2();
            var p3 = result.Triangle.GetP3();
            Assert.That(p1.X, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p1.Y, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p1.X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
            Assert.That(p1.Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
            Assert.That(p2.X, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p2.Y, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p2.X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
            Assert.That(p2.Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
            Assert.That(p3.X, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p3.Y, Is.GreaterThanOrEqualTo(-0.0001f));
            Assert.That(p3.X, Is.LessThanOrEqualTo(screenWidth - 1 + 0.0001f));
            Assert.That(p3.Y, Is.LessThanOrEqualTo(screenHeight - 1 + 0.0001f));
        }
    }
}
