// See https://aka.ms/new-console-template for more information

namespace ComputerGraphics1_opentk_tomography;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("No file specified!");
            return;
        }

        string path = args[0];
        BinTomography tomography;
        try
        {
            tomography = new BinTomography(path);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine("File does not exist!");
            return;
        }

        using View view = new View(tomography, Path.GetFileName(path));
        view.Run();

    }
}
