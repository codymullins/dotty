using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace DottyTest;

public static unsafe partial class Program
{
    private const string LibraryName = "Native/libghostty.dylib";

    [LibraryImport(LibraryName)]
    public static partial int ghostty_init(nuint argc, byte** argv);

    [LibraryImport(LibraryName)]
    public static partial IntPtr ghostty_config_new();

    [LibraryImport(LibraryName)]
    public static partial void ghostty_config_load_default_files(IntPtr config);

    [LibraryImport(LibraryName)]
    public static partial void ghostty_config_finalize(IntPtr config);

    [LibraryImport(LibraryName)]
    public static partial IntPtr ghostty_app_new(GhosttyRuntimeConfig* runtimeConfig, IntPtr ghosttyConfig);

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting ghostty test...");
        try 
        {
            Console.WriteLine("Calling ghostty_init...");
            ghostty_init(0, null);
            Console.WriteLine("Initialization successful.");

            Console.WriteLine("Calling ghostty_config_new...");
            IntPtr config = ghostty_config_new();
            Console.WriteLine($"Config created: {config}");

            if (config != IntPtr.Zero)
            {
                ghostty_config_load_default_files(config);
                ghostty_config_finalize(config);
                Console.WriteLine("Config finalized.");
                
                Console.WriteLine("Creating runtime config...");
                var runtimeConfig = new GhosttyRuntimeConfig();
                Console.WriteLine("Calling ghostty_app_new...");
                IntPtr app = ghostty_app_new(&runtimeConfig, config);
                Console.WriteLine($"App created: {app}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
        }
        Console.WriteLine("Test complete.");
    }
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
