using System.Text;

// AccessControl 命名空间仅在 .NET Framework 或特定条件下可用
#if NETFRAMEWORK
using System.Security.AccessControl;
#endif
namespace SharpFAI.Editor.Core.Platform.FileProvider;

/// <summary>
/// 链式文件系统操作类，封装 Path/File/Directory 所有静态方法。
/// 支持隐式字符串转换，提供类似命令行风格的导航方法。
/// </summary>
/// <remarks>
/// 使用示例：
/// <code>
/// FluentFS fs = @"C:\Temp";
/// fs.Cd("SubFolder").CreateDirectory().Cd("file.txt").WriteAllText("Hello");
/// </code>
/// </remarks>
public class FluentFS
{
    private string _currentPath;

    /// <summary>
    /// 获取当前操作路径
    /// </summary>
    public string CurrentPath => _currentPath;

    /// <summary>
    /// 使用初始路径创建 FluentFS 实例
    /// </summary>
    /// <param name="path">初始路径</param>
    /// <exception cref="ArgumentNullException">path 为 null 时抛出</exception>
    public FluentFS(string path)
    {
        _currentPath = path ?? throw new ArgumentNullException(nameof(path));
    }

    // ========== 隐式转换 ==========

    /// <summary>
    /// 隐式将字符串转换为 FluentFS 实例
    /// </summary>
    /// <param name="path">路径字符串</param>
    public static implicit operator FluentFS(string path) => new FluentFS(path);

    /// <summary>
    /// 隐式将 FluentFS 实例转换为字符串（当前路径）
    /// </summary>
    /// <param name="fs">FluentFS 实例</param>
    public static implicit operator string(FluentFS fs) => fs._currentPath;

    // ========== 路径导航（类似命令行 cd/pwd） ==========

    /// <summary>
    /// 切换到指定路径（相当于 cd 命令）
    /// </summary>
    /// <param name="path">相对或绝对路径</param>
    /// <returns>当前实例（支持链式调用）</returns>
    /// <remarks>
    /// 例如：当前路径为 C:\Temp，Cd("Sub") 后路径变为 C:\Temp\Sub
    /// </remarks>
    public FluentFS Cd(string path)
    {
        _currentPath = Path.GetFullPath(Path.Combine(_currentPath, path));
        return this;
    }

    /// <summary>
    /// 返回上一级目录（相当于 cd ..）
    /// </summary>
    /// <returns>当前实例（支持链式调用）</returns>
    public FluentFS Up()
    {
        var parent = Directory.GetParent(_currentPath);
        if (parent != null) _currentPath = parent.FullName;
        return this;
    }

    /// <summary>
    /// 切换到根目录（相当于 cd \）
    /// </summary>
    /// <returns>当前实例（支持链式调用）</returns>
    public FluentFS Root()
    {
        _currentPath = Path.GetPathRoot(_currentPath);
        return this;
    }

    /// <summary>
    /// 组合路径但不改变当前路径（返回新实例）
    /// </summary>
    /// <param name="paths">要组合的路径片段</param>
    /// <returns>包含组合路径的新 FluentFS 实例</returns>
    public FluentFS Combine(params string[] paths)
    {
        var all = new[] { _currentPath }.Concat(paths).ToArray();
        return new FluentFS(Path.Combine(all));
    }

    /// <summary>
    /// 获取父目录（相当于 dirname）
    /// </summary>
    /// <returns>当前实例（支持链式调用）</returns>
    public FluentFS GetParent()
    {
        var parent = Directory.GetParent(_currentPath);
        if (parent != null) _currentPath = parent.FullName;
        return this;
    }

    // ========== Path 静态方法封装 ==========

    #region Path methods

    /// <inheritdoc cref="Path.ChangeExtension(string, string)"/>
    public string ChangeExtension(string extension) => Path.ChangeExtension(_currentPath, extension);

    /// <inheritdoc cref="Path.GetDirectoryName(string)"/>
    public string GetDirectoryName() => Path.GetDirectoryName(_currentPath);

    /// <inheritdoc cref="Path.GetExtension(string)"/>
    public string GetExtension() => Path.GetExtension(_currentPath);

