using Android.Views;
using SharpFAI.Editor.Core.Framework.Assets;

namespace SharpFAI.Editor.Platform.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private ImGuiView? imguiView;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Initialize asset manager
        AssetManager.Initialize(new AndroidAssetManager(this));

        // 创建 ImGuiView 并设置为内容视图
        imguiView = new ImGuiView(this);
        SetContentView(imguiView);
    }

    protected override void OnResume()
    {
        base.OnResume();
        imguiView?.OnResume();
    }

    protected override void OnPause()
    {
        base.OnPause();
        imguiView?.OnPause();
    }
    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e != null)
        {
            // 关键：立即处理删除键，不要等待输入法
            if (e.KeyCode == Keycode.Del)
            {
                InputProcessor.Instance.ProcessKeyEvent(e);
                // 返回 true 表示已处理，阻止输入法进一步处理
                return true;
            }

            InputProcessor.Instance.ProcessKeyEvent(e);
        }
        return base.DispatchKeyEvent(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            imguiView?.Dispose();
            imguiView = null;
        }
        base.Dispose(disposing);
    }
}