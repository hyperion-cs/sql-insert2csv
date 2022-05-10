using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace SqlInsert2Sql;

public class DataWriter : IDisposable
{
    protected readonly CsvConfiguration _csvConfiguration;
    protected readonly Encoding _encoding;

    protected FileStream? _fileStream;
    protected readonly FileStreamOptions _fileStreamOptions;
    protected readonly int _bufferSize;

    protected StreamWriter? _streamWriter;
    protected CsvWriter? _csvWriter;

    protected const string DELIMITER = "|";
    protected const char QUOTE = '"';

    public string OutputDir { get; private set; }

    public DataWriter(string outputDir, string encoding, int buffer)
    {
        OutputDir = outputDir;
        Directory.CreateDirectory(OutputDir);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = Encoding.GetEncoding(encoding);

        _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DELIMITER,
            Quote = QUOTE,
            Encoding = _encoding,
            HasHeaderRecord = false,
        };

        _bufferSize = buffer;

        _fileStreamOptions = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            BufferSize = _bufferSize
        };
    }

    public void NextInsert(long insertNum, string tableName)
    {
        Dispose();

        var dt = DateTime.Now.ToString("yyyyMMddHHmmss");
        var path = Path.Combine(OutputDir, $"{insertNum}_{tableName}_{ dt}.csv");

        _fileStream = File.Open(path, _fileStreamOptions);
        _streamWriter = new StreamWriter(_fileStream, _encoding);
        _csvWriter = new CsvWriter(_streamWriter, _csvConfiguration);
    }

    public void NextLine()
    {
        _csvWriter?.NextRecord();
    }

    public void WriteField(string val)
    {
        _csvWriter?.WriteField(val);
    }

    public void Dispose()
    {
        if (_csvWriter is not null)
        {
            _csvWriter.Flush();
            _csvWriter.Dispose();
        }

        if (_streamWriter is not null)
        {
            _streamWriter.Dispose();
        }

        if (_fileStream is not null)
        {
            _fileStream.Dispose();
        }
    }
}
