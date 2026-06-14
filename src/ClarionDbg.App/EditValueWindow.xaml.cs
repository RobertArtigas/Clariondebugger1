using System.Windows;
using System.Windows.Input;

namespace ClarionDbg.App;

public partial class EditValueWindow : Window
{
    public string NewValue => TxtValue.Text;

    public EditValueWindow(string name, string type, uint addr, string current)
    {
        InitializeComponent();
        TxtName.Text = name;
        TxtInfo.Text = $"{type}   @ 0x{addr:X8}";
        // strip the [tls] marker and surrounding quotes for easy editing
        current = current.Replace("[tls] ", "");
        if (current.Length >= 2 && current[0] == '\'' && current[^1] == '\'') current = current[1..^1];
        TxtValue.Text = current;
        Loaded += (_, _) => { TxtValue.Focus(); TxtValue.SelectAll(); };
    }

    void Ok_Click(object sender, RoutedEventArgs e) { DialogResult = true; }
    void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    void TxtValue_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { DialogResult = true; } }
}
