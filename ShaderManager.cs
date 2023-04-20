namespace ComputerGraphics1_opentk_tomography;

public static class ShaderManager
{
    public static Shader UIShader { get; } = new Shader("UI.vert", "UI.frag");

    public static Shader TomographyTextureShader { get; } = new Shader("tomographyTexture.vert", "UI.frag");

    public static Shader TomographyQuadShader { get; } = new Shader("shader.vert", "shader.frag");
}