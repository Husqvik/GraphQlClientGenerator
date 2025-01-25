using System.Text;

namespace GraphQlClientGenerator;

public interface ICodeFileEmitter
{
    CodeFile CreateFile(string memberName);

    CodeFileInfo CollectFileInfo(CodeFile codeFile);
}

public struct CodeFileInfo
{
    public string FileName { get; set; }

    public long Length { get; set; }
}

public class CodeFile : IDisposable
{
    private Stream _stream;
    private StreamWriter _writer;

    public CodeFile(string fileName, Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException();
        _writer = new StreamWriter(_stream, Encoding.UTF8);
        FileName = fileName;
    }

    public string FileName { get; }

    public Stream Stream => _stream ?? throw new ObjectDisposedException(nameof(CodeFile));

    public TextWriter Writer => _writer ?? throw new ObjectDisposedException(nameof(CodeFile));

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
        _stream?.Dispose();
        _stream = null;
    }
}