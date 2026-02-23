using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Runtime.InteropServices;

namespace Dotty;

public unsafe class GhosttyControl : NativeControlHost
{
    private IntPtr _app;
    private IntPtr _surface;
    private DispatcherTimer? _tickTimer;

    public GhosttyControl()
    {
        Focusable = true;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);
        
        // Ensure Ghostty is initialized
        GhosttyManager.Initialize();

        // Start ghostty tick
        _app = GhosttyManager.AppHandle;

        var topLevel = TopLevel.GetTopLevel(this);
        var scale = topLevel?.RenderScaling ?? 1.0;

        var config = LibGhostty.ghostty_surface_config_new();
        config.scale_factor = scale;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            config.platform_tag = GhosttyPlatformTag.MacOS;
            config.platform.nsview = handle.Handle;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            config.platform_tag = GhosttyPlatformTag.Windows;
            config.platform.hwnd = handle.Handle;
        }
        
        _surface = LibGhostty.ghostty_surface_new(_app, &config);

        if (_tickTimer == null)
        {
            _tickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps tick
            };
            _tickTimer.Tick += (s, e) => 
            {
                LibGhostty.ghostty_app_tick(_app);
                if (_surface != IntPtr.Zero)
                {
                    LibGhostty.ghostty_surface_draw(_surface);
                }
            };
            _tickTimer.Start();
        }

        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (_surface != IntPtr.Zero)
        {
            LibGhostty.ghostty_surface_free(_surface);
            _surface = IntPtr.Zero;
        }

        base.DestroyNativeControlCore(control);
    }

    public void HandlePointerPressed(Avalonia.Input.PointerPressedEventArgs e)
    {
        Console.WriteLine("GhosttyControl: PointerPressed - requesting Focus()");
        Focus();
    }

    protected override void OnGotFocus(Avalonia.Input.GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        Console.WriteLine("GhosttyControl: GotFocus");
        if (_surface != IntPtr.Zero)
        {
            LibGhostty.ghostty_surface_set_focus(_surface, true);
        }
    }

    protected override void OnLostFocus(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        Console.WriteLine("GhosttyControl: LostFocus");
        if (_surface != IntPtr.Zero)
        {
            LibGhostty.ghostty_surface_set_focus(_surface, false);
        }
    }

    public void HandleKeyDown(Avalonia.Input.KeyEventArgs e)
    {
        if (_surface != IntPtr.Zero)
        {
            var keycode = MapAvaloniaKey(e.Key);
            var mods = GetMods(e.KeyModifiers);
            var ghosttyKey = new GhosttyInputKey
            {
                action = GhosttyInputAction.Press,
                keycode = keycode,
                mods = mods
            };
            bool result = LibGhostty.ghostty_surface_key(_surface, ghosttyKey);
            Console.WriteLine($"GhosttyControl: KeyDown {e.Key} mapped to {keycode}. Result: {result}");
            e.Handled = result;
        }
    }

    public void HandleKeyUp(Avalonia.Input.KeyEventArgs e)
    {
        if (_surface != IntPtr.Zero)
        {
            var ghosttyKey = new GhosttyInputKey
            {
                action = GhosttyInputAction.Release,
                keycode = MapAvaloniaKey(e.Key),
                mods = GetMods(e.KeyModifiers)
            };
            bool result = LibGhostty.ghostty_surface_key(_surface, ghosttyKey);
            Console.WriteLine($"GhosttyControl: KeyUp {e.Key}. Result: {result}");
            e.Handled = result;
        }
    }

    public void HandleTextInput(Avalonia.Input.TextInputEventArgs e)
    {
        if (_surface != IntPtr.Zero && !string.IsNullOrEmpty(e.Text))
        {
            Console.WriteLine($"GhosttyControl: TextInput '{e.Text}'");
            var bytes = System.Text.Encoding.UTF8.GetBytes(e.Text);
            fixed (byte* ptr = bytes)
            {
                LibGhostty.ghostty_surface_text(_surface, ptr, (nuint)bytes.Length);
            }
            e.Handled = true;
        }
    }

    private GhosttyInputMods GetMods(Avalonia.Input.KeyModifiers mods)
    {
        GhosttyInputMods result = GhosttyInputMods.None;
        if (mods.HasFlag(Avalonia.Input.KeyModifiers.Shift)) result |= GhosttyInputMods.Shift;
        if (mods.HasFlag(Avalonia.Input.KeyModifiers.Control)) result |= GhosttyInputMods.Ctrl;
        if (mods.HasFlag(Avalonia.Input.KeyModifiers.Alt)) result |= GhosttyInputMods.Alt;
        if (mods.HasFlag(Avalonia.Input.KeyModifiers.Meta)) result |= GhosttyInputMods.Super;
        return result;
    }

    private uint MapAvaloniaKey(Avalonia.Input.Key key)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows Virtual-Key Codes (from Avalonia.Input.Key which roughly maps to them)
            return key switch
            {
                Avalonia.Input.Key.A => 0x41,
                Avalonia.Input.Key.S => 0x53,
                Avalonia.Input.Key.D => 0x44,
                Avalonia.Input.Key.F => 0x46,
                Avalonia.Input.Key.H => 0x48,
                Avalonia.Input.Key.G => 0x47,
                Avalonia.Input.Key.Z => 0x5A,
                Avalonia.Input.Key.X => 0x58,
                Avalonia.Input.Key.C => 0x43,
                Avalonia.Input.Key.V => 0x56,
                Avalonia.Input.Key.B => 0x42,
                Avalonia.Input.Key.Q => 0x51,
                Avalonia.Input.Key.W => 0x57,
                Avalonia.Input.Key.E => 0x45,
                Avalonia.Input.Key.R => 0x52,
                Avalonia.Input.Key.Y => 0x59,
                Avalonia.Input.Key.T => 0x54,
                Avalonia.Input.Key.D1 => 0x31,
                Avalonia.Input.Key.D2 => 0x32,
                Avalonia.Input.Key.D3 => 0x33,
                Avalonia.Input.Key.D4 => 0x34,
                Avalonia.Input.Key.D6 => 0x36,
                Avalonia.Input.Key.D5 => 0x35,
                Avalonia.Input.Key.OemPlus => 0xBB,
                Avalonia.Input.Key.D9 => 0x39,
                Avalonia.Input.Key.D7 => 0x37,
                Avalonia.Input.Key.OemMinus => 0xBD,
                Avalonia.Input.Key.D8 => 0x38,
                Avalonia.Input.Key.D0 => 0x30,
                Avalonia.Input.Key.OemCloseBrackets => 0xDD,
                Avalonia.Input.Key.O => 0x4F,
                Avalonia.Input.Key.U => 0x55,
                Avalonia.Input.Key.OemOpenBrackets => 0xDB,
                Avalonia.Input.Key.I => 0x49,
                Avalonia.Input.Key.P => 0x50,
                Avalonia.Input.Key.Enter => 0x0D,
                Avalonia.Input.Key.L => 0x4C,
                Avalonia.Input.Key.J => 0x4A,
                Avalonia.Input.Key.OemQuotes => 0xDE,
                Avalonia.Input.Key.K => 0x4B,
                Avalonia.Input.Key.OemSemicolon => 0xBA,
                Avalonia.Input.Key.OemPipe => 0xDC,
                Avalonia.Input.Key.OemComma => 0xBC,
                Avalonia.Input.Key.OemQuestion => 0xBF,
                Avalonia.Input.Key.N => 0x4E,
                Avalonia.Input.Key.M => 0x4D,
                Avalonia.Input.Key.OemPeriod => 0xBE,
                Avalonia.Input.Key.Tab => 0x09,
                Avalonia.Input.Key.Space => 0x20,
                Avalonia.Input.Key.OemTilde => 0xC0,
                Avalonia.Input.Key.Back => 0x08,
                Avalonia.Input.Key.Escape => 0x1B,
                
                Avalonia.Input.Key.LeftCtrl => 0xA2,
                Avalonia.Input.Key.LeftShift => 0xA0,
                Avalonia.Input.Key.LeftAlt => 0xA4,
                Avalonia.Input.Key.LWin => 0x5B,
                Avalonia.Input.Key.RightCtrl => 0xA3,
                Avalonia.Input.Key.RightShift => 0xA1,
                Avalonia.Input.Key.RightAlt => 0xA5,
                Avalonia.Input.Key.RWin => 0x5C,
                
                Avalonia.Input.Key.Left => 0x25,
                Avalonia.Input.Key.Right => 0x27,
                Avalonia.Input.Key.Down => 0x28,
                Avalonia.Input.Key.Up => 0x26,
                
                _ => 0xFFFF // Unknown
            };
        }

        // macOS native keycodes (CGKeyCode)
        return key switch
        {
            Avalonia.Input.Key.A => 0x00,
            Avalonia.Input.Key.S => 0x01,
            Avalonia.Input.Key.D => 0x02,
            Avalonia.Input.Key.F => 0x03,
            Avalonia.Input.Key.H => 0x04,
            Avalonia.Input.Key.G => 0x05,
            Avalonia.Input.Key.Z => 0x06,
            Avalonia.Input.Key.X => 0x07,
            Avalonia.Input.Key.C => 0x08,
            Avalonia.Input.Key.V => 0x09,
            Avalonia.Input.Key.B => 0x0b,
            Avalonia.Input.Key.Q => 0x0c,
            Avalonia.Input.Key.W => 0x0d,
            Avalonia.Input.Key.E => 0x0e,
            Avalonia.Input.Key.R => 0x0f,
            Avalonia.Input.Key.Y => 0x10,
            Avalonia.Input.Key.T => 0x11,
            Avalonia.Input.Key.D1 => 0x12,
            Avalonia.Input.Key.D2 => 0x13,
            Avalonia.Input.Key.D3 => 0x14,
            Avalonia.Input.Key.D4 => 0x15,
            Avalonia.Input.Key.D6 => 0x16,
            Avalonia.Input.Key.D5 => 0x17,
            Avalonia.Input.Key.OemPlus => 0x18, // Equal
            Avalonia.Input.Key.D9 => 0x19,
            Avalonia.Input.Key.D7 => 0x1a,
            Avalonia.Input.Key.OemMinus => 0x1b, // Minus
            Avalonia.Input.Key.D8 => 0x1c,
            Avalonia.Input.Key.D0 => 0x1d,
            Avalonia.Input.Key.OemCloseBrackets => 0x1e,
            Avalonia.Input.Key.O => 0x1f,
            Avalonia.Input.Key.U => 0x20,
            Avalonia.Input.Key.OemOpenBrackets => 0x21,
            Avalonia.Input.Key.I => 0x22,
            Avalonia.Input.Key.P => 0x23,
            Avalonia.Input.Key.Enter => 0x24,
            Avalonia.Input.Key.L => 0x25,
            Avalonia.Input.Key.J => 0x26,
            Avalonia.Input.Key.OemQuotes => 0x27,
            Avalonia.Input.Key.K => 0x28,
            Avalonia.Input.Key.OemSemicolon => 0x29,
            Avalonia.Input.Key.OemPipe => 0x2a, // Backslash
            Avalonia.Input.Key.OemComma => 0x2b,
            Avalonia.Input.Key.OemQuestion => 0x2c, // Slash
            Avalonia.Input.Key.N => 0x2d,
            Avalonia.Input.Key.M => 0x2e,
            Avalonia.Input.Key.OemPeriod => 0x2f,
            Avalonia.Input.Key.Tab => 0x30,
            Avalonia.Input.Key.Space => 0x31,
            Avalonia.Input.Key.OemTilde => 0x32, // Backquote
            Avalonia.Input.Key.Back => 0x33,
            Avalonia.Input.Key.Escape => 0x35,
            
            Avalonia.Input.Key.LeftCtrl => 0x3b,
            Avalonia.Input.Key.LeftShift => 0x38,
            Avalonia.Input.Key.LeftAlt => 0x3a,
            Avalonia.Input.Key.LWin => 0x37,
            Avalonia.Input.Key.RightCtrl => 0x3e,
            Avalonia.Input.Key.RightShift => 0x3c,
            Avalonia.Input.Key.RightAlt => 0x3d,
            Avalonia.Input.Key.RWin => 0x36,
            
            Avalonia.Input.Key.Left => 0x7b,
            Avalonia.Input.Key.Right => 0x7c,
            Avalonia.Input.Key.Down => 0x7d,
            Avalonia.Input.Key.Up => 0x7e,
            
            _ => 0xFFFF // Unknown
        };
    }
}

