using Avalonia.Controls;

namespace Dotty;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Forward inputs from the transparent border to the terminal control explicitly
        // This avoids recursive raising of routed events back up the tree.
        InputCatcher.PointerPressed += (s, e) => TerminalControl.HandlePointerPressed(e);
        InputCatcher.KeyDown += (s, e) => TerminalControl.HandleKeyDown(e);
        InputCatcher.KeyUp += (s, e) => TerminalControl.HandleKeyUp(e);
        InputCatcher.TextInput += (s, e) => TerminalControl.HandleTextInput(e);
        
        this.Opened += (s, e) => InputCatcher.Focus();
    }
}
