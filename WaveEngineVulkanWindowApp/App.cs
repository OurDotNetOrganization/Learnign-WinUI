using System.Diagnostics;
using System.Reflection;
using WaveEngineDotNetLibrary;
using WaveEngineDotNetLibrary.Vulkan;
using WaveEngineDotNetLibrary.Window;

namespace WaveEngineVulkanWindowApp;

internal class App
{
    private readonly Form _window;
    private readonly Control _control;
    private readonly IVkSurface _vkWindowSurface;
    private readonly VkContext _vkContext;
    const uint WIDTH = 800;
    const uint HEIGHT = 600;

    public App()
    {
        _window = new Form()
        {
            Text = "Vulkan Triangle Rasterization",
            Size = new Size((int)WIDTH, (int)HEIGHT),
            FormBorderStyle = FormBorderStyle.FixedToolWindow,            
        };
        _control = new Control()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
        };

        //Add MenuStrip

        _window.MainMenuStrip = new MenuStrip();
        _window.Controls.Add(_window.MainMenuStrip);
        _window.MainMenuStrip.Items.Add("File");
        _window.MainMenuStrip.Items.Add("Edit");
        _window.MainMenuStrip.Items.Add("View");

        IntPtr hInstance = Process.GetCurrentProcess().Handle;
        _vkWindowSurface = new VkWindowSurface(new WindowHandle(_window.Handle, hInstance));
        _vkContext = new VkContext(WIDTH, HEIGHT, _vkWindowSurface);
    }

    public void Run()
    {
        SubscribeToControlEvent();
        _window.Show();

        Task<byte[]> loadVertShaderCode = File.ReadAllBytesAsync($"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Shaders/vert.spv");
        Task<byte[]> loadFragShaderCode = File.ReadAllBytesAsync($"{System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/Shaders/frag.spv");

        Task.WaitAll(loadVertShaderCode, loadFragShaderCode);

        _vkContext.Init(loadVertShaderCode.Result, loadFragShaderCode.Result);
        
        MainLoop();
        CleanUp();
    }

    public IntPtr SubscribeToControlEvent()
    {
        //control.Resize += NativeWindow_Resize;
        _control.MouseWheel += NativeWindow_MouseWheel;
        _control.MouseMove += NativeWindow_MouseMove;
        _control.MouseDown += NativeWindow_MouseDown;
        //NativeWindow.KeyDown += NativeWindow_KeyDown;
        return _control.Handle;
    }

    private void MainLoop()
    {
        bool isClosing = false;
        _window.FormClosing += (s, e) =>
        {
            isClosing = true;
        };

        while (!isClosing)
        {
                    
            Application.DoEvents(); //Applicaiton event
            _vkContext.DrawFrame();
        }

        _vkContext.CheckDeviceWaitIdleError();
    }
    private void NativeWindow_MouseWheel(object? sender, MouseEventArgs e)
    {
    }
    private void NativeWindow_MouseMove(object? sender, MouseEventArgs e)
    {

    }
    private void NativeWindow_MouseDown(object? sender, MouseEventArgs e)
    {

    }
    private void CleanUp()
    {
        _vkContext.CleanUp();
        _window.Dispose();
        _window.Close();
    }
}
