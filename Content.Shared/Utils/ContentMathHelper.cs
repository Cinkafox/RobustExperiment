
namespace Content.Shared.Utils;

public static class ContentMathHelper
{
    public const float Epsilon = 1.192092896e-012f;
    public const float ZeroEpsilonSq = Epsilon * Epsilon;

    public static bool IsNearlyZero(this Vector3 vector3)
    {
        return vector3.LengthSquared() < ZeroEpsilonSq;
    }

    public static void Normalize(this ref Vector3 vector3)
    {
        vector3 = Vector3.Normalize(vector3);
    }
}