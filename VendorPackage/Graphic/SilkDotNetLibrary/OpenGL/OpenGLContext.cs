using MathNet.Numerics;
using Microsoft.Extensions.Logging;
using Serilog;
using SharedLibrary.Cameras;
using SharedLibrary.Math;
using SharedLibrary.Systems;
using SharedLibrary.Transforms;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkDotNetLibrary.OpenGL.Buffers;
using SilkDotNetLibrary.OpenGL.Meshes;
using SilkDotNetLibrary.OpenGL.Primitives;
using SilkDotNetLibrary.OpenGL.Textures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using StbImageSharp;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Shader = SilkDotNetLibrary.OpenGL.Shaders.Shader;

namespace SilkDotNetLibrary.OpenGL;

public class OpenGLContext : IOpenGLContext, IDisposable
{
    private bool _disposedValue;
    private readonly IWindow _window;
    private readonly ICamera _camera;
    private readonly ILogger<OpenGLContext> _logger;
    private readonly MeshComponentFactory _meshComponentFactory;
    private readonly Transform[] _transforms = new Transform[4];
    private readonly Vector3 _lampPosition = new(1.2f, 1.0f, 2.0f);
    private GL _gl;

    //private ImGuiController _ImGuiController;
    private FrameBufferObject Fbo { get; set; }
    private RenderBufferObject Rbo { get; set; }
    private BufferObject<float> SkyBoxVbo { get; set; }
    private VertexArrayBufferObject<float, uint> SkyBoxVao { get; set; }
    private BufferObject<float> CubeVbo { get; set; }
    private BufferObject<uint> CubeEbo { get; set; }
    private VertexArrayBufferObject<float, uint> CubeVao { get; set; }
    
    // 二十面體 (Icosahedron) 相關的緩衝區
    private BufferObject<float> IcosahedronVbo { get; set; }
    private BufferObject<uint> IcosahedronEbo { get; set; }
    private VertexArrayBufferObject<float, uint> IcosahedronVao { get; set; }

    // 十二面體 (Dodecahedron) 相關的緩衝區
    private BufferObject<float> DodecahedronVbo { get; set; }
    private BufferObject<uint> DodecahedronEbo { get; set; }
    private VertexArrayBufferObject<float, uint> DodecahedronVao { get; set; }
    
    // 四面體 (Tetrahedron) 相關的緩衝區
    private BufferObject<float> TetrahedronVbo { get; set; }
    private BufferObject<uint> TetrahedronEbo { get; set; }
    private VertexArrayBufferObject<float, uint> TetrahedronVao { get; set; }
    
    // 球體 (Sphere) 相關的緩衝區
    private BufferObject<float> SphereVbo { get; set; }
    private BufferObject<uint> SphereEbo { get; set; }
    private VertexArrayBufferObject<float, uint> SphereVao { get; set; }

    private BufferObject<float> QuadVbo { get; set; }
    private VertexArrayBufferObject<float, uint> QuadVao { get; set; }
    private Shader LightingShader { get; set; }
    private Shader LampShader { get; set; }
    private Shader ScreenShader { get; set; }
    private Shader[] MeshShaders { get; set; } = new Shader[3];
    private Shader SkyBoxShader { get; set; } = new Shader();

    //private Textures.Texture Texture { get; set; }
    private Textures.Texture DiffuseMap { get; set; }
    private Textures.Texture SpecularMap { get; set; }
    private CubeMapTexture CubeMapTexture { get; set; }
    private FrameBufferTexture Fbt { get; set; }
    private float Time { get; set; }
    private MeshComponent MeshComponent { get; set; }

    //Setup the camera's location, and relative up and right directions
    public OpenGLContext(
        IWindow window,
        ICamera camera,
        ILogger<OpenGLContext> logger,
        MeshComponentFactory meshComponentFactory)
    {
        _window = window;
        _camera = camera;
        _logger = logger;
        _logger.LogInformation("Creating OpenGLContext...");
        _meshComponentFactory = meshComponentFactory;
    }

