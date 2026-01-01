using System;

namespace SilkDotNetLibrary.OpenGL.Primitives;

/// <summary>
/// Sphere - A UV sphere primitive with complete vertex attributes
/// Generated using spherical coordinates with configurable subdivision levels
/// Position (3) + Normal (3) + TexCoords (2) + Tangent (3) + BiTangent (3) + Color (3) = 17 floats
/// <seealso cref="https://en.wikipedia.org/wiki/Differential_geometry_of_surfaces"/>
/// </summary>
public static class Sphere
{
    public const int VerticeSize = 17; // Matches Vertex structure completely
    
    // Sphere generation parameters
    private const int SectorCount = 16; // Horizontal subdivisions (longitude)
    private const int StackCount = 12;  // Vertical subdivisions (latitude)
    private const float Radius = 1.0f;
    
    /// <summary>
    /// Sphere vertex data with all attributes
    /// Generated procedurally using spherical coordinates
    /// </summary>
    public static readonly float[] Vertices = GenerateVertices();
    
    /// <summary>
    /// Sphere triangle indices
    /// Generated to create triangular faces across the sphere surface
    /// </summary>
    public static readonly uint[] Indices = GenerateIndices();
    
    /// <summary>
    /// Generate sphere vertices using spherical coordinates
    /// </summary>
    private static float[] GenerateVertices()
    {
        var vertices = new System.Collections.Generic.List<float>();
        
        float sectorStep = 2 * MathF.PI / SectorCount;
        float stackStep = MathF.PI / StackCount;
        
        for (int i = 0; i <= StackCount; ++i)
        {
            float stackAngle = MathF.PI / 2 - i * stackStep; // From pi/2 to -pi/2
            float xy = Radius * MathF.Cos(stackAngle);       // r * cos(u)
            float z = Radius * MathF.Sin(stackAngle);        // r * sin(u)
            
            for (int j = 0; j <= SectorCount; ++j)
            {
                float sectorAngle = j * sectorStep; // From 0 to 2pi
                
                // Position
                float x = xy * MathF.Cos(sectorAngle); // r * cos(u) * cos(v)
                float y = xy * MathF.Sin(sectorAngle); // r * cos(u) * sin(v)
                vertices.Add(x);
                vertices.Add(y);
                vertices.Add(z);
                
                // Normal (normalized position for unit sphere)
                float length = MathF.Sqrt(x * x + y * y + z * z);
                vertices.Add(x / length); // Nx
                vertices.Add(y / length); // Ny
                vertices.Add(z / length); // Nz
                
                // Texture coordinates
                float s = (float)j / SectorCount;
                float t = (float)i / StackCount;
                vertices.Add(s); // U
                vertices.Add(t); // V
                
                // Tangent (approximate tangent along longitude)
                float tx = -MathF.Sin(sectorAngle);
                float ty = MathF.Cos(sectorAngle);
                float tz = 0.0f;
                vertices.Add(tx);
                vertices.Add(ty);
                vertices.Add(tz);
                
                // BiTangent (cross product of normal and tangent)
                float nx = x / length;
                float ny = y / length;
                float nz = z / length;
                float bx = ny * tz - nz * ty;
                float by = nz * tx - nx * tz;
                float bz = nx * ty - ny * tx;
                vertices.Add(bx);
                vertices.Add(by);
                vertices.Add(bz);
                
                // Color (light blue)
                vertices.Add(0.5f); // R
                vertices.Add(0.7f); // G
                vertices.Add(1.0f); // B
            }
        }
        
        return vertices.ToArray();
    }
    
    /// <summary>
    /// Generate sphere indices for triangular faces
    /// </summary>
    private static uint[] GenerateIndices()
    {
        var indices = new System.Collections.Generic.List<uint>();
        
        for (int i = 0; i < StackCount; ++i)
        {
            uint k1 = (uint)(i * (SectorCount + 1));     // Beginning of current stack
            uint k2 = (uint)(k1 + SectorCount + 1);      // Beginning of next stack
            
            for (int j = 0; j < SectorCount; ++j, ++k1, ++k2)
            {
                // Two triangles per sector
                if (i != 0) // Skip first stack
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }
                
                if (i != (StackCount - 1)) // Skip last stack
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }
        
        return indices.ToArray();
    }
    
    /// <summary>
    /// Get total vertex count
    /// </summary>
    public static int VertexCount => Vertices.Length / VerticeSize;
    
    /// <summary>
    /// Get total triangle count
    /// </summary>
    public static int TriangleCount => Indices.Length / 3;
    
    /// <summary>
    /// Get sphere radius
    /// </summary>
    public static float SphereRadius => Radius;
    
    /// <summary>
    /// Get bounding sphere radius (same as sphere radius)
    /// </summary>
    public static float BoundingSphereRadius => Radius;
    
    /// <summary>
    /// Get sphere surface area
    /// </summary>
    public static float SurfaceArea => 4 * MathF.PI * Radius * Radius;
    
    /// <summary>
    /// Get sphere volume
    /// </summary>
    public static float Volume => (4.0f / 3.0f) * MathF.PI * Radius * Radius * Radius;
} 