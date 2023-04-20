using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace ComputerGraphics1_opentk_tomography;

public static class TextUtility
{
    private static int _fontTexture = -1;

    private static List<int> _charVaos;
    private static Image<Rgba32> _font;
    public static Vector2i WindowSize { get; set; } = Vector2i.One;
    public static void Draw(int x, int y, string text, int height)
    {
        const string availableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}\\|;:'\".,<>/?`~ ";
        if (_fontTexture == -1)
        {
            _fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
            _font = Image.Load<Rgba32>("font.png");
            _font.Mutate(x => x.Flip(FlipMode.Vertical));
            var fontTextureBytes = new byte[4 * _font.Width * _font.Height];
            _font.CopyPixelDataTo(fontTextureBytes);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
                _font.Width, _font.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, fontTextureBytes);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
            _charVaos = new List<int>();
            float xTexSize = 8f / _font.Width;
            for (int i = 0; i < availableChars.Length; ++i)
            {
                _charVaos.Add(GL.GenVertexArray());
                GL.BindVertexArray(_charVaos[i]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, GL.GenBuffer());
                GL.BufferData(BufferTarget.ArrayBuffer, 24 * sizeof(float), new float[]
                {
                    0f, 0f, xTexSize * i, 0f,
                    0f, 1f, xTexSize * i, 1f,
                    0.5f, 1f, xTexSize * (i+1), 1f,
                    0f, 0f, xTexSize * i, 0f,
                    0.5f, 1f, xTexSize * (i+1), 1f,
                    0.5f, 0f, xTexSize * (i+1), 0f
                }, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2*sizeof(float));
                GL.EnableVertexAttribArray(1);
            }
            
        }
        
        float heightScale = 2f * height / WindowSize.Y;
        float widthScale = (float) height / WindowSize.X;
        Matrix4 sizeTransform = Matrix4.CreateScale(widthScale, heightScale, 1f);
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        ShaderManager.UIShader.Use();
        int currentXPos = x;
        foreach (char c in text)
        {
            int index = availableChars.IndexOf(c);
            
            GL.BindVertexArray(_charVaos[index]);
            var uniformLocation = GL.GetUniformLocation(ShaderManager.UIShader.Handle, "transform");
            var transform =  sizeTransform * Matrix4.CreateTranslation(2f * currentXPos / WindowSize.X - 1f, 2f * y / WindowSize.Y - 1f, 0.1f);
            GL.UniformMatrix4(uniformLocation, false, ref transform);
            

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            currentXPos += height / 2;
        }
        GL.Disable(EnableCap.Texture2D);
    }
}