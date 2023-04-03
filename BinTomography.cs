namespace ComputerGraphics1_opentk_tomography;

public class BinTomography
{
    public int XSize { get; private set; }
    public int YSize { get; private set; }
    public int ZSize { get; private set; }

    private short[] _dataArray;
    
    public BinTomography(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Not found bin file on this path");
        }

        using FileStream fs = new FileStream(path, FileMode.Open);
        using BinaryReader br = new BinaryReader(fs);
        XSize = br.ReadInt32();
        YSize = br.ReadInt32();
        ZSize = br.ReadInt32();
        int arraySize = XSize * YSize * ZSize;
        _dataArray = new short[arraySize];
        for (int i = 0; i < arraySize; i++)
        {
            _dataArray[i] = br.ReadInt16();
        }
    }

    public short this[int x, int y, int z] => _dataArray[x + y * XSize + z * XSize * YSize];
}