using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ClarionDbg.Engine;

namespace ClarionDbg.App;

public partial class DisassemblyWindow : Window
{
    readonly DebugSession _session;
    const int Count = 60;
    readonly System.Windows.Threading.DispatcherTimer _timer =
        new() { Interval = TimeSpan.FromMilliseconds(600) };

    public DisassemblyWindow(DebugSession session)
    {
        InitializeComponent();
        _session = session;
        _timer.Tick += (_, _) => { if (ChkFollow.IsChecked == true) RenderAtEip(); };
        Loaded += (_, _) => { RenderAtEip(); _timer.Start(); };
        Closed += (_, _) => _timer.Stop();
    }

    void RenderAtEip()
    {
        uint eip = _session.CurrentEip;
        if (eip == 0) return;
        TxtAddr.Text = $"0x{eip:X8}";
        Render(eip, eip);
    }

    void Addr_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Refresh_Click(sender, e); }

    void Refresh_Click(object sender, RoutedEventArgs e)
    {
        var s = TxtAddr.Text.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a))
            Render(a, _session.CurrentEip);
    }

    void Render(uint addr, uint eip)
    {
        var rows = _session.Disassemble(addr, Count).Select(d => new DisRow
        {
            Marker = d.Addr == eip ? "▶" : "",
            Addr = $"0x{d.Addr:X8}",
            Bytes = d.Bytes,
            Text = d.Text,
            Source = d.Source ?? "",
            IsCurrent = d.Addr == eip
        }).ToList();
        Grid.ItemsSource = rows;
        var cur = rows.FirstOrDefault(r => r.IsCurrent);
        if (cur != null) Grid.ScrollIntoView(cur);
    }

    sealed class DisRow
    {
        public string Marker { get; set; } = "";
        public string Addr { get; set; } = "";
        public string Bytes { get; set; } = "";
        public string Text { get; set; } = "";
        public string Source { get; set; } = "";
        public bool IsCurrent { get; set; }
    }
}
