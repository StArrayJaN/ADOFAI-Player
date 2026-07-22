namespace SharpFAI.Editor.Core.Platform.FileProvider;

/// <summary>
/// 文件对话框接口，提供跨平台的文件选择功能
/// </summary>
public interface IFileDialog
{
    /// <summary>
    /// 打开文件选择对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="basePath">初始目录路径</param>
    /// <param name="filter">文件过滤器</param>
    /// <returns>选中的文件路径，如果取消则返回 null</returns>
    string OpenFile(string title, string basePath, OpenFileFilter filter);
    
    /// <summary>
    /// 打开保存文件对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="basePath">初始目录路径</param>
    /// <param name="defaultName">默认文件名</param>
    /// <param name="filter">文件过滤器</param>
    /// <returns>保存的文件路径，如果取消则返回 null</returns>
    string SaveFile(string title, string basePath, string defaultName, SaveFileFilter filter);
    
    /// <summary>
    /// 打开文件夹选择对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="basePath">初始目录路径</param>
    /// <returns>选中的文件夹路径，如果取消则返回 null</returns>
    string OpenFolder(string title, string basePath);
}

/// <summary>
/// 文件对话框工厂类，根据操作系统返回对应的实现
/// </summary>
public static class FileDialog
{
    /// <summary>
    /// 获取当前平台的文件对话框实例
    /// </summary>
    /// <returns>文件对话框实例，如果平台不支持则返回 null</returns>
    public static IFileDialog? GetFileDialog()
    {
#if NET5_0_OR_GREATER
        if (OperatingSystem.IsWindows())
        {
            return new WindowsFileDialog();
        }
        if (OperatingSystem.IsMacOS())
        {
            return new MacOSFileDialog();
        }
        return OperatingSystem.IsLinux() ? new LinuxFileDialog() : null;
#else
        // .NET Framework 只支持 Windows
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return new WindowsFileDialog();
        }
        // Unix/Linux
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            // 尝试检测是否为 macOS
            try
            {
                var uname = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "uname",
                    Arguments = "-s",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (uname != null)
                {
                    var output = uname.StandardOutput.ReadToEnd().Trim();
                    uname.WaitForExit();
                    if (output == "Darwin")
                        return new MacOSFileDialog();
                    else
                        return new LinuxFileDialog();
                }
            }
            catch
            {
                // 默认为 Linux
                return new LinuxFileDialog();
            }
        }
        return null;
#endif
    }
}