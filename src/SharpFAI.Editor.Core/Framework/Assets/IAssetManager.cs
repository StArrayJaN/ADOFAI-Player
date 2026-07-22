namespace SharpFAI.Editor.Core.Framework.Assets;

/// <summary>
/// Asset manager interface for loading resources
/// 资源管理器接口，用于加载资源
/// </summary>
public interface IAssetManager
{
    /// <summary>
    /// Load text file content / 加载文本文件内容
    /// </summary>
    string LoadText(string assetPath);

    /// <summary>
    /// Load binary file content / 加载二进制文件内容
    /// </summary>
    byte[] LoadBinary(string assetPath);

    /// <summary>
    /// Check if asset exists / 检查资源是否存在
    /// </summary>
    bool AssetExists(string assetPath);

    /// <summary>
    /// Get full path of asset / 获取资源的完整路径
    /// </summary>
    string GetAssetPath(string assetPath);
}
