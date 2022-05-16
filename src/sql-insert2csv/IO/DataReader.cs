using System.Text;

namespace SqlInsert2Sql;

public class DataReader : IDisposable
{
    protected StreamReader _streamReader;
    protected readonly Encoding _encoding;

    protected const int FIRST_BLOCK_NUM = 0;

    public int BufferSize { get; }
    public char[] Buffer { get; }
    public long BlockNum { get; protected set; } = FIRST_BLOCK_NUM - 1;

    public DataReader(string path, string encoding, int buffer)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        _encoding = Encoding.GetEncoding(encoding);
        _streamReader = new StreamReader(path, _encoding);

        BufferSize = buffer;
        Buffer = new char[BufferSize];

        if (!TryReadBlock())
        {
            throw new Exception("It is not possible to read at least one data block.");
        }
    }

    public bool IsFirstBlock()
    {
        return BlockNum == FIRST_BLOCK_NUM;
    }

    public bool TryReadBlock()
    {
        if (_streamReader.Read(Buffer, 0, BufferSize) > 0)
        {
            BlockNum++;
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        _streamReader.Dispose();
    }
}
