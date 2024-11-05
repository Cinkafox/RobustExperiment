using Robust.Shared.Serialization;

namespace Content.Shared.Physics.Data;

[DataDefinition]
public sealed partial class ManifoldPoints
{
    public static ManifoldPoints Empty => new();
    
    [DataField] public Vector3 A = Vector3.Zero;
    [DataField] public Vector3 B = Vector3.Zero;
    [DataField] public Vector3 Normal = Vector3.Zero;
    [DataField] public float Depth;
    [DataField] public bool HasContact;
    
    public ManifoldPoints(){}

    public ManifoldPoints(Vector3 a, Vector3 b)
    {
        HasContact = true;
        A = a;
        B = b;
        var ba = a - b;
        Depth = ba.Length();
        
        if (Depth > 0.00001f)
        {
            Normal = ba / Depth;
        }
        else {
            Normal = new Vector3(0, 1, 0);
            Depth = 1;
        }
        HasContact = true;
    }

    public ManifoldPoints(Vector3 a, Vector3 b, Vector3 normal, float distance)
    {
        A = a;
        B = b;
        Normal = normal;
        Depth = distance;
        HasContact = true;
    }

    public void SwapPoints()
    {
        (A, B) = (B, A);
        Normal = -Normal;
    }
}