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

public class CodeFile(string fileName, Stream stream) : IDisposable
{
    private Stream _stream = stream ?? throw new ArgumentNullException();
    private StreamWriter _writer = new(stream);

    public string FileName { get; } = fileName;

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