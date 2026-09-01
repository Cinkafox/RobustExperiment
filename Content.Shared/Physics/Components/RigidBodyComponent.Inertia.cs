using Content.Shared.Utils;

namespace Content.Shared.Physics.Components;

public partial class RigidBodyComponent
{
    // Cached inertia values
    private Vector3 _localInertia;
    private Vector3 _localInvInertia;
    private Matrix3x3 _worldInertia;
    private Matrix3x3 _worldInvInertia;
    private bool _inertiaDirty = true;
    
    /// <summary>
    /// Local-space diagonal inertia tensor (Ixx, Iyy, Izz)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector3 LocalInertia
    {
        get
        {
            UpdateInertiaIfNeeded();
            return _localInertia;
        }
    }
    
    /// <summary>
    /// Local-space diagonal inverse inertia tensor
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector3 LocalInvInertia
    {
        get
        {
            UpdateInertiaIfNeeded();
            return _localInvInertia;
        }
    }
    
    /// <summary>
    /// World-space inertia tensor (3x3 matrix)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Matrix3x3 WorldInertia
    {
        get
        {
            UpdateInertiaIfNeeded();
            return _worldInertia;
        }
    }
    
    /// <summary>
    /// World-space inverse inertia tensor (3x3 matrix)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Matrix3x3 WorldInvInertia
    {
        get
        {
            UpdateInertiaIfNeeded();
            return _worldInvInertia;
        }
    }
    
     public void InvalidateInertia()
    {
        _inertiaDirty = true;
    }
    
    private void UpdateInertiaIfNeeded()
    {
        if (!_inertiaDirty) return;
        
        var mass = Mass;
        
        if (PhysType != PhysType.Dynamic || mass <= 0f)
        {
            _localInertia = Vector3.Zero;
            _localInvInertia = Vector3.Zero;
            _worldInertia = Matrix3x3.Zero;
            _worldInvInertia = Matrix3x3.Zero;
        }
        else
        {
            // Calculate local diagonal inertia
            _localInertia = Shape.CalculateLocalInertia(mass);
            
            // Inverse (avoid division by zero)
            _localInvInertia = new Vector3(
                _localInertia.X > 1e-6f ? 1f / _localInertia.X : 0f,
                _localInertia.Y > 1e-6f ? 1f / _localInertia.Y : 0f,
                _localInertia.Z > 1e-6f ? 1f / _localInertia.Z : 0f
            );
            
            // World-space tensors will be computed during physics step
            // using the body's rotation quaternion
            _worldInertia = Matrix3x3.Identity; // Placeholder
            _worldInvInertia = Matrix3x3.Identity; // Placeholder
        }
        
        _inertiaDirty = false;
    }
    
    /// <summary>
    /// Update world-space inertia tensors based on current rotation.
    /// Call this during physics integration after rotation changes.
    /// </summary>
    public void UpdateWorldInertia(Quaternion rotation)
    {
        if (PhysType != PhysType.Dynamic)
        {
            _worldInertia = Matrix3x3.Zero;
            _worldInvInertia = Matrix3x3.Zero;
            return;
        }
        
        UpdateInertiaIfNeeded();
        
        // Convert diagonal local inertia to 3x3 matrix
        var localInertiaMatrix = new Matrix3x3(
           _localInertia.X, 0f, 0f,
            0f, _localInertia.Y, 0f,
            0f, 0f, _localInertia.Z
        );
        
        var localInvInertiaMatrix = new Matrix3x3(
            _localInvInertia.X, 0, 0,
            0, _localInvInertia.Y, 0,
            0, 0, _localInvInertia.Z
        );
        
        // Rotate to world space: I_world = R * I_local * R^T
        var rotationMatrix = Matrix3x3.CreateFromQuaternion(rotation);
        var rotationMatrixT = Matrix3x3.Transpose(rotationMatrix);
        
        _worldInertia = rotationMatrix * localInertiaMatrix * rotationMatrixT;
        _worldInvInertia = rotationMatrix * localInvInertiaMatrix * rotationMatrixT;
    }
}