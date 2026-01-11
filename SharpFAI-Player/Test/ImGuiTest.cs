using NUnit.Framework;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;

namespace SharpFAI_Player.Test;

/// <summary>
/// Test program for ImGuiGameWindow
/// ImGuiGameWindow 测试程序
/// 
/// Note: OpenGL/GLFW tests cannot run in standard unit test environments
/// because GLFW requires initialization on the main thread.
/// These tests verify type existence and basic functionality without
/// requiring OpenGL context.
/// </summary>
[TestFixture]
public class ImGuiTest
{
    /// <summary>
    /// Test that ImGuiGameWindow types exist and are accessible
    /// 测试 ImGuiGameWindow 类型存在且可访问
    /// </summary>
    [Test]
    public void TestImguiGameWindow_TypesExist()
    {
        // Verify the classes exist and can be referenced
        Assert.That(typeof(ImGuiGameWindow), Is.Not.Null, "ImGuiGameWindow type should exist");
        Assert.That(typeof(ImGuiController), Is.Not.Null, "ImGuiController type should exist");
        
        // Verify ImGuiGameWindow inherits from GameWindow
        Assert.That(typeof(GameWindow).IsAssignableFrom(typeof(ImGuiGameWindow)), Is.True,
            "ImGuiGameWindow should inherit from GameWindow");
    }

    /// <summary>
    /// Test that ImGuiGameWindow has expected public methods
    /// 测试 ImGuiGameWindow 具有预期的公共方法
    /// </summary>
    [Test]
    public void TestImguiGameWindow_HasExpectedMethods()
    {
        var type = typeof(ImGuiGameWindow);
        
        // Check for inherited GameWindow methods
        Assert.That(type.GetMethod("Run"), Is.Not.Null, "Should have Run method");
        Assert.That(type.GetMethod("Close"), Is.Not.Null, "Should have Close method");
        
        // Check for overridden methods
        var onLoadMethod = type.GetMethod("OnLoad", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(onLoadMethod, Is.Not.Null, "Should override OnLoad method");
        
        var onRenderFrameMethod = type.GetMethod("OnRenderFrame",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(onRenderFrameMethod, Is.Not.Null, "Should override OnRenderFrame method");
    }

    /// <summary>
    /// Test that ImGuiController has expected public methods
    /// 测试 ImGuiController 具有预期的公共方法
    /// </summary>
    [Test]
    public void TestImguiController_HasExpectedMethods()
    {
        var type = typeof(ImGuiController);
        
        // Check for expected methods
        Assert.That(type.GetMethod("Update"), Is.Not.Null, "Should have Update method");
        Assert.That(type.GetMethod("Render"), Is.Not.Null, "Should have Render method");
        Assert.That(type.GetMethod("WindowResized"), Is.Not.Null, "Should have WindowResized method");
        Assert.That(type.GetMethod("PressChar"), Is.Not.Null, "Should have PressChar method");
        Assert.That(type.GetMethod("MouseScroll"), Is.Not.Null, "Should have MouseScroll method");
        
        // Check that it implements IDisposable
        Assert.That(typeof(IDisposable).IsAssignableFrom(type), Is.True,
            "ImGuiController should implement IDisposable");
    }

    /// <summary>
    /// Test that ImGuiGameWindow constructor signature is correct
    /// 测试 ImGuiGameWindow 构造函数签名正确
    /// </summary>
    [Test]
    public void TestImguiGameWindow_ConstructorExists()
    {
        var type = typeof(ImGuiGameWindow);
        var constructor = type.GetConstructor(new[] { 
            typeof(GameWindowSettings), 
            typeof(NativeWindowSettings) 
        });
        
        Assert.That(constructor, Is.Not.Null, 
            "ImGuiGameWindow should have constructor accepting GameWindowSettings and NativeWindowSettings");
    }

    /// <summary>
    /// Test that ImGuiController constructor signature is correct
    /// 测试 ImGuiController 构造函数签名正确
    /// </summary>
    [Test]
    public void TestImguiController_ConstructorExists()
    {
        var type = typeof(ImGuiController);
        var constructor = type.GetConstructor(new[] { typeof(int), typeof(int) });
        
        Assert.That(constructor, Is.Not.Null, 
            "ImGuiController should have constructor accepting width and height");
    }

    /// <summary>
    /// Integration test note
    /// 集成测试说明
    /// </summary>
    [Test]
    [Explicit("This is a documentation test explaining how to run integration tests")]
    public void TestImguiWindow_IntegrationTestInstructions()
    {
        Assert.Pass(@"
To test ImGuiGameWindow interactively:

1. Create a console application or modify MainPlayer.cs
2. Add the following code:

    var nativeWindowSettings = new NativeWindowSettings()
    {
        ClientSize = new Vector2i(1280, 720),
        Title = ""SharpFAI Player - ImGui Test"",
        Flags = ContextFlags.ForwardCompatible,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core
    };

    var gameWindowSettings = new GameWindowSettings()
    {
        UpdateFrequency = 60.0
    };

    using (var window = new ImGuiGameWindow(gameWindowSettings, nativeWindowSettings))
    {
        window.Run();
    }

3. Run the application from the main thread

Note: GLFW requires initialization on the process's main thread,
so automated unit tests cannot create OpenGL windows.
");
    }
}
