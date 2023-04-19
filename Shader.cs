using OpenTK.Graphics.OpenGL;
using GL = OpenTK.Graphics.OpenGL.GL;
namespace ComputerGraphics1_opentk_tomography;

public class Shader
{
    private int _handle;

    public Shader(string vertexPath, string fragmentPath)
    {
        int vertexShader;
        int fragmentShader;
        string vertexShaderSource = File.ReadAllText(vertexPath);
        string fragmentShaderSource = File.ReadAllText(fragmentPath);

        vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);

        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            Console.Error.WriteLine("Vertex Shader compile error: ");
            Console.Error.WriteLine(GL.GetShaderInfoLog(vertexShader));
        }

        success = 0;
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
        if (success == 0)
        {
            Console.Error.WriteLine("Fragment Shader compile error: ");
            Console.Error.WriteLine(GL.GetShaderInfoLog(vertexShader));
        }

        success = 0;

        _handle = GL.CreateProgram();
        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);
        GL.LinkProgram(_handle);

        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out success);
        if (success == 0)
        {
            Console.Error.WriteLine("Program linking failed: ");
            Console.Error.WriteLine(GL.GetProgramInfoLog(_handle));
        }

        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public int Handle => _handle;


    public void Use()
    {
        GL.UseProgram(_handle);
    }
    
    
    private bool disposedValue = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            GL.DeleteProgram(_handle);

            disposedValue = true;
        }
    }

    ~Shader()
    {
        if (disposedValue == false)
        {
            Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
        }
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}