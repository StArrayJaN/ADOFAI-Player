using Android.Content;
using Android.Opengl;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Editor.Core.Platform.Graphics;

namespace SharpFAI.Editor.Platform.Android;

public class InputProcessor
{
    private bool lastWantTextInput = false;
    public static InputProcessor Instance = new ();

    private AndroidKeyboardState? _keyboardState;
    private AndroidMouseState? _mouseState;

    public void SetKeyboardState(AndroidKeyboardState keyboardState)
    {
        _keyboardState = keyboardState;
    }

    public void SetMouseState(AndroidMouseState mouseState)
    {
        _mouseState = mouseState;
    }
    public void ProcessTouchEvent(MotionEvent e)
    {
        var io = ImGui.GetIO();
        var action = e.Action;
        action &= MotionEventActions.Mask;
        var pointIndex = (int)(e.Action & MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
        switch (e.GetToolType(pointIndex))
        {
            case MotionEventToolType.Mouse:
                io.AddMouseSourceEvent(ImGuiMouseSource.Mouse);
                break;
            case MotionEventToolType.Stylus:
            case MotionEventToolType.Eraser:
                io.AddMouseSourceEvent(ImGuiMouseSource.Pen);
                break;
            case MotionEventToolType.Finger:
            default:
                io.AddMouseSourceEvent(ImGuiMouseSource.TouchScreen);
                break;
        }

        switch (action)
        {
            case MotionEventActions.Down:
            case MotionEventActions.Up:
                var tool = e.GetToolType(pointIndex);
                if (tool == MotionEventToolType.Finger || tool == MotionEventToolType.Unknown)
                {
                    float x = e.GetX(pointIndex);
                    float y = e.GetY(pointIndex);
                    _mouseState?.SetPosition(x, y);
                    io.AddMousePosEvent(x, y);
                    bool pressed = e.Action == MotionEventActions.Down;
                    _mouseState?.SetButtonState(MouseButton.Left, pressed);
                    io.AddMouseButtonEvent(0, pressed);
                }
                break;
            case MotionEventActions.ButtonPress:
            case MotionEventActions.ButtonRelease:
                var buttonState = e.ButtonState;
                bool leftPressed = (buttonState & MotionEventButtonState.Primary) != 0;
                bool rightPressed = (buttonState & MotionEventButtonState.Secondary) != 0;
                bool middlePressed = (buttonState & MotionEventButtonState.Tertiary) != 0;
                _mouseState?.SetButtonState(MouseButton.Left, leftPressed);
                _mouseState?.SetButtonState(MouseButton.Right, rightPressed);
                _mouseState?.SetButtonState(MouseButton.Middle, middlePressed);
                io.AddMouseButtonEvent(0, leftPressed);
                io.AddMouseButtonEvent(1, rightPressed);
                io.AddMouseButtonEvent(2, middlePressed);
                break;
            case MotionEventActions.HoverMove:
            case MotionEventActions.Move:
                float moveX = e.GetX(pointIndex);
                float moveY = e.GetY(pointIndex);
                _mouseState?.SetPosition(moveX, moveY);
                io.AddMousePosEvent(moveX, moveY);
                break;
            case MotionEventActions.Scroll:
                float scrollX = e.GetAxisValue(Axis.Hscroll, pointIndex);
                float scrollY = e.GetAxisValue(Axis.Vscroll, pointIndex);
                _mouseState?.SetScroll(scrollX, scrollY);
                io.AddMouseWheelEvent(scrollX, scrollY);
                break;
        }
    }
    public void ProcessKeyEvent(KeyEvent e)
    {
        if (e == null) return;

        var isPress = e.Action == KeyEventActions.Down;
        var io = ImGui.GetIO();

        // 处理修饰键状态
        io.AddKeyEvent(ImGuiKey.ModCtrl, e.IsCtrlPressed);
        io.AddKeyEvent(ImGuiKey.ModShift, e.IsShiftPressed);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.IsAltPressed);
        io.AddKeyEvent(ImGuiKey.ModSuper, e.IsMetaPressed);

        // 映射 Android Keycode 到 ImGui Key
        ImGuiKey imguiKey = MapAndroidKeyToImGuiKey(e.KeyCode);

        if (imguiKey != ImGuiKey.None)
        {
            io.AddKeyEvent(imguiKey, isPress);
            io.SetKeyEventNativeData(imguiKey, (int)e.KeyCode, e.ScanCode);
            Log.Error(nameof(InputProcessor), $"Key event: {e.KeyCode} -> {imguiKey}, pressed: {isPress}");
        }

        // 处理字符输入（仅在按下时）
        if (isPress && e.UnicodeChar != 0)
        {
            io.AddInputCharacter((uint)e.UnicodeChar);
            Log.Error(nameof(InputProcessor), $"Input character: {(char)e.UnicodeChar} (0x{e.UnicodeChar:X})");
        }
    }
    public void OnImGUIRender(GLSurfaceView view)
    {
        bool currentWantTextInput = ImGui.GetIO().WantTextInput;

        // 状态变化时才操作输入法
        if (currentWantTextInput != lastWantTextInput)
        {
            InputMethodManager? imm = InputMethodManager.FromContext(view.Context);
            if (imm != null)
            {
                imm.ToggleSoftInputFromWindow(view.WindowToken, ShowSoftInputFlags.Forced, HideSoftInputFlags.None);
            }

            lastWantTextInput = currentWantTextInput;
        }
    }
    /// <summary>
    /// 映射 Android Keycode 到 ImGui Key
    /// </summary>
    private ImGuiKey MapAndroidKeyToImGuiKey(Keycode keyCode)
    {
        return keyCode switch
        {
            Keycode.Tab => ImGuiKey.Tab,
            Keycode.DpadLeft => ImGuiKey.LeftArrow,
            Keycode.DpadRight => ImGuiKey.RightArrow,
            Keycode.DpadUp => ImGuiKey.UpArrow,
            Keycode.DpadDown => ImGuiKey.DownArrow,
            Keycode.PageUp => ImGuiKey.PageUp,
            Keycode.PageDown => ImGuiKey.PageDown,
            Keycode.MoveHome => ImGuiKey.Home,
            Keycode.MoveEnd => ImGuiKey.End,
            Keycode.Insert => ImGuiKey.Insert,
            Keycode.ForwardDel => ImGuiKey.Delete,
            Keycode.Del => ImGuiKey.Backspace,
            Keycode.Space => ImGuiKey.Space,
            Keycode.Enter => ImGuiKey.Enter,
            Keycode.NumpadEnter => ImGuiKey.KeypadEnter,
            Keycode.Escape => ImGuiKey.Escape,
            Keycode.Apostrophe => ImGuiKey.Apostrophe,
            Keycode.Comma => ImGuiKey.Comma,
            Keycode.Minus => ImGuiKey.Minus,
            Keycode.Period => ImGuiKey.Period,
            Keycode.Slash => ImGuiKey.Slash,
            Keycode.Semicolon => ImGuiKey.Semicolon,
            Keycode.Equals => ImGuiKey.Equal,
            Keycode.LeftBracket => ImGuiKey.LeftBracket,
            Keycode.Backslash => ImGuiKey.Backslash,
            Keycode.RightBracket => ImGuiKey.RightBracket,
            Keycode.Grave => ImGuiKey.GraveAccent,
            Keycode.CapsLock => ImGuiKey.CapsLock,
            Keycode.ScrollLock => ImGuiKey.ScrollLock,
            Keycode.NumLock => ImGuiKey.NumLock,
            Keycode.Sysrq => ImGuiKey.PrintScreen,
            Keycode.Break => ImGuiKey.Pause,
            Keycode.Numpad0 => ImGuiKey.Keypad0,
            Keycode.Numpad1 => ImGuiKey.Keypad1,
            Keycode.Numpad2 => ImGuiKey.Keypad2,
            Keycode.Numpad3 => ImGuiKey.Keypad3,
            Keycode.Numpad4 => ImGuiKey.Keypad4,
            Keycode.Numpad5 => ImGuiKey.Keypad5,
            Keycode.Numpad6 => ImGuiKey.Keypad6,
            Keycode.Numpad7 => ImGuiKey.Keypad7,
            Keycode.Numpad8 => ImGuiKey.Keypad8,
            Keycode.Numpad9 => ImGuiKey.Keypad9,
            Keycode.NumpadDot => ImGuiKey.KeypadDecimal,
            Keycode.NumpadDivide => ImGuiKey.KeypadDivide,
            Keycode.NumpadMultiply => ImGuiKey.KeypadMultiply,
            Keycode.NumpadSubtract => ImGuiKey.KeypadSubtract,
            Keycode.NumpadAdd => ImGuiKey.KeypadAdd,
            Keycode.NumpadEquals => ImGuiKey.KeypadEqual,
            Keycode.CtrlLeft => ImGuiKey.LeftCtrl,
            Keycode.ShiftLeft => ImGuiKey.LeftShift,
            Keycode.AltLeft => ImGuiKey.LeftAlt,
            Keycode.MetaLeft => ImGuiKey.LeftSuper,
            Keycode.CtrlRight => ImGuiKey.RightCtrl,
            Keycode.ShiftRight => ImGuiKey.RightShift,
            Keycode.AltRight => ImGuiKey.RightAlt,
            Keycode.MetaRight => ImGuiKey.RightSuper,
            Keycode.Menu => ImGuiKey.Menu,
            Keycode.Num0 => ImGuiKey._0,
            Keycode.Num1 => ImGuiKey._1,
            Keycode.Num2 => ImGuiKey._2,
            Keycode.Num3 => ImGuiKey._3,
            Keycode.Num4 => ImGuiKey._4,
            Keycode.Num5 => ImGuiKey._5,
            Keycode.Num6 => ImGuiKey._6,
            Keycode.Num7 => ImGuiKey._7,
            Keycode.Num8 => ImGuiKey._8,
            Keycode.Num9 => ImGuiKey._9,
            Keycode.A => ImGuiKey.A,
            Keycode.B => ImGuiKey.B,
            Keycode.C => ImGuiKey.C,
            Keycode.D => ImGuiKey.D,
            Keycode.E => ImGuiKey.E,
            Keycode.F => ImGuiKey.F,
            Keycode.G => ImGuiKey.G,
            Keycode.H => ImGuiKey.H,
            Keycode.I => ImGuiKey.I,
            Keycode.J => ImGuiKey.J,
            Keycode.K => ImGuiKey.K,
            Keycode.L => ImGuiKey.L,
            Keycode.M => ImGuiKey.M,
            Keycode.N => ImGuiKey.N,
            Keycode.O => ImGuiKey.O,
            Keycode.P => ImGuiKey.P,
            Keycode.Q => ImGuiKey.Q,
            Keycode.R => ImGuiKey.R,
            Keycode.S => ImGuiKey.S,
            Keycode.T => ImGuiKey.T,
            Keycode.U => ImGuiKey.U,
            Keycode.V => ImGuiKey.V,
            Keycode.W => ImGuiKey.W,
            Keycode.X => ImGuiKey.X,
            Keycode.Y => ImGuiKey.Y,
            Keycode.Z => ImGuiKey.Z,
            Keycode.F1 => ImGuiKey.F1,
            Keycode.F2 => ImGuiKey.F2,
            Keycode.F3 => ImGuiKey.F3,
            Keycode.F4 => ImGuiKey.F4,
            Keycode.F5 => ImGuiKey.F5,
            Keycode.F6 => ImGuiKey.F6,
            Keycode.F7 => ImGuiKey.F7,
            Keycode.F8 => ImGuiKey.F8,
            Keycode.F9 => ImGuiKey.F9,
            Keycode.F10 => ImGuiKey.F10,
            Keycode.F11 => ImGuiKey.F11,
            Keycode.F12 => ImGuiKey.F12,
            _ => ImGuiKey.None
        };
    }
}