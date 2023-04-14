using OpenTK.Graphics.OpenGL;

namespace ComputerGraphics1_opentk_tomography;

public class TextUtility
{
    private static int fontTexture = -1;

    private static Image<Rgba32> font;
    public static void Draw(int x, int y, string text, int height, float winScaleX, float winScaleY)
    {
        const string availableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}\\|;:'\".,<>/?`~ ";
        if (fontTexture == -1)
        {
            fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, fontTexture);
            font = Image.Load<Rgba32>("font.png");
            var fontTextureBytes = new byte[4 * font.Width * font.Height];
            font.CopyPixelDataTo(fontTextureBytes);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
                font.Width, font.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, fontTextureBytes);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
        }

        float xTexSize = 8f / font.Width;
        float heightScale = height / 16f;
        GL.Enable(EnableCap.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.Begin(PrimitiveType.Quads);
        int currentXPos = x;
        GL.Color3(1f, 1f, 1f);
        foreach (char c in text)
        {
            int index = availableChars.IndexOf(c);
            
            GL.TexCoord2(xTexSize * index, 1f);
            GL.Vertex2(currentXPos * winScaleX, y * winScaleY);
            GL.TexCoord2(xTexSize * (index+1), 1f);
            GL.Vertex2((currentXPos + 8 * heightScale) * winScaleX, y* winScaleY);
            GL.TexCoord2(xTexSize * (index+1), 0f);
            GL.Vertex2((currentXPos + 8 * heightScale) * winScaleX, (y + 16 * heightScale)* winScaleY);
            GL.TexCoord2(xTexSize * index, 0f);
            GL.Vertex2(currentXPos * winScaleX, (y + 16 * heightScale)* winScaleY);

            currentXPos += (int) (8 * heightScale);

        }
        GL.End();
        GL.Disable(EnableCap.Texture2D);
    }
}