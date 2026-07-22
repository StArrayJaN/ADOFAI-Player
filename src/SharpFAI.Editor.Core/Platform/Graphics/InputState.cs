using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace SharpFAI.Editor.Core.Platform.Graphics;

/// <summary>
/// 统一的键盘状态接口
/// </summary>
public interface IKeyboardState
{
    bool IsKeyDown(Keys key);
    bool IsKeyPressed(Keys key);
}

/// <summary>
/// 统一的鼠标状态接口
/// </summary>
public interface IMouseState
{
    float X { get; }
    float Y { get; }
    Vector2 Scroll { get; }
    bool IsButtonDown(MouseButton button);
}

/// <summary>
/// Desktop 平台的键盘状态适配器
/// </summary>
public class DesktopKeyboardState : IKeyboardState
{
    private readonly KeyboardState _keyboardState;

    public DesktopKeyboardState(KeyboardState keyboardState)
    {
        _keyboardState = keyboardState;
    }

    public bool IsKeyDown(Keys key) => _keyboardState.IsKeyDown(key);
    public bool IsKeyPressed(Keys key) => _keyboardState.IsKeyPressed(key);
}

/// <summary>
/// Desktop 平台的鼠标状态适配器
/// </summary>
public class DesktopMouseState : IMouseState
{
    private readonly MouseState _mouseState;

    public DesktopMouseState(MouseState mouseState)
    {
        _mouseState = mouseState;
    }

    public float X => _mouseState.X;
    public float Y => _mouseState.Y;
    public Vector2 Scroll => _mouseState.Scroll;
    public bool IsButtonDown(MouseButton button) => _mouseState.IsButtonDown(button);
}

/// <summary>
/// Android 平台的键盘状态实现
/// </summary>
public class AndroidKeyboardState : IKeyboardState
{
    private readonly Dictionary<Keys, bool> _keyStates = new();

    public void SetKeyState(Keys key, bool pressed)
    {
        _keyStates[key] = pressed;
    }

    public bool IsKeyDown(Keys key)
    {
        return _keyStates.TryGetValue(key, out var pressed) && pressed;
    }

    public bool IsKeyPressed(Keys key)
    {
        return _keyStates.TryGetValue(key, out var pressed) && pressed;
    }
}

/// <summary>
/// Android 平台的鼠标状态实现
/// </summary>
public class AndroidMouseState : IMouseState
{
    private float _x;
    private float _y;
    private Vector2 _scroll;
    private readonly Dictionary<MouseButton, bool> _buttonStates = new();

    public void SetPosition(float x, float y)
    {
        _x = x;
        _y = y;
    }

    public void SetScroll(float x, float y)
    {
        _scroll = new Vector2(x, y);
    }

    public void SetButtonState(MouseButton button, bool pressed)
    {
        _buttonStates[button] = pressed;
    }

    public float X => _x;
    public float Y => _y;
    public Vector2 Scroll => _scroll;

    public bool IsButtonDown(MouseButton button)
    {
        return _buttonStates.TryGetValue(button, out var pressed) && pressed;
    }
}
