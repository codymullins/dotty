using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace Dotty;

public static unsafe partial class LibGhostty
{
    private const string LibraryName = "Native/libghostty.dylib";

    [LibraryImport(LibraryName)]
    public static partial int ghostty_init(nuint argc, byte** argv);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_cli_try_action();

    [LibraryImport(LibraryName)]
    public static partial IntPtr ghostty_config_new();

    [LibraryImport(LibraryName)]
    public static partial void ghostty_config_free(IntPtr config);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_config_load_default_files(IntPtr config);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_config_finalize(IntPtr config);

    [LibraryImport(LibraryName)]
    public static partial IntPtr ghostty_app_new(GhosttyRuntimeConfig* runtimeConfig, IntPtr ghosttyConfig);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_app_free(IntPtr app);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_app_tick(IntPtr app);

    [LibraryImport(LibraryName)]
    public static partial GhosttySurfaceConfig ghostty_surface_config_new();

    [LibraryImport(LibraryName)]
    public static partial IntPtr ghostty_surface_new(IntPtr app, GhosttySurfaceConfig* config);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_free(IntPtr surface);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_set_focus(IntPtr surface, [MarshalAs(UnmanagedType.I1)] bool focus);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_draw(IntPtr surface);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_set_size(IntPtr surface, uint width, uint height);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_set_content_scale(IntPtr surface, double scaleX, double scaleY);

    [LibraryImport(LibraryName)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool ghostty_surface_key(IntPtr surface, GhosttyInputKey key);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_surface_text(IntPtr surface, byte* text, nuint len);
}

[StructLayout(LayoutKind.Sequential)]
public struct GhosttyRuntimeConfig
{
    public IntPtr userdata;
    public bool supports_selection_clipboard;
    public IntPtr wakeup_cb;
    public IntPtr action_cb;
    public IntPtr read_clipboard_cb;
    public IntPtr confirm_read_clipboard_cb;
    public IntPtr write_clipboard_cb;
    public IntPtr close_surface_cb;
}

[StructLayout(LayoutKind.Sequential)]
public struct GhosttySurfaceConfig
{
    public GhosttyPlatformTag platform_tag;
    public GhosttyPlatformUnion platform;
    public IntPtr userdata;
    public double scale_factor;
    public float font_size;
    public IntPtr working_directory;
    public IntPtr command;
    public IntPtr env_vars;
    public nuint env_var_count;
    public IntPtr initial_input;
    public bool wait_after_command;
    public uint context;
}

public enum GhosttyPlatformTag : uint
{
    Invalid = 0,
    MacOS = 1,
    iOS = 2
}

[StructLayout(LayoutKind.Explicit)]
public struct GhosttyPlatformUnion
{
    [FieldOffset(0)] public IntPtr nsview;
    [FieldOffset(0)] public IntPtr uiview;
}

public enum GhosttyInputAction : uint
{
    Release = 0,
    Press = 1,
    Repeat = 2
}

[Flags]
public enum GhosttyInputMods : uint
{
    None = 0,
    Shift = 1 << 0,
    Ctrl = 1 << 1,
    Alt = 1 << 2,
    Super = 1 << 3,
    Caps = 1 << 4,
    Num = 1 << 5,
    ShiftRight = 1 << 6,
    CtrlRight = 1 << 7,
    AltRight = 1 << 8,
    SuperRight = 1 << 9
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct GhosttyInputKey
{
    public GhosttyInputAction action;
    public GhosttyInputMods mods;
    public GhosttyInputMods consumed_mods;
    public uint keycode;
    public byte* text;
    public uint unshifted_codepoint;
    public bool composing;
}