    /// <inheritdoc cref="Path.GetFileName(string)"/>
    public string GetFileName() => Path.GetFileName(_currentPath);

    /// <inheritdoc cref="Path.GetFileNameWithoutExtension(string)"/>
    public string GetFileNameWithoutExtension() => Path.GetFileNameWithoutExtension(_currentPath);

    /// <inheritdoc cref="Path.GetFullPath(string)"/>
    public string GetFullPath() => Path.GetFullPath(_currentPath);

    /// <inheritdoc cref="Path.GetInvalidFileNameChars"/>
    public char[] GetInvalidFileNameChars() => Path.GetInvalidFileNameChars();

    /// <inheritdoc cref="Path.GetInvalidPathChars"/>
    public char[] GetInvalidPathChars() => Path.GetInvalidPathChars();

    /// <inheritdoc cref="Path.GetPathRoot(string)"/>
    public string GetPathRoot() => Path.GetPathRoot(_currentPath);

    /// <inheritdoc cref="Path.GetRandomFileName"/>
    public string GetRandomFileName() => Path.GetRandomFileName();

    /// <summary>
    /// 创建唯一命名的临时文件并更新当前路径
    /// </summary>
    /// <returns>临时文件完整路径</returns>
    /// <inheritdoc cref="Path.GetTempFileName"/>
    public string GetTempFileName()
    {
        _currentPath = Path.GetTempFileName();
        return _currentPath;
    }

    /// <inheritdoc cref="Path.GetTempPath"/>
    public string GetTempPath() => Path.GetTempPath();

    /// <inheritdoc cref="Path.HasExtension(string)"/>
    public bool HasExtension() => Path.HasExtension(_currentPath);

    /// <inheritdoc cref="Path.IsPathRooted(string)"/>
    public bool IsPathRooted() => Path.IsPathRooted(_currentPath);

    #endregion

    // ========== File 静态方法封装 ==========

    #region File methods

    /// <inheritdoc cref="File.Exists(string)"/>
    public bool Exists() => File.Exists(_currentPath);

    /// <inheritdoc cref="File.Open(string, FileMode)"/>
    public FileStream Open(FileMode mode) => File.Open(_currentPath, mode);

    /// <inheritdoc cref="File.Open(string, FileMode, FileAccess)"/>
    public FileStream Open(FileMode mode, FileAccess access) => File.Open(_currentPath, mode, access);

    /// <inheritdoc cref="File.Open(string, FileMode, FileAccess, FileShare)"/>
    public FileStream Open(FileMode mode, FileAccess access, FileShare share) => File.Open(_currentPath, mode, access, share);

    /// <inheritdoc cref="File.OpenRead(string)"/>
    public FileStream OpenRead() => File.OpenRead(_currentPath);

    /// <inheritdoc cref="File.OpenWrite(string)"/>
    public FileStream OpenWrite() => File.OpenWrite(_currentPath);

    /// <inheritdoc cref="File.OpenText(string)"/>
    public StreamReader OpenText() => File.OpenText(_currentPath);

    /// <inheritdoc cref="File.CreateText(string)"/>
    public StreamWriter CreateText() => File.CreateText(_currentPath);

    /// <inheritdoc cref="File.AppendText(string)"/>
    public StreamWriter AppendText() => File.AppendText(_currentPath);

    /// <inheritdoc cref="File.Create(string)"/>
    public FileStream Create() => File.Create(_currentPath);

    /// <inheritdoc cref="File.Create(string, int)"/>
    public FileStream Create(int bufferSize) => File.Create(_currentPath, bufferSize);

    /// <inheritdoc cref="File.Create(string, int, FileOptions)"/>
    public FileStream Create(int bufferSize, FileOptions options) => File.Create(_currentPath, bufferSize, options);

    /// <inheritdoc cref="File.Copy(string, string)"/>
    public FluentFS Copy(string destFileName) { File.Copy(_currentPath, destFileName); return this; }

    /// <inheritdoc cref="File.Copy(string, string, bool)"/>
    public FluentFS Copy(string destFileName, bool overwrite) { File.Copy(_currentPath, destFileName, overwrite); return this; }

