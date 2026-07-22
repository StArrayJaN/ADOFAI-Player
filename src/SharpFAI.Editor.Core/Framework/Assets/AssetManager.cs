namespace SharpFAI.Editor.Core.Framework.Assets;

/// <summary>
/// Global asset manager singleton
/// 全局资源管理器单例
/// </summary>
public static class AssetManager
{
    private static IAssetManager _instance;

    /// <summary>
    /// Initialize asset manager with specific implementation
    /// 使用特定实现初始化资源管理器
    /// </summary>
    public static void Initialize(IAssetManager assetManager)
    {
        _instance = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
    }

    /// <summary>
    /// Get current asset manager instance
    /// 获取当前资源管理器实例
    /// </summary>
    public static IAssetManager Instance
    {
        get
        {
            if (_instance == null)
                throw new InvalidOperationException("AssetManager not initialized. Call Initialize() first.");
            return _instance;
        }
    }

    /// <summary>
    /// Load text file content / 加载文本文件内容
    /// </summary>
    public static string LoadText(string assetPath) => Instance.LoadText(assetPath);

    /// <summary>
    /// Load binary file content / 加载二进制文件内容
    /// </summary>
    public static byte[] LoadBinary(string assetPath) => Instance.LoadBinary(assetPath);

    /// <summary>
    /// Check if asset exists / 检查资源是否存在
    /// </summary>
    public static bool AssetExists(string assetPath) => Instance.AssetExists(assetPath);

    /// <summary>
    /// Get full path of asset / 获取资源的完整路径
    /// </summary>
    public static string GetAssetPath(string assetPath) => Instance.GetAssetPath(assetPath);
}
