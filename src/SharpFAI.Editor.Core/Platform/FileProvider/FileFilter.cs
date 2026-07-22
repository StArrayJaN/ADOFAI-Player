namespace SharpFAI.Editor.Core.Platform.FileProvider;

/// <summary>
/// 打开文件对话框的过滤器配置
/// </summary>
public struct OpenFileFilter
{
    /// <summary>
    /// 文件过滤器字典，键为显示名称，值为扩展名列表（如 ["txt", "log"]）
    /// </summary>
    public Dictionary<string, List<string>> Filter;
    
    /// <summary>
    /// 是否包含"所有文件"选项
    /// </summary>
    public bool IncludeAllFiles;
}

/// <summary>
/// 保存文件对话框的过滤器配置
/// </summary>
public struct SaveFileFilter
{
    /// <summary>
    /// 文件过滤器字典，键为显示名称，值为单个扩展名（如 "txt"）
    /// </summary>
    public Dictionary<string, string> Filter;
    
    /// <summary>
    /// 是否包含"所有文件"选项
    /// </summary>
    public bool IncludeAllFiles;
}