    /// <summary>
    /// 移动文件到新位置，并更新当前路径
    /// </summary>
    /// <param name="destFileName">目标路径</param>
    /// <returns>当前实例</returns>
    /// <inheritdoc cref="File.Move(string, string)"/>
    public FluentFS Move(string destFileName) 
    { 
        File.Move(_currentPath, destFileName); 
        _currentPath = destFileName; 
        return this; 
    }

    /// <inheritdoc cref="File.Delete(string)"/>
    public FluentFS Delete() { File.Delete(_currentPath); return this; }

    /// <inheritdoc cref="File.Replace(string, string, string)"/>
    public FluentFS Replace(string destinationFileName, string destinationBackupFileName) 
    { 
        File.Replace(_currentPath, destinationFileName, destinationBackupFileName); 
        _currentPath = destinationFileName; 
        return this; 
    }

    /// <inheritdoc cref="File.Replace(string, string, string, bool)"/>
    public FluentFS Replace(string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) 
    { 
        File.Replace(_currentPath, destinationFileName, destinationBackupFileName, ignoreMetadataErrors); 
        _currentPath = destinationFileName; 
        return this; 
    }

    /// <inheritdoc cref="File.GetAttributes(string)"/>
    public FileAttributes GetAttributes() => File.GetAttributes(_currentPath);

    /// <inheritdoc cref="File.SetAttributes(string, FileAttributes)"/>
    public FluentFS SetAttributes(FileAttributes fileAttributes) 
    { 
        File.SetAttributes(_currentPath, fileAttributes); 
        return this; 
    }

    /// <inheritdoc cref="File.GetCreationTime(string)"/>
    public DateTime GetCreationTime() => File.GetCreationTime(_currentPath);

    /// <inheritdoc cref="File.GetCreationTimeUtc(string)"/>
    public DateTime GetCreationTimeUtc() => File.GetCreationTimeUtc(_currentPath);

    /// <inheritdoc cref="File.SetCreationTime(string, DateTime)"/>
    public FluentFS SetCreationTime(DateTime creationTime) 
    { 
        File.SetCreationTime(_currentPath, creationTime); 
        return this; 
    }

    /// <inheritdoc cref="File.SetCreationTimeUtc(string, DateTime)"/>
    public FluentFS SetCreationTimeUtc(DateTime creationTimeUtc) 
    { 
        File.SetCreationTimeUtc(_currentPath, creationTimeUtc); 
        return this; 
    }

    /// <inheritdoc cref="File.GetLastAccessTime(string)"/>
    public DateTime GetLastAccessTime() => File.GetLastAccessTime(_currentPath);

    /// <inheritdoc cref="File.GetLastAccessTimeUtc(string)"/>
    public DateTime GetLastAccessTimeUtc() => File.GetLastAccessTimeUtc(_currentPath);

    /// <inheritdoc cref="File.SetLastAccessTime(string, DateTime)"/>
    public FluentFS SetLastAccessTime(DateTime lastAccessTime) 
    { 
        File.SetLastAccessTime(_currentPath, lastAccessTime); 
        return this; 
    }

    /// <inheritdoc cref="File.SetLastAccessTimeUtc(string, DateTime)"/>
    public FluentFS SetLastAccessTimeUtc(DateTime lastAccessTimeUtc) 
    { 
        File.SetLastAccessTimeUtc(_currentPath, lastAccessTimeUtc); 
        return this; 
    }

    /// <inheritdoc cref="File.GetLastWriteTime(string)"/>
    public DateTime GetLastWriteTime() => File.GetLastWriteTime(_currentPath);

    /// <inheritdoc cref="File.GetLastWriteTimeUtc(string)"/>
    public DateTime GetLastWriteTimeUtc() => File.GetLastWriteTimeUtc(_currentPath);

    /// <inheritdoc cref="File.SetLastWriteTime(string, DateTime)"/>
    public FluentFS SetLastWriteTime(DateTime lastWriteTime) 
    { 
        File.SetLastWriteTime(_currentPath, lastWriteTime); 
        return this; 
    }

    /// <inheritdoc cref="File.SetLastWriteTimeUtc(string, DateTime)"/>
    public FluentFS SetLastWriteTimeUtc(DateTime lastWriteTimeUtc) 
    { 
        File.SetLastWriteTimeUtc(_currentPath, lastWriteTimeUtc); 
        return this; 
    }

