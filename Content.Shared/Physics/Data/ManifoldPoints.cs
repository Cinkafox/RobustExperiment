
namespace Content.Shared.Physics.Data;

[DataDefinition]
public sealed partial class ManifoldPoints
{
    // Empty manifold with negative depth to indicate no contact
    public static readonly ManifoldPoints Empty = new() { Depth = -1f };

    [DataField] public Vector3 A = Vector3.Zero;      // Contact point on shape A (world space)
    [DataField] public Vector3 B = Vector3.Zero;      // Contact point on shape B (world space)
    [DataField] public Vector3 Normal = Vector3.Zero; // Unit normal pointing FROM A TO B
    [DataField] public float Depth = -1f;             // Penetration depth (positive = overlap)

    /// <summary>
    /// Whether this manifold represents an actual collision.
    /// Derived property - NOT serialized to prevent desync between Depth and HasContact.
    /// </summary>
    [ViewVariables]
    public bool HasContact => Depth > 1e-4f;

    public ManifoldPoints() {}

    /// <summary>
    /// Primary constructor for collision detection systems.
    /// </summary>
    /// <param name="pointA">Contact point on shape A's surface</param>
    /// <param name="pointB">Contact point on shape B's surface</param>
    /// <param name="normal">Collision normal pointing FROM A TO B (will be normalized)</param>
    /// <param name="penetrationDepth">Penetration depth (positive value when shapes overlap)</param>
    public ManifoldPoints(Vector3 pointA, Vector3 pointB, Vector3 normal, float penetrationDepth)
    {
        A = pointA;
        B = pointB;
        Normal = normal.LengthSquared() > 1e-6f ? Vector3.Normalize(normal) : Vector3.UnitY;
        Depth = MathF.Max(0f, penetrationDepth); // Clamp to non-negative
    }

    /// <summary>
    /// Swap contact points and reverse normal direction.
    /// Used for symmetric collision handling (e.g., RevCollider).
    /// Preserves penetration depth magnitude.
    /// </summary>
    public void SwapPoints()
    {
        (A, B) = (B, A);
        Normal = -Normal;
        // Depth remains unchanged (penetration amount is symmetric)
    }

    /// <summary>
    /// Validate manifold for numerical stability.
    /// </summary>
    public void Validate(string context = "")
    {
        if (Depth < -1e-3f)
            Logger.Warning($"Invalid manifold depth ({Depth}) {context}");

        if (Depth > 10f) // Prevent excessive penetration impulses
            Logger.Warning($"Excessive penetration depth ({Depth}) {context}");

        var normalLenSq = Normal.LengthSquared();
        if (normalLenSq < 0.95f || normalLenSq > 1.05f)
            Logger.Warning($"Non-unit normal (length²={normalLenSq}) {context}");
    }
}