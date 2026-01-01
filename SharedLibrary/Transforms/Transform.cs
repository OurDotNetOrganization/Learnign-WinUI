using System.Numerics;

namespace SharedLibrary.Transforms;

public struct Transform : ITransfrom
{
    public Transform()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = 1f;
    }
    public Vector3 Position { get; set; } = new Vector3(0, 0, 0);

    public float Scale { get; set; } = 1f;

    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public LowerTriangular ShearLowerTriangular { get; set; } = new LowerTriangular(1f,3f,4f);
    public UpperTriangular ShearUpperTriangular { get; set; } = new UpperTriangular();
    public readonly Matrix4x4 ShearMatrix => new Matrix4x4(
        1, ShearUpperTriangular.XY, ShearUpperTriangular.XZ, 0,
        ShearLowerTriangular.YX, 1, ShearUpperTriangular.YZ, 0,
        ShearLowerTriangular.ZX, ShearLowerTriangular.ZY, 1, 0,
        0, 0, 0, 1);

    /// <summary>
    /// MVP Matrix
    /// </summary>
    public readonly Matrix4x4 ViewMatrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position);
}

public readonly record struct UpperTriangular(float XY, float XZ, float YZ);
public readonly record struct LowerTriangular(float YX, float ZX, float ZY);