    #if NETFRAMEWORK
    /// <inheritdoc cref="File.Decrypt(string)"/>
    public FluentFS Decrypt() { File.Decrypt(_currentPath); return this; }

    /// <inheritdoc cref="File.Encrypt(string)"/>
    public FluentFS Encrypt() { File.Encrypt(_currentPath); return this; }

    /// <inheritdoc cref="File.GetAccessControl(string)"/>
    public FileSecurity GetAccessControl() => File.GetAccessControl(_currentPath);

    /// <inheritdoc cref="File.GetAccessControl(string, AccessControlSections)"/>
    public FileSecurity GetAccessControl(AccessControlSections includeSections) => 
        File.GetAccessControl(_currentPath, includeSections);

    /// <inheritdoc cref="File.SetAccessControl(string, FileSecurity)"/>
    public FluentFS SetAccessControl(FileSecurity fileSecurity) 
    { 
        File.SetAccessControl(_currentPath, fileSecurity); 
        return this; 
    }
    #endif

    /// <inheritdoc cref="File.ReadAllText(string)"/>
    public string ReadAllText() => File.ReadAllText(_currentPath);

    /// <inheritdoc cref="File.ReadAllText(string, Encoding)"/>
    public string ReadAllText(Encoding encoding) => File.ReadAllText(_currentPath, encoding);

    /// <inheritdoc cref="File.WriteAllText(string, string)"/>
    public FluentFS WriteAllText(string contents) 
    { 
        File.WriteAllText(_currentPath, contents); 
        return this; 
    }

    /// <inheritdoc cref="File.WriteAllText(string, string, Encoding)"/>
    public FluentFS WriteAllText(string contents, Encoding encoding) 
    { 
        File.WriteAllText(_currentPath, contents, encoding); 
        return this; 
    }

    /// <inheritdoc cref="File.ReadAllBytes(string)"/>
    public byte[] ReadAllBytes() => File.ReadAllBytes(_currentPath);

    /// <inheritdoc cref="File.WriteAllBytes(string, byte[])"/>
    public FluentFS WriteAllBytes(byte[] bytes) 
    { 
        File.WriteAllBytes(_currentPath, bytes); 
        return this; 
    }

    /// <inheritdoc cref="File.ReadAllLines(string)"/>
    public string[] ReadAllLines() => File.ReadAllLines(_currentPath);

    /// <inheritdoc cref="File.ReadAllLines(string, Encoding)"/>
    public string[] ReadAllLines(Encoding encoding) => File.ReadAllLines(_currentPath, encoding);

    /// <inheritdoc cref="File.WriteAllLines(string, string[])"/>
    public FluentFS WriteAllLines(string[] contents) 
    { 
        File.WriteAllLines(_currentPath, contents); 
        return this; 
    }

    /// <inheritdoc cref="File.WriteAllLines(string, string[], Encoding)"/>
    public FluentFS WriteAllLines(string[] contents, Encoding encoding) 
    { 
        File.WriteAllLines(_currentPath, contents, encoding); 
        return this; 
    }

    /// <inheritdoc cref="File.WriteAllLines(string, IEnumerable{string})"/>
    public FluentFS WriteAllLines(IEnumerable<string> contents) 
    { 
        File.WriteAllLines(_currentPath, contents); 
        return this; 
    }

    /// <inheritdoc cref="File.WriteAllLines(string, IEnumerable{string}, Encoding)"/>
    public FluentFS WriteAllLines(IEnumerable<string> contents, Encoding encoding) 
    { 
        File.WriteAllLines(_currentPath, contents, encoding); 
        return this; 
    }

    /// <inheritdoc cref="File.AppendAllText(string, string)"/>
    public FluentFS AppendAllText(string contents) 
    { 
        File.AppendAllText(_currentPath, contents); 
        return this; 
    }

    /// <inheritdoc cref="File.AppendAllText(string, string, Encoding)"/>
    public FluentFS AppendAllText(string contents, Encoding encoding) 
    { 
        File.AppendAllText(_currentPath, contents, encoding); 
        return this; 
    }

