using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace ComputerGraphics1_opentk_tomography;

// TODO: переписать это на нормальный API с шейдерами. Immediate mode сейчас уже никто не использует и он Deprecated:
// https://stackoverflow.com/questions/6733934/what-does-immediate-mode-mean-in-opengl
public class View : GameWindow
{
    public delegate Color TransferFunction(short value);

    private int _layerIndex;
    private readonly BinTomography _bin;
    private readonly TransferFunction _transferFunction;
    private Timer _fpsTimer;

    private class FpsLock
    {
        public int FpsCount { get; set; }
    }

    private FpsLock _fpsLock;
    public View(BinTomography bin, string name, int width = 800, int height = 600,
        TransferFunction? transferFunction = null
    ) : base(GameWindowSettings.Default,
        new NativeWindowSettings()
        {
            Size = (width, height),
            Title = $"Tomography View: ${name}",
            // В лабе используется древний immediate режим отрисовки. 
            // Сейчас так никто не делает. Без этой строки не работает
            Profile = ContextProfile.Compatability
        }
    )
    {
        _bin = bin;
        _fpsLock = new FpsLock();
        if (transferFunction == null)
        {
            transferFunction = (value) =>
            {
                int min = 0;
                int max = 2000;
                int newVal = Math.Clamp((value - min) * 255 / (max - min), 0, 255);
                return Color.FromArgb(newVal, newVal, newVal);
            };
        }
        _transferFunction = transferFunction;
        _layerIndex = 0;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        DrawQuads(_layerIndex);
        SwapBuffers();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ShadeModel(ShadingModel.Smooth);
        //GL.ClearColor(0f, 0f, 0f, 0f);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.Ortho(0, _bin.XSize, 0, _bin.YSize, -1, 1);
        GL.Viewport(0, 0, this.Size.X, this.Size.Y);
        DrawQuads(3);
        
        _fpsTimer = new Timer(state =>
        {
            lock (_fpsLock)
            {
                Console.WriteLine(_fpsLock.FpsCount);
                _fpsLock.FpsCount = 0;
            }
        }, null, 1000, 1000);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, this.Size.X, this.Size.Y);
    }

    private void DrawQuads(int layer)
    {
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Begin(PrimitiveType.Quads);
        for (int x = 0; x < _bin.XSize - 1; x++)
        {
            for (int y = 0; y < _bin.YSize-1; y++)
            {
                short value;
                value = _bin[x, y, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x, y);
                
                value = _bin[x, y+1, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x, y+1);
                
                value = _bin[x + 1, y + 1, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x + 1, y + 1);
                
                value = _bin[x + 1, y, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x + 1, y);
                
                
            }
        }

        GL.End();
    }

    private uint _keyCooldown = 0;
    private const uint MaxKeyCooldown = 1;
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        KeyboardState keyboard = KeyboardState;
        if (keyboard.IsKeyDown(Keys.Up))
        {
            if (_keyCooldown == 0)
            {
                _keyCooldown = MaxKeyCooldown;
                _layerIndex = Math.Clamp(_layerIndex + 1, 0, _bin.ZSize-1);
            }
            else _keyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.Up))
        {
            _keyCooldown = 0;
        }
        else if (keyboard.IsKeyDown(Keys.Down))
        {
            if (_keyCooldown == 0)
            {
                _keyCooldown = MaxKeyCooldown;
                _layerIndex = Math.Clamp(_layerIndex - 1, 0, _bin.ZSize-1);
            }
            else _keyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _keyCooldown = 0;
        }

        lock (_fpsLock)
        {
            _fpsLock.FpsCount++;
        }
    }
}