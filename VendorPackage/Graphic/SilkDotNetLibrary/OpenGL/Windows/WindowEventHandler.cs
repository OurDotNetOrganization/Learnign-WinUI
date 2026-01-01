using Microsoft.Extensions.Logging;
using SharedLibrary.Event.Handler;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SilkDotNetLibrary.OpenGL.Windows;

public class WindowEventHandler : IWindowEventHandler
{
    protected bool _disposedValue;
    protected GL GL { get; set; }
    protected readonly OpenGLContext _openGLContext;
    protected readonly IEventHandler _eventHandler;
    protected readonly IReadOnlyDictionary<Key, string> _keyBoardKeyMap;
    protected readonly ILogger<WindowEventHandler> _logger;
    protected readonly IWindow _window;
    protected IKeyboard PrimaryKeyboard { get; set; }
    protected IInputContext InputContext { get; set; }
    protected ImGuiController ImGuiController { get; set; }
    protected Vector2 LastMousePosition { get; set; }

    //protected EventHandler<>
    public WindowEventHandler(IWindow window,
                              OpenGLContext openGLContext,
                              IEventHandler eventHandler,
                              ILogger<WindowEventHandler> logger)
    {
        _window = window;
        _openGLContext = openGLContext;
        _eventHandler = eventHandler;
        _keyBoardKeyMap = new Dictionary<Key, string>
        {
            {Key.Escape, "Escape" },
            {Key.Space, "Space" },
            {Key.Enter, "Enter" },
            {Key.A, "A" },
            {Key.B, "B" },
            {Key.C, "C" },
            {Key.D, "D" },
            {Key.E, "E" },
            {Key.F, "F" },
            {Key.G, "G" },
            {Key.H, "H" },
            {Key.I, "I" },
            {Key.J, "J" },
            {Key.K, "K" },
            {Key.L, "L" },
            {Key.M, "M" },
            {Key.N, "N" },
            {Key.O, "O" },
            {Key.P, "P" },
            {Key.Q, "Q" },
            {Key.R, "R" },
            {Key.S, "S" },
            {Key.T, "T" },
            {Key.U, "U" },
            {Key.V, "V" },
            {Key.W, "W" },
            {Key.X, "X" },
            {Key.Y, "Y" },
            {Key.Z, "Z" },

        };
        _logger = logger;
    }

    public virtual Task Start(CancellationToken cancellationToken = default)
    {
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnFrameBufferResize;
        return Task.Run(() =>
        {
            _window?.Run();
        }, cancellationToken);
    }

    public virtual void Start()
    {
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnFrameBufferResize;
        _window?.Run();
    }
    public virtual Task Stop(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Window Closing...");
        return Task.Run(() =>
        {
            _window?.Close();
        }, cancellationToken);
    }

    public virtual void Stop()
    {
        _logger.LogInformation("Window Closing...");
        _window?.Close();
    }
    
    public void OnLoad()
    {
        InputContext = _window.CreateInput();
        foreach (IKeyboard inputContextKeyboard in InputContext.Keyboards)
        {
            PrimaryKeyboard = inputContextKeyboard;
            inputContextKeyboard.KeyDown += OnKeyDown;
            break;
        }
        foreach (IMouse mice in InputContext.Mice)
        {
            //InputContext.Mice[i].Cursor.CursorMode = CursorMode.Normal;
            mice.MouseMove += OnMouseMove;
            mice.Scroll += OnMouseWheel;
        }
        GL = _openGLContext.OnLoad();
        ImGuiController = new ImGuiController(GL, _window, InputContext);
    }

    public void OnRender(double dt)
    {
        _openGLContext.OnRender(dt);
        ImGuiNET.ImGui.ShowDemoWindow();
        ImGuiController.Render();
    }

    public void OnStop()
    {

    }

    public void OnFrameBufferResize(Vector2D<int> resize)
    {
        _eventHandler.OnWindowFrameBufferResizeHandler(new Vector2(resize.X, resize.Y));
        _openGLContext.OnWindowFrameBufferResize(resize);
    }

    public void OnUpdate(double dt)
    {
        foreach ((Key key, string value) in _keyBoardKeyMap)
        {
            if (PrimaryKeyboard.IsKeyPressed(key))
            {
                _eventHandler.OnKeyBoardKeyDownHandler(value);
            }
        }
        _eventHandler.OnWindowUpdateUpdateHandler(dt);
        ImGuiController.Update((float)dt);
        _openGLContext.OnUpdate(dt);

    }
    public virtual void OnClosing()
    {
        //trigger by closing Window
        //mabye a onclose for this?
        //Window.Close();
        //ImGuiController?.Dispose();
        //_openGLContext?.Dispose();
        //Window?.Dispose();
        //Window = null;
        //Window.Close();
        Dispose();
    }

    public virtual void OnKeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        //Log.Information("Escpae Key Pressed...");
        //if(_keyBoardKeyMap.TryGetValue(arg2, out string keycode))
        //{
        //    _eventHandler.OnKeyBoardKeyDownHandler(new SharedLibrary.Event.EventArgs.KeyBoardKeyDownEventArgs()
        //    {
        //        KeyCode = keycode
        //    });
        //}

        if (arg2 == Key.Escape)
        {
            OnClosing();
        }
    }
    public void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (LastMousePosition == default)
        { 
            LastMousePosition = position; 
        }
        else
        {
            _eventHandler.OnMouseMoveHandler((LastMousePosition, position));
            LastMousePosition = position;
        }
    }

    public void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        _eventHandler.OnMouseScrollWheelHandler((scrollWheel.X, scrollWheel.Y));
    }

    protected virtual void OnDispose()
    {
        _logger.LogInformation("ImGui Disposing...");
        ImGuiController?.Dispose();
        _logger.LogInformation("Input Disposing...");
        InputContext?.Dispose();
        _logger.LogInformation("GL Disposing...");
        _openGLContext.Dispose();
        _logger.LogInformation("Window Disposing...");
        //_logger.LogInformation("Window Disposes...");
        //if (Window is not null)
        //{
        //    Window.Dispose();
        //    Log.Information("Window Disposed...");
        //}
        //else
        //{
        //    Log.Information("Window could not be found...");
        //}
        //_window.Dispose();
        //_logger.LogInformation("Window Disposed...");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                OnDispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
        _logger.LogInformation("WindowEventHandler Already Disposed...");
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~WindowEventHandler()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        _logger.LogInformation("WindowEventHandler Disposing...");
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