    /// <inheritdoc cref="File.AppendAllLines(string, IEnumerable{string})"/>
    public FluentFS AppendAllLines(IEnumerable<string> contents) 
    { 
        File.AppendAllLines(_currentPath, contents); 
        return this; 
    }

    /// <inheritdoc cref="File.AppendAllLines(string, IEnumerable{string}, Encoding)"/>
    public FluentFS AppendAllLines(IEnumerable<string> contents, Encoding encoding) 
    { 
        File.AppendAllLines(_currentPath, contents, encoding); 
        return this; 
    }

    #endregion

    // ========== Directory 静态方法封装 ==========

    #region Directory methods

    /// <summary>
    /// 判断当前路径是否为存在的目录
    /// </summary>
    /// <inheritdoc cref="Directory.Exists(string)"/>
    public bool DirectoryExists() => Directory.Exists(_currentPath);

    /// <inheritdoc cref="Directory.CreateDirectory(string)"/>
    public FluentFS CreateDirectory() 
    { 
        Directory.CreateDirectory(_currentPath); 
        return this; 
    }

    #if NETFRAMEWORK
    /// <inheritdoc cref="Directory.CreateDirectory(string, DirectorySecurity)"/>
    public FluentFS CreateDirectory(DirectorySecurity directorySecurity) 
    { 
        Directory.CreateDirectory(_currentPath, directorySecurity); 
        return this; 
    }
    #endif

    /// <inheritdoc cref="Directory.Delete(string)"/>
    public FluentFS DeleteDirectory() 
    { 
        Directory.Delete(_currentPath); 
        return this; 
    }

    /// <inheritdoc cref="Directory.Delete(string, bool)"/>
    public FluentFS DeleteDirectory(bool recursive) 
    { 
        Directory.Delete(_currentPath, recursive); 
        return this; 
    }

    /// <summary>
    /// 移动目录到新位置，并更新当前路径
    /// </summary>
    /// <param name="destDirName">目标目录路径</param>
    /// <returns>当前实例</returns>
    /// <inheritdoc cref="Directory.Move(string, string)"/>
    public FluentFS MoveDirectory(string destDirName) 
    { 
        Directory.Move(_currentPath, destDirName); 
        _currentPath = destDirName; 
        return this; 
    }

    /// <inheritdoc cref="Directory.GetFiles(string)"/>
    public string[] GetFiles() => Directory.GetFiles(_currentPath);