public static unsafe class GhosttyManager
{
    public static IntPtr AppHandle { get; private set; }
    private static bool _initialized;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WakeupCb(IntPtr userdata) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static bool ActionCb(IntPtr app, IntPtr target, IntPtr action) { return false; }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ReadClipboardCb(IntPtr userdata, int type, IntPtr req) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ConfirmReadClipboardCb(IntPtr userdata, IntPtr text, IntPtr req, int reqType) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WriteClipboardCb(IntPtr userdata, int type, IntPtr content, nuint len, bool confirm) { }

    public static void Initialize()
    {
        if (_initialized) return;

        // Init ghostty logs/environment
        LibGhostty.ghostty_init(0, null);

        var config = LibGhostty.ghostty_config_new();
        LibGhostty.ghostty_config_load_default_files(config);
        LibGhostty.ghostty_config_finalize(config);

        var runtimeConfig = new GhosttyRuntimeConfig
        {
            wakeup_cb = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&WakeupCb,
            action_cb = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, bool>)&ActionCb,
            read_clipboard_cb = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, void>)&ReadClipboardCb,
            confirm_read_clipboard_cb = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, void>)&ConfirmReadClipboardCb,
            write_clipboard_cb = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, nuint, bool, void>)&WriteClipboardCb
        };
        
        AppHandle = LibGhostty.ghostty_app_new(&runtimeConfig, config);
        
        _initialized = true;
    }
}
