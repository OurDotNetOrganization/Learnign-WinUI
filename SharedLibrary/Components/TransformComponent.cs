using System.Numerics;

namespace SharedLibrary.Components;

public struct TransformComponent : IComponent
{
    public Vector3 Position { get; set; } 
    public Quaternion Rotation { get; set; }
}