    /// <inheritdoc cref="Directory.GetFiles(string, string)"/>
    public string[] GetFiles(string searchPattern) => Directory.GetFiles(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.GetFiles(string, string, SearchOption)"/>
    public string[] GetFiles(string searchPattern, SearchOption searchOption) => 
        Directory.GetFiles(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.GetDirectories(string)"/>
    public string[] GetDirectories() => Directory.GetDirectories(_currentPath);

    /// <inheritdoc cref="Directory.GetDirectories(string, string)"/>
    public string[] GetDirectories(string searchPattern) => Directory.GetDirectories(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.GetDirectories(string, string, SearchOption)"/>
    public string[] GetDirectories(string searchPattern, SearchOption searchOption) => 
        Directory.GetDirectories(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.GetFileSystemEntries(string)"/>
    public string[] GetFileSystemEntries() => Directory.GetFileSystemEntries(_currentPath);

    /// <inheritdoc cref="Directory.GetFileSystemEntries(string, string)"/>
    public string[] GetFileSystemEntries(string searchPattern) => 
        Directory.GetFileSystemEntries(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.GetFileSystemEntries(string, string, SearchOption)"/>
    public string[] GetFileSystemEntries(string searchPattern, SearchOption searchOption) => 
        Directory.GetFileSystemEntries(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.EnumerateFiles(string)"/>
    public IEnumerable<string> EnumerateFiles() => Directory.EnumerateFiles(_currentPath);

    /// <inheritdoc cref="Directory.EnumerateFiles(string, string)"/>
    public IEnumerable<string> EnumerateFiles(string searchPattern) => 
        Directory.EnumerateFiles(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.EnumerateFiles(string, string, SearchOption)"/>
    public IEnumerable<string> EnumerateFiles(string searchPattern, SearchOption searchOption) => 
        Directory.EnumerateFiles(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
    public IEnumerable<string> EnumerateDirectories() => Directory.EnumerateDirectories(_currentPath);

    /// <inheritdoc cref="Directory.EnumerateDirectories(string, string)"/>
    public IEnumerable<string> EnumerateDirectories(string searchPattern) => 
        Directory.EnumerateDirectories(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.EnumerateDirectories(string, string, SearchOption)"/>
    public IEnumerable<string> EnumerateDirectories(string searchPattern, SearchOption searchOption) => 
        Directory.EnumerateDirectories(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.EnumerateFileSystemEntries(string)"/>
    public IEnumerable<string> EnumerateFileSystemEntries() => Directory.EnumerateFileSystemEntries(_currentPath);

    /// <inheritdoc cref="Directory.EnumerateFileSystemEntries(string, string)"/>
    public IEnumerable<string> EnumerateFileSystemEntries(string searchPattern) => 
        Directory.EnumerateFileSystemEntries(_currentPath, searchPattern);

    /// <inheritdoc cref="Directory.EnumerateFileSystemEntries(string, string, SearchOption)"/>
    public IEnumerable<string> EnumerateFileSystemEntries(string searchPattern, SearchOption searchOption) => 
        Directory.EnumerateFileSystemEntries(_currentPath, searchPattern, searchOption);

    /// <inheritdoc cref="Directory.GetCreationTime(string)"/>
    public DateTime GetCreationTimeDirectory() => Directory.GetCreationTime(_currentPath);

    /// <inheritdoc cref="Directory.GetCreationTimeUtc(string)"/>
    public DateTime GetCreationTimeUtcDirectory() => Directory.GetCreationTimeUtc(_currentPath);

    /// <inheritdoc cref="Directory.SetCreationTime(string, DateTime)"/>
    public FluentFS SetCreationTimeDirectory(DateTime creationTime) 
    { 
        Directory.SetCreationTime(_currentPath, creationTime); 
        return this; 
    }

    /// <inheritdoc cref="Directory.SetCreationTimeUtc(string, DateTime)"/>
    public FluentFS SetCreationTimeUtcDirectory(DateTime creationTimeUtc) 
    { 
        Directory.SetCreationTimeUtc(_currentPath, creationTimeUtc); 
        return this; 
    }

    /// <inheritdoc cref="Directory.GetLastAccessTime(string)"/>
    public DateTime GetLastAccessTimeDirectory() => Directory.GetLastAccessTime(_currentPath);

    /// <inheritdoc cref="Directory.GetLastAccessTimeUtc(string)"/>
    public DateTime GetLastAccessTimeUtcDirectory() => Directory.GetLastAccessTimeUtc(_currentPath);

    /// <inheritdoc cref="Directory.SetLastAccessTime(string, DateTime)"/>
    public FluentFS SetLastAccessTimeDirectory(DateTime lastAccessTime) 
    { 
        Directory.SetLastAccessTime(_currentPath, lastAccessTime); 
        return this; 
    }

    /// <inheritdoc cref="Directory.SetLastAccessTimeUtc(string, DateTime)"/>
    public FluentFS SetLastAccessTimeUtcDirectory(DateTime lastAccessTimeUtc) 
    { 
        Directory.SetLastAccessTimeUtc(_currentPath, lastAccessTimeUtc); 
        return this; 
    }

    /// <inheritdoc cref="Directory.GetLastWriteTime(string)"/>
    public DateTime GetLastWriteTimeDirectory() => Directory.GetLastWriteTime(_currentPath);

    /// <inheritdoc cref="Directory.GetLastWriteTimeUtc(string)"/>
    public DateTime GetLastWriteTimeUtcDirectory() => Directory.GetLastWriteTimeUtc(_currentPath);

    /// <inheritdoc cref="Directory.SetLastWriteTime(string, DateTime)"/>
    public FluentFS SetLastWriteTimeDirectory(DateTime lastWriteTime) 
    { 
        Directory.SetLastWriteTime(_currentPath, lastWriteTime); 
        return this; 
    }

    /// <inheritdoc cref="Directory.SetLastWriteTimeUtc(string, DateTime)"/>
    public FluentFS SetLastWriteTimeUtcDirectory(DateTime lastWriteTimeUtc) 
    { 
        Directory.SetLastWriteTimeUtc(_currentPath, lastWriteTimeUtc); 
        return this; 
    }

    #if NETFRAMEWORK
    /// <inheritdoc cref="Directory.GetAccessControl(string)"/>
    public DirectorySecurity GetAccessControlDirectory() => Directory.GetAccessControl(_currentPath);

    /// <inheritdoc cref="Directory.GetAccessControl(string, AccessControlSections)"/>
    public DirectorySecurity GetAccessControlDirectory(AccessControlSections includeSections) => 
        Directory.GetAccessControl(_currentPath, includeSections);

    /// <inheritdoc cref="Directory.SetAccessControl(string, DirectorySecurity)"/>
    public FluentFS SetAccessControlDirectory(DirectorySecurity directorySecurity) 
    { 
        Directory.SetAccessControl(_currentPath, directorySecurity); 
        return this; 
    }
    #endif

    /// <inheritdoc cref="Directory.GetDirectoryRoot(string)"/>
    public string GetDirectoryRoot() => Directory.GetDirectoryRoot(_currentPath);

    /// <inheritdoc cref="Directory.GetParent(string)"/>
    public FluentFS GetParentDirectory()
    {
        var parent = Directory.GetParent(_currentPath);
        if (parent != null) _currentPath = parent.FullName;
        return this;
    }

    /// <inheritdoc cref="Directory.GetCurrentDirectory"/>
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    /// <summary>
    /// 将当前路径设置为应用程序的当前工作目录
    /// </summary>
    /// <returns>当前实例</returns>
    /// <inheritdoc cref="Directory.SetCurrentDirectory(string)"/>
    public FluentFS SetCurrentDirectory()
    {
        Directory.SetCurrentDirectory(_currentPath);
        return this;
    }

    /// <inheritdoc cref="Directory.GetLogicalDrives"/>
    public static string[] GetLogicalDrives() => Directory.GetLogicalDrives();

    #endregion

    // ========== 类似 Java Files.walkFileTree 的遍历 ==========

    /// <summary>
    /// 递归遍历文件树，使用委托提供访问回调（类似 Java Files.walkFileTree）
    /// </summary>
    /// <param name="preVisitDirectory">进入目录前调用，返回 true 继续遍历该目录，否则跳过</param>
    /// <param name="postVisitDirectory">离开目录后调用</param>
    /// <param name="visitFile">访问文件时调用</param>
    /// <param name="visitFileFailed">访问失败时调用（参数：路径，异常）</param>
    /// <returns>当前实例（支持链式调用）</returns>
    /// <remarks>
    /// 遍历顺序：深度优先，先处理子目录和文件，最后调用 postVisitDirectory
    /// </remarks>
    public FluentFS WalkFileTree(
        Func<string, bool> preVisitDirectory = null,
        Action<string> postVisitDirectory = null,
        Action<string> visitFile = null,
        Action<string, Exception> visitFileFailed = null)
    {
        WalkFileTreeInternal(_currentPath, preVisitDirectory, postVisitDirectory, visitFile, visitFileFailed);
        return this;
    }

    private void WalkFileTreeInternal(
        string directory,
        Func<string, bool> preVisitDirectory,
        Action<string> postVisitDirectory,
        Action<string> visitFile,
        Action<string, Exception> visitFileFailed)
    {
        if (preVisitDirectory != null && !preVisitDirectory(directory))
            return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(directory);
        }
        catch (Exception ex)
        {
            visitFileFailed?.Invoke(directory, ex);
            return;
        }

        foreach (var entry in entries)
        {
            try
            {
                bool isDirectory = (File.GetAttributes(entry) & FileAttributes.Directory) == FileAttributes.Directory;
                if (isDirectory)
                {
                    WalkFileTreeInternal(entry, preVisitDirectory, postVisitDirectory, visitFile, visitFileFailed);
                }
                else
                {
                    visitFile?.Invoke(entry);
                }
            }
            catch (Exception ex)
            {
                visitFileFailed?.Invoke(entry, ex);
            }
        }

        postVisitDirectory?.Invoke(directory);
    }
}