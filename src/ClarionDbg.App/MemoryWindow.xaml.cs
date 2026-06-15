using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ClarionDbg.Engine;

namespace ClarionDbg.App;

public partial class MemoryWindow : Window
{
    readonly DebugSession _session;
    uint _addr;
    const int Bytes = 256;
    readonly System.Windows.Threading.DispatcherTimer _timer =
        new() { Interval = TimeSpan.FromMilliseconds(500) };

    public MemoryWindow(DebugSession session, uint addr, string label)
    {
        InitializeComponent();
        _session = session;
        _addr = addr;
        Title = $"Memory — {label}";
        TxtAddr.Text = $"0x{addr:X8}";
        _timer.Tick += (_, _) => { if (ChkLive.IsChecked == true) Render(); };
        Loaded += (_, _) => { Render(); _timer.Start(); };
        Closed += (_, _) => _timer.Stop();
    }

    void Addr_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Refresh_Click(sender, e); }

    void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var s = TxtAddr.Text.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a)) _addr = a;
        Render();
    }

    void Render()
    {
        var data = _session.ReadMemory(_addr, Bytes);
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += 16)
        {
            sb.Append($"{_addr + (uint)i:X8}  ");
            for (int j = 0; j < 16; j++)
            {
                sb.Append(i + j < data.Length ? data[i + j].ToString("X2") : "  ").Append(' ');
                if (j == 7) sb.Append(' ');
            }
            sb.Append(' ');
            for (int j = 0; j < 16 && i + j < data.Length; j++)
            {
                byte b = data[i + j];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            sb.Append('\n');
        }
        if (data.Length == 0) sb.Append("(unreadable — process not running, or address not mapped)");
        // preserve scroll position on live refresh
        int caret = TxtDump.SelectionStart;
        TxtDump.Text = sb.ToString();
        TxtDump.SelectionStart = Math.Min(caret, TxtDump.Text.Length);
    }
}