    public GL OnLoad()
    {
        _gl = _window.CreateOpenGL();

        //Skybox VBO and VAO
        SkyBoxVbo = new BufferObject<float>(_gl, Skybox.Vertices, BufferTargetARB.ArrayBuffer);
        SkyBoxVao = new VertexArrayBufferObject<float, uint>(_gl, SkyBoxVbo);
        SkyBoxVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, 3, 0);
        _gl.BindVertexArray(0);

        Task<byte[]> skyboxRightTask = File.ReadAllBytesAsync("Assets/skybox/right.jpg");
        Task<byte[]> skyboxLeftTask = File.ReadAllBytesAsync("Assets/skybox/left.jpg");
        Task<byte[]> skyboxTopTask = File.ReadAllBytesAsync("Assets/skybox/top.jpg");
        Task<byte[]> skyboxBottomTask = File.ReadAllBytesAsync("Assets/skybox/bottom.jpg");
        Task<byte[]> skyboxFrontTask = File.ReadAllBytesAsync("Assets/skybox/front.jpg");
        Task<byte[]> skyboxBackTask = File.ReadAllBytesAsync("Assets/skybox/back.jpg");

        Task.WaitAll(skyboxRightTask,
                     skyboxLeftTask,
                     skyboxTopTask,
                     skyboxBottomTask,
                     skyboxFrontTask,
                     skyboxBackTask);

        CubeMapTexture = new CubeMapTexture(_gl, new ImageResult[]
        {
            ImageResult.FromMemory(skyboxRightTask.Result, ColorComponents.RedGreenBlue),
            ImageResult.FromMemory(skyboxLeftTask.Result, ColorComponents.RedGreenBlue),
            ImageResult.FromMemory(skyboxTopTask.Result, ColorComponents.RedGreenBlue),
            ImageResult.FromMemory(skyboxBottomTask.Result, ColorComponents.RedGreenBlue),
            ImageResult.FromMemory(skyboxFrontTask.Result, ColorComponents.RedGreenBlue),
            ImageResult.FromMemory(skyboxBackTask.Result, ColorComponents.RedGreenBlue)
        });

        SkyBoxShader = new Shader(_gl);
        Task<string> skyboxVertexShaderTask = File.ReadAllTextAsync("Shaders/6.1.skybox.vert");
        Task<string> skyboxFragmentShaderTask = File.ReadAllTextAsync("Shaders/6.1.skybox.frag");
        Task.WaitAll(skyboxVertexShaderTask, skyboxFragmentShaderTask);
        SkyBoxShader.LoadBy(_gl, skyboxVertexShaderTask.Result, skyboxFragmentShaderTask.Result);

        SkyBoxShader.UseBy(_gl);
        SkyBoxShader.SetUniformBy(_gl, "skybox", 0);


