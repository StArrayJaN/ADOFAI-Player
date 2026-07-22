namespace SharpFAI.Editor.Core.Framework.Assets;

/// <summary>
/// Desktop platform asset manager
/// 桌面平台资源管理器
/// </summary>
public class DesktopAssetManager : IAssetManager
{
    private readonly string _basePath;
    private readonly string _resourcesPath;

    public DesktopAssetManager()
    {
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        _resourcesPath = Path.Combine(_basePath, "Resources");
    }

    public string LoadText(string assetPath)
    {
        try
        {
            string fullPath = GetAssetPath(assetPath);
            Console.WriteLine(fullPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Asset not found: {assetPath}");

            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load text asset '{assetPath}': {ex.Message}", ex);
        }
    }

    public byte[] LoadBinary(string assetPath)
    {
        try
        {
            string fullPath = GetAssetPath(assetPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Asset not found: {assetPath}");

            return File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load binary asset '{assetPath}': {ex.Message}", ex);
        }
    }

    public bool AssetExists(string assetPath)
    {
        try
        {
            string fullPath = GetAssetPath(assetPath);
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    public string GetAssetPath(string assetPath)
    {
        // 首先尝试从 Resources 文件夹加载
        string resourcePath = Path.Combine(_resourcesPath, assetPath);
        if (File.Exists(resourcePath))
            return resourcePath;

        // 如果在开发环境中，尝试从项目目录加载
        string projectPath = Path.Combine(_basePath, "..", "..", "..", "SharpFAI.Editor.Resources", assetPath);
        if (File.Exists(projectPath))
            return Path.GetFullPath(projectPath);

        // 返回 Resources 路径作为默认值（即使不存在）
        return resourcePath;
    }
}
