using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Graphics.OpenGL.ErrorCode;
using Color = System.Drawing.Color;
using Image = SixLabors.ImageSharp.Image;

namespace ComputerGraphics1_opentk_tomography;

// TODO: переписать это на нормальный API с шейдерами. Immediate mode сейчас уже никто не использует и он Deprecated:
// https://stackoverflow.com/questions/6733934/what-does-immediate-mode-mean-in-opengl
public class View : GameWindow
{
    private Image<Rgba32> image;
    private int _VBOTexture;

    private int _iconsTexture;
    private byte[] _iconsTexBytes;
    private Image<Rgba32> icons;

    private int transferMin = 0;
    private int transferMax = 2000;
    
    public delegate Color TransferFunction(short value);

    private float ScaleFactorX { get; set; }
    private float ScaleFactorY { get; set; }
    private int _layerIndex;
    private readonly BinTomography _bin;
    private readonly TransferFunction _transferFunction;
    private Timer _fpsTimer;
    private bool needsReload = true;
    private class FpsLock
    {
        public int FpsCount { get; set; }
        public bool Redraw { get; set; }
    }

    private DrawMode _mode;
    
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
                int newVal = Math.Clamp((value - transferMin) * 255 / (transferMax - transferMin), 0, 255);
                return System.Drawing.Color.FromArgb(newVal, newVal, newVal);
            };
        }
        _transferFunction = transferFunction;
        LayerIndex = 0;
        icons = Image.Load<Rgba32>("icons.png");
        _iconsTexBytes = new byte[4 * icons.Width * icons.Height];
        icons.CopyPixelDataTo(_iconsTexBytes);
        _mode = DrawMode.Quads;
        ScaleFactorX = (float)_bin.XSize / Size.X;
        ScaleFactorY = (float)_bin.YSize / Size.Y;
    }

    private int cachedFps = 0;
    private int LayerIndex
    {
        get => _layerIndex;
        set
        {
            needsReload = true;
            _layerIndex = value;
        }
    }

    void GenerateTextureImage(int layer)
    {
        image = new Image<Rgba32>(_bin.XSize, _bin.YSize);
        for (int i = 0; i < _bin.XSize; ++i)
        {
            for (int j = 0; j < _bin.YSize; j++)
            {
                var pixel = _transferFunction(_bin[i, j, layer]);
                image[i, j] = new Rgba32(pixel.R, pixel.G, pixel.B);
            }
        }
    }
    void Load2DTexture()
    {
        var data = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(data);
        GL.BindTexture(TextureTarget.Texture2D, _VBOTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
            image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);

        ErrorCode e = GL.GetError();
        if (e != ErrorCode.NoError)
        {
            Console.Error.WriteLine(e.ToString());
        }
        
    }
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        switch (_mode)
        {
            case DrawMode.Quads:
                DrawQuads(_layerIndex);
                break;
            case DrawMode.Texture:
                if (needsReload)
                {
                    GenerateTextureImage(LayerIndex);
                    Load2DTexture();
                    needsReload = false;
                }
                DrawTexture();
                break;
            case DrawMode.QuadStrip:
                DrawQuadStrips(_layerIndex);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        TextUtility.Draw(-30, Size.Y + 20, cachedFps.ToString(), 32, ScaleFactorX, ScaleFactorY);
        DrawModeIcon();
        lock (_fpsLock)
        {
            if (_fpsLock.Redraw)
            {
                cachedFps = _fpsLock.FpsCount;
                Console.WriteLine(_fpsLock.FpsCount);
                _fpsLock.FpsCount = 0;
                _fpsLock.Redraw = false;
            }
        }
        SwapBuffers();
    }

    private void DrawModeIcon()
    {
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, _iconsTexture);

        GL.Begin(PrimitiveType.Quads);
        GL.Color3(Color.White);
        
        GL.TexCoord2(0f + 0.25f * ((int) _mode), 0f);
        GL.Vertex2(-50, -50);
        GL.TexCoord2(0.25f + 0.25f * ((int) _mode), 0f);
        GL.Vertex2(80 * ScaleFactorX - 50, -50);
        GL.TexCoord2(0.25f + 0.25f * ((int) _mode), 1f);
        GL.Vertex2(80 * ScaleFactorX - 50, 80 * ScaleFactorY - 50);
        GL.TexCoord2(0f + 0.25f * ((int) _mode), 1f);
        GL.Vertex2(-50, 80 * ScaleFactorY - 50);
        
        GL.End();

        GL.Disable(EnableCap.Texture2D);
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        _iconsTexture = GL.GenTexture();
        _VBOTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _iconsTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
            icons.Width, icons.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, _iconsTexBytes);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        GL.ShadeModel(ShadingModel.Smooth);
        //GL.ClearColor(0f, 0f, 0f, 0f);
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.Ortho(-50, _bin.XSize + 50, -50, _bin.YSize + 50, -1, 1);
        GL.Viewport(0, 0, this.Size.X, this.Size.Y);
        //DrawQuads(3);
        
        _fpsTimer = new Timer(state =>
        {
            lock (_fpsLock)
            {
                _fpsLock.Redraw = true;
            }
        }, null, 1000, 1000);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, this.Size.X, this.Size.Y);
        ScaleFactorX = (float)_bin.XSize / Size.X;
        ScaleFactorY = (float)_bin.YSize / Size.Y;
    }

    private void DrawTexture()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, _VBOTexture);
        
        GL.Begin(PrimitiveType.Quads);
        GL.Color3(Color.White);
        GL.TexCoord2(0f, 0f);
        GL.Vertex2(0, 0);
        GL.TexCoord2(0f, 1f);
        GL.Vertex2(0, _bin.YSize);
        GL.TexCoord2(1f, 1f);
        GL.Vertex2(_bin.XSize, _bin.YSize);
        GL.TexCoord2(1f, 0f);
        GL.Vertex2(_bin.XSize, 0);
        GL.End();
        
        GL.Disable(EnableCap.Texture2D);
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
    
    private void DrawQuadStrips(int layer)
    {
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        for (int x = 0; x < _bin.XSize - 1; x++)
        {
            GL.Begin(PrimitiveType.QuadStrip);
            for (int y = 0; y < _bin.YSize; y++)
            {
                short value;
                value = _bin[x, y, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x, y);

                value = _bin[x + 1, y, layer];
                GL.Color3(_transferFunction(value));
                GL.Vertex2(x + 1, y);
            }
            GL.End();
        }

    }

    private uint _keyCooldown = 0;
    private uint _maxKeyCooldown = 1;
    
    private uint _TFKeyCooldown = 0;
    private uint _maxTFKeyCooldown = 0;
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        KeyboardState keyboard = KeyboardState;
        if (keyboard.IsKeyDown(Keys.Up))
        {
            if (_keyCooldown == 0)
            {
                _keyCooldown = _maxKeyCooldown;
                LayerIndex = Math.Clamp(LayerIndex + 1, 0, _bin.ZSize-1);
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
                _keyCooldown = _maxKeyCooldown;
                LayerIndex = Math.Clamp(LayerIndex - 1, 0, _bin.ZSize-1);
            }
            else _keyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.Down))
        {
            _keyCooldown = 0;
        }

        if (keyboard.IsKeyReleased(Keys.D1))
        {
            _maxKeyCooldown = 1;
            _mode = DrawMode.Quads;
        }
        else if (keyboard.IsKeyReleased(Keys.D2))
        {
            _maxKeyCooldown = 1000;
            _mode = DrawMode.Texture;
        }
        else if (keyboard.IsKeyReleased(Keys.D3))
        {
            _maxKeyCooldown = 2;
            _mode = DrawMode.QuadStrip;
        }
        lock (_fpsLock)
        {
            _fpsLock.FpsCount++;
        }
        
        if (keyboard.IsKeyDown(Keys.KeyPadAdd))
        {
            if (_TFKeyCooldown == 0)
            {
                _TFKeyCooldown = _maxTFKeyCooldown;
                transferMin = Math.Clamp(transferMin+1, 0, transferMax);
                needsReload = true;
            }
            else _TFKeyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.KeyPadAdd))
        {
            _TFKeyCooldown = 0;
        }
        else if (keyboard.IsKeyDown(Keys.KeyPadSubtract))
        {
            if (_TFKeyCooldown == 0)
            {
                _TFKeyCooldown = _maxTFKeyCooldown;
                transferMin = Math.Clamp(transferMin-1, 0, transferMax);
                needsReload = true;
            }
            else _TFKeyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.KeyPadSubtract))
        {
            _TFKeyCooldown = 0;
        }
        
        if (keyboard.IsKeyDown(Keys.Z))
        {
            if (_TFKeyCooldown == 0)
            {
                _TFKeyCooldown = _maxTFKeyCooldown;
                transferMax = Math.Clamp(transferMax+1, transferMin, 10000);
                needsReload = true;
            }
            else _TFKeyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.Z))
        {
            _TFKeyCooldown = 0;
        }
        else if (keyboard.IsKeyDown(Keys.X))
        {
            if (_TFKeyCooldown == 0)
            {
                _TFKeyCooldown = _maxTFKeyCooldown;
                transferMax = Math.Clamp(transferMax-1, transferMin, 10000);
                needsReload = true;
            }
            else _TFKeyCooldown--;
        }
        else if (keyboard.IsKeyReleased(Keys.X))
        {
            _TFKeyCooldown = 0;
        }
        
    }
}

internal enum DrawMode
{
    Quads = 0,
    Texture = 1,
    QuadStrip = 2
}