        //Telling the VAO object how to lay out the attribute pointers
        CubeEbo = new BufferObject<uint>(_gl, TexturedNormaledCube.Indices, BufferTargetARB.ElementArrayBuffer);
        CubeVbo = new BufferObject<float>(_gl, TexturedNormaledCube.Vertices, BufferTargetARB.ArrayBuffer);
        CubeVao = new VertexArrayBufferObject<float, uint>(_gl, CubeVbo, CubeEbo);
        CubeVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, TexturedNormaledCube.VerticeSize, 0);
        CubeVao.VertexAttributePointer(_gl, 1, 3, VertexAttribPointerType.Float, TexturedNormaledCube.VerticeSize, 3);
        CubeVao.VertexAttributePointer(_gl, 2, 2, VertexAttribPointerType.Float, TexturedNormaledCube.VerticeSize, 6);
        _gl.BindVertexArray(0);

        // 初始化二十面體 (Icosahedron) - 使用完整的頂點屬性
        IcosahedronEbo = new BufferObject<uint>(_gl, FullIcosahedron.Indices, BufferTargetARB.ElementArrayBuffer);
        IcosahedronVbo = new BufferObject<float>(_gl, FullIcosahedron.Vertices, BufferTargetARB.ArrayBuffer);
        IcosahedronVao = new VertexArrayBufferObject<float, uint>(_gl, IcosahedronVbo, IcosahedronEbo);
        
        // 設定所有頂點屬性指標以匹配 Vertex 結構 (17 floats total)
        IcosahedronVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 0);  // Position
        IcosahedronVao.VertexAttributePointer(_gl, 1, 3, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 3);  // Normal  
        IcosahedronVao.VertexAttributePointer(_gl, 2, 2, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 6);  // TexCoords
        IcosahedronVao.VertexAttributePointer(_gl, 3, 3, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 8);  // Tangent
        IcosahedronVao.VertexAttributePointer(_gl, 4, 3, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 11); // BiTangent
        IcosahedronVao.VertexAttributePointer(_gl, 5, 3, VertexAttribPointerType.Float, FullIcosahedron.VerticeSize, 14); // Color
        _gl.BindVertexArray(0);

        // 初始化十二面體 (Dodecahedron) - 使用完整的頂點屬性
        DodecahedronEbo = new BufferObject<uint>(_gl, Dodecahedron.Indices, BufferTargetARB.ElementArrayBuffer);
        DodecahedronVbo = new BufferObject<float>(_gl, Dodecahedron.Vertices, BufferTargetARB.ArrayBuffer);
        DodecahedronVao = new VertexArrayBufferObject<float, uint>(_gl, DodecahedronVbo, DodecahedronEbo);

        // 設定所有頂點屬性指標以匹配 Vertex 結構 (17 floats total)
        DodecahedronVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 0);  // Position
        DodecahedronVao.VertexAttributePointer(_gl, 1, 3, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 3);  // Normal  
        DodecahedronVao.VertexAttributePointer(_gl, 2, 2, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 6);  // TexCoords
        DodecahedronVao.VertexAttributePointer(_gl, 3, 3, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 8);  // Tangent
        DodecahedronVao.VertexAttributePointer(_gl, 4, 3, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 11); // BiTangent
        DodecahedronVao.VertexAttributePointer(_gl, 5, 3, VertexAttribPointerType.Float, Dodecahedron.VerticeSize, 14); // Color
        _gl.BindVertexArray(0);

        // 初始化四面體 (Tetrahedron) - 使用完整的頂點屬性
        TetrahedronEbo = new BufferObject<uint>(_gl, Tetrahedron.Indices, BufferTargetARB.ElementArrayBuffer);
        TetrahedronVbo = new BufferObject<float>(_gl, Tetrahedron.Vertices, BufferTargetARB.ArrayBuffer);
        TetrahedronVao = new VertexArrayBufferObject<float, uint>(_gl, TetrahedronVbo, TetrahedronEbo);

        // 設定所有頂點屬性指標以匹配 Vertex 結構 (17 floats total)
        TetrahedronVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 0);  // Position
        TetrahedronVao.VertexAttributePointer(_gl, 1, 3, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 3);  // Normal  
        TetrahedronVao.VertexAttributePointer(_gl, 2, 2, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 6);  // TexCoords
        TetrahedronVao.VertexAttributePointer(_gl, 3, 3, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 8);  // Tangent
        TetrahedronVao.VertexAttributePointer(_gl, 4, 3, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 11); // BiTangent
        TetrahedronVao.VertexAttributePointer(_gl, 5, 3, VertexAttribPointerType.Float, Tetrahedron.VerticeSize, 14); // Color
        _gl.BindVertexArray(0);

        // Initialize Sphere - using complete vertex attributes
        SphereEbo = new BufferObject<uint>(_gl, Sphere.Indices, BufferTargetARB.ElementArrayBuffer);
        SphereVbo = new BufferObject<float>(_gl, Sphere.Vertices, BufferTargetARB.ArrayBuffer);
        SphereVao = new VertexArrayBufferObject<float, uint>(_gl, SphereVbo, SphereEbo);

        // Set all vertex attribute pointers to match Vertex structure (17 floats total)
        SphereVao.VertexAttributePointer(_gl, 0, 3, VertexAttribPointerType.Float, Sphere.VerticeSize, 0);  // Position
        SphereVao.VertexAttributePointer(_gl, 1, 3, VertexAttribPointerType.Float, Sphere.VerticeSize, 3);  // Normal  
        SphereVao.VertexAttributePointer(_gl, 2, 2, VertexAttribPointerType.Float, Sphere.VerticeSize, 6);  // TexCoords
        SphereVao.VertexAttributePointer(_gl, 3, 3, VertexAttribPointerType.Float, Sphere.VerticeSize, 8);  // Tangent
        SphereVao.VertexAttributePointer(_gl, 4, 3, VertexAttribPointerType.Float, Sphere.VerticeSize, 11); // BiTangent
        SphereVao.VertexAttributePointer(_gl, 5, 3, VertexAttribPointerType.Float, Sphere.VerticeSize, 14); // Color
        _gl.BindVertexArray(0);

        ////for sceen
        QuadVbo = new BufferObject<float>(_gl, Quad.Vertices, BufferTargetARB.ArrayBuffer);
        QuadVao = new VertexArrayBufferObject<float, uint>(_gl, QuadVbo);
        QuadVao.VertexAttributePointer(_gl, 0, 2, VertexAttribPointerType.Float, Quad.VerticeSize, 0);
        QuadVao.VertexAttributePointer(_gl, 1, 2, VertexAttribPointerType.Float, Quad.VerticeSize, 2);
        // 解除所有 VAO 綁定，確保乾淨的初始狀態
        _gl.BindVertexArray(0);
        
        //The Lamp shader uses a fragment shader that just colors it solid white so that we know it is the light source
        ScreenShader = new Shader(_gl);
        LightingShader = new Shader(_gl);
        LampShader = new Shader(_gl); ;

        Task<string> screenVertexShaderTask = File.ReadAllTextAsync("Shaders/screen.vert");
        Task<string> screenFragmentShaderTask = File.ReadAllTextAsync("Shaders/screen.frag");
        Task<string> lightingVertexShaderTask = File.ReadAllTextAsync("Shaders/basic.vert");
        Task<string> lightingFragmentShaderTask = File.ReadAllTextAsync("Shaders/material.frag");
        Task<string> lampVertexShaderTask = File.ReadAllTextAsync("Shaders/basic.vert");
        Task<string> lampFragmentShaderTask = File.ReadAllTextAsync("Shaders/white.frag");

        Task.WaitAll(screenVertexShaderTask,
                     screenFragmentShaderTask,
                     lightingVertexShaderTask,
                     lightingFragmentShaderTask,
                     lampVertexShaderTask,
                     lampFragmentShaderTask);
        
        ScreenShader.LoadBy(_gl, screenVertexShaderTask.Result, screenFragmentShaderTask.Result);
        LightingShader.LoadBy(_gl, lightingVertexShaderTask.Result, lightingFragmentShaderTask.Result);
        LampShader.LoadBy(_gl, lampVertexShaderTask.Result, lampFragmentShaderTask.Result);

        Task<Image> diffuseTextureTask = Image.LoadAsync("Textures/silkBoxed.png");
        Task<Image> specularTextureTask = Image.LoadAsync("Textures/silkSpecular.png");
        Task.WaitAll(diffuseTextureTask, specularTextureTask);
        diffuseTextureTask.Result.Mutate(x => x.Flip(FlipMode.Vertical));
        specularTextureTask.Result.Mutate(x => x.Flip(FlipMode.Vertical));
        DiffuseMap = new Textures.Texture(_gl, diffuseTextureTask.Result);
        SpecularMap = new Textures.Texture(_gl, specularTextureTask.Result);

        ScreenShader.UseBy(_gl);
        ScreenShader.SetUniformBy(_gl, "screenTexture", 2);

        Fbo = new FrameBufferObject(_gl);
        Fbt = new FrameBufferTexture(_gl, (uint)_window.Size.X, (uint)_window.Size.Y);
        Rbo = new RenderBufferObject(_gl, (uint)_window.Size.X, (uint)_window.Size.Y);

        if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
        {
            _logger.LogError("ERROR::FRAMEBUFFER:: Framebuffer is not complete!");
        }
        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);

        // draw as wireframe
        //_gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);

        #region Mesh

        _logger.LogInformation("Loading MeshComponent...");

        //Load the mesh component with a glTF model of an avocado
        MeshComponent = _meshComponentFactory.LoadModel(_gl, "Assets/avocado/avocado.gltf");

        //Load the mesh shaders
        _logger.LogInformation("Reading Mesh Shaders...");
        Task<string> meshVertexShaderTask = File.ReadAllTextAsync("Shaders/avocado_debug.vert");
        Task<string> meshFragmentShaderTask = File.ReadAllTextAsync("Shaders/avocado_debug2.frag");

        Task.WaitAll(meshVertexShaderTask, meshFragmentShaderTask);

        //Create the mesh shaders
        _logger.LogInformation("Loading Mesh Shaders...");
        for (int i = 0; i < MeshComponent.Meshes.Count; i++)
        {
            _logger.LogInformation("Loading MeshShader {Index}...", i + 1);
            MeshShaders[i] = new Shader(_gl);
            MeshShaders[i].LoadBy(_gl, meshVertexShaderTask.Result, meshFragmentShaderTask.Result);
        }
        #endregion
        return _gl;
    }

    public Task<GL> OnLoadAsync()
    {
        throw new NotImplementedException();
    }
    private void DrawSkyBox()
    {
        _logger.LogInformation("Drawing SkyBox...");
        
        // Change depth function so depth test passes when values are equal to depth buffer's content
        _gl.DepthFunc(DepthFunction.Lequal);
        
        SkyBoxShader.UseBy(_gl);
        
        // Remove translation from view matrix - keep only rotation
        var view = _camera.GetViewMatrix();
        var viewNoTranslation = new Matrix4x4(
            view.M11, view.M12, view.M13, 0.0f,
            view.M21, view.M22, view.M23, 0.0f,
            view.M31, view.M32, view.M33, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        
        SkyBoxShader.SetUniformBy(_gl, "view", viewNoTranslation);
        SkyBoxShader.SetUniformBy(_gl, "projection", _camera.GetProjectionMatrix());
        
        // Skybox cube
        SkyBoxVao.BindBy(_gl);
        _gl.ActiveTexture(TextureUnit.Texture0);
        CubeMapTexture.BindBy(_gl, TextureUnit.Texture0);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        
        // Set depth function back to default
        _gl.DepthFunc(DepthFunction.Less);
        
        // Unbind skybox VAO to avoid conflicts
        _gl.BindVertexArray(0);
    }

    private void DrawMesh()
    {
        //MeshComponent.Draw(_gl, new Shader[] { MeshShader1, MeshShader2, MeshShader3}, _camera, _lampPosition);
        _logger.LogInformation("Drawing MeshComponent with MeshShaders...");
        _logger.LogInformation("MeshComponent Meshes Count: {Count}", MeshComponent.Meshes.Count);
        //MeshComponent.Draw(_gl, MeshShaders.AsSpan()[..MeshComponent.Meshes.Count], _camera, _lampPosition);
        MeshComponent.DrawWithoutTexture(_gl, MeshShaders.AsSpan()[..MeshComponent.Meshes.Count], _camera, _lampPosition);
    }
    private unsafe void RenderScene(double dt)
    {
        DiffuseMap.BindBy(_gl, TextureUnit.Texture0);
        SpecularMap.BindBy(_gl, TextureUnit.Texture1);

        float difference = (float)(_window.Time * 100);

        //Slightly rotate the cube to give it an angled face to look at
        LightingShader.UseBy(_gl);

        Matrix4x4 shearMatrix = new Matrix4x4
        (
            1, 0, 0, 0,
            0.5f, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        var model = Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference)) * shearMatrix;

        LightingShader.SetUniformBy(_gl, "uModel", model);
        LightingShader.SetUniformBy(_gl, "uView", _camera.GetViewMatrix());
        LightingShader.SetUniformBy(_gl, "uProjection", _camera.GetProjectionMatrix());
        LightingShader.SetUniformBy(_gl, "viewPos", _camera.Position);
        LightingShader.SetUniformBy(_gl, "material.diffuse", 0);
        //Specular is set to 1 because our diffuseMap is bound to Texture1
        LightingShader.SetUniformBy(_gl, "material.specular", 1);
        LightingShader.SetUniformBy(_gl, "material.shininess", 32.0f);

        var diffuseColor = new Vector3(0.5f);
        var ambientColor = diffuseColor * new Vector3(0.2f);

        LightingShader.SetUniformBy(_gl, "light.ambient", ambientColor);
        LightingShader.SetUniformBy(_gl, "light.diffuse", diffuseColor); // darkened
        LightingShader.SetUniformBy(_gl, "light.specular", new Vector3(1.0f, 1.0f, 1.0f));
        LightingShader.SetUniformBy(_gl, "light.position", _lampPosition);

        //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
        CubeVao.BindBy(_gl);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)TexturedNormaledCube.Vertices.Length / TexturedNormaledCube.VerticeSize);

        // 渲染二十面體 (Icosahedron) - 位置稍微偏移以避免與立方體重疊
        var icosahedronModel = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference * 0.5f)) 
                               * Matrix4x4.CreateTranslation(new Vector3(3f, 0.0f, 0.0f))
                               * Matrix4x4.CreateScale(0.5f);
        LightingShader.SetUniformBy(_gl, "uModel", icosahedronModel);
        
        IcosahedronVao.BindBy(_gl);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)FullIcosahedron.Indices.Length, DrawElementsType.UnsignedInt, (void*)0);

        // 渲染四面體 (Tetrahedron) - 位置在上方，快速旋轉
        var tetrahedronModel = Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference * 1.2f))
                              * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference * 1.5f))
                              * Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(difference * 0.8f))
                              * Matrix4x4.CreateTranslation(new Vector3(0.0f, -5f, 0.0f))
                              * Matrix4x4.CreateScale(0.3f);
        LightingShader.SetUniformBy(_gl, "uModel", tetrahedronModel);
        TetrahedronVao.BindBy(_gl);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)Tetrahedron.Indices.Length, DrawElementsType.UnsignedInt, (void*)0);

        // Render Sphere - positioned to the left, slow rotation
        var sphereModel = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference * 0.4f))
                         * Matrix4x4.CreateTranslation(new Vector3(-3.0f, -2.0f, 0.0f))
                         * Matrix4x4.CreateScale(0.8f);
        LightingShader.SetUniformBy(_gl, "uModel", sphereModel);
        SphereVao.BindBy(_gl);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)Sphere.Indices.Length, DrawElementsType.UnsignedInt, (void*)0);

        //// 渲染十二面體 (Dodecahedron) - 位置偏移並稍微縮小
        //var dodecahedronModel = Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference * 0.3f))
        //                       * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference * 0.7f))
        //                       * Matrix4x4.CreateTranslation(new Vector3(-3.0f, 0.0f, 0.0f))
        //                       * Matrix4x4.CreateScale(0.4f);
        //LightingShader.SetUniformBy(_gl, "uModel", dodecahedronModel);
        //DodecahedronVao.BindBy(_gl);
        //_gl.DrawElements(PrimitiveType.Triangles, (uint)Dodecahedron.Indices.Length, DrawElementsType.UnsignedInt, (void*)0);

        // 切換到燈光著色器並重新綁定立方體 VAO 用於燈光渲染
        LampShader.UseBy(_gl);

        var lampMatrix = Matrix4x4.Identity
                         * Matrix4x4.CreateScale(0.2f)
                         * Matrix4x4.CreateTranslation(_lampPosition);
        //* Matrix4x4.CreateTranslation(new Vector3(1.2f, 1.0f, 2.0f));

        LampShader.SetUniformBy(_gl, "uModel", lampMatrix);
        LampShader.SetUniformBy(_gl, "uView", _camera.GetViewMatrix());
        LampShader.SetUniformBy(_gl, "uProjection", _camera.GetProjectionMatrix());

        // 重新綁定立方體 VAO 用於燈光渲染
        CubeVao.BindBy(_gl);
        //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        _gl.BindVertexArray(0);

        #region mesh
        DrawMesh();
        #endregion

        // Draw skybox as the last geometry (draw as last to prevent overdraw)
        //DrawSkyBox();

        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            _logger.LogError("OpenGL Error: {Error}", error);
        }
    }

    private void Reset()
    {
        _gl.Enable(EnableCap.DepthTest);
        _gl.ClearColor(new Vector4D<float>(0.1f, 0.1f, 0.1f, 1.0f));
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }
    public void OnRender(double dt)
    {
        Time += (float)dt;
        Fbo.BindBy(_gl);
        Reset();
        RenderScene(dt);
        // now bind back to default framebuffer and draw a quad plane with the attached framebuffer color texture
        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        
        OnPostProcessing();
    }
    //public void OnRender(double dt)
    //{
    //    Reset();
    //    DrawMesh();
    //}

    private void OnPostProcessing()
    {
        // disable depth test so screen-space quad isn't discarded due to depth test.
        _gl.Disable(EnableCap.DepthTest);
        // clear all relevant buffers
        // set clear color to white (not really necessary actually, since we won't be able to see behind the quad anyways)
        _gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        _gl.Clear((uint)GLEnum.ColorBufferBit);
        Fbt.BindBy(_gl, TextureUnit.Texture2);
        ScreenShader.UseBy(_gl);
        QuadVao.BindBy(_gl);
 
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    public void OnStop()
    {
        throw new NotImplementedException();
    }

    //void OnLoad()
    //{
    //    throw new NotImplementedException();
    //}
   
    public void OnUpdate(double dt)
    {

    }

    public void OnWindowFrameBufferResize(Vector2D<int> resize)
    {
        _gl.Viewport(resize);
    }

    private void OnDispose()
    {
        _logger.LogInformation("OpenGLContext Disposing...");
        
        // Clean up skybox resources
        SkyBoxVbo.DisposeBy(_gl);
        SkyBoxVao.DisposeBy(_gl);
        CubeMapTexture.DisposeBy(_gl);
        SkyBoxShader.DisposeBy(_gl);
        
        QuadVbo.DisposeBy(_gl);
        QuadVao.DisposeBy(_gl);
        CubeVbo.DisposeBy(_gl);
        CubeEbo.DisposeBy(_gl);
        CubeVao.DisposeBy(_gl);
        
        // 清理二十面體資源
        IcosahedronVbo.DisposeBy(_gl);
        IcosahedronEbo.DisposeBy(_gl);
        IcosahedronVao.DisposeBy(_gl);

        // 清理十二面體資源
        DodecahedronVbo.DisposeBy(_gl);
        DodecahedronEbo.DisposeBy(_gl);
        DodecahedronVao.DisposeBy(_gl);
        
        // 清理四面體資源
        TetrahedronVbo.DisposeBy(_gl);
        TetrahedronEbo.DisposeBy(_gl);
        TetrahedronVao.DisposeBy(_gl);
        
        // Clean up sphere resources
        SphereVbo.DisposeBy(_gl);
        SphereEbo.DisposeBy(_gl);
        SphereVao.DisposeBy(_gl);

        ScreenShader.DisposeBy(_gl);
        LampShader.DisposeBy(_gl);
        LightingShader.DisposeBy(_gl);
        DiffuseMap.DisposeBy(_gl);
        SpecularMap.DisposeBy(_gl);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposedValue)
        {
            if (disposing)
            {
                OnDispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
        _logger.LogInformation("OpenGLContext Already Disposed...");
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~OpenGLContext()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    void ISystem.OnLoad()
    {
        throw new NotImplementedException();
    }
}
