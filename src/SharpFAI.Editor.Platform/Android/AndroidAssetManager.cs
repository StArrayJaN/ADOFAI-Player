using Android.Content;
using Android.Content.Res;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// Android platform asset manager
/// 安卓平台资源管理器
/// </summary>
public class AndroidAssetManager : SharpFAI.Editor.Core.Framework.Assets.IAssetManager
{
    private readonly AssetManager _assetManager;

    public AndroidAssetManager(Context context)
    {
        _assetManager = context?.Assets ?? throw new ArgumentNullException(nameof(context));
    }

    public string LoadText(string assetPath)
    {
        try
        {
            using (var stream = _assetManager.Open(assetPath))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
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
            using (var stream = _assetManager.Open(assetPath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
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
            using (var stream = _assetManager.Open(assetPath))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public string GetAssetPath(string assetPath)
    {
        // Android 中资源通过 AssetManager 访问，返回相对路径
        return assetPath;
    }
}
