using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClarionDbg.App;

public partial class AttachWindow : Window
{
    public int SelectedPid { get; private set; }
    public string? SelectedPath { get; private set; }

    public sealed record ProcRow(int Pid, string Name, string Path);

    public AttachWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Populate();
    }

    void Refresh_Click(object sender, RoutedEventArgs e) => Populate();

    void Populate()
    {
        bool clarionOnly = ChkClarionOnly.IsChecked == true;
        var rows = new List<ProcRow>();
        foreach (var p in Process.GetProcesses())
        {
            string path;
            try { path = p.MainModule?.FileName ?? ""; }
            catch { continue; }          // 64-bit / protected processes we can't read
            if (string.IsNullOrEmpty(path)) continue;
            if (clarionOnly && !HasClarionDebug(path)) continue;
            rows.Add(new ProcRow(p.Id, p.ProcessName, path));
        }
        Grid.ItemsSource = rows.OrderBy(r => r.Name).ToList();
    }

    /// <summary>Cheap check: does the EXE contain a ".cwdebug" PE section (a Clarion Debug build)?</summary>
    static bool HasClarionDebug(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            int n = (int)Math.Min(fs.Length, 16384);
            var buf = new byte[n];
            fs.ReadExactly(buf, 0, n);
            var needle = ".cwdebug"u8;
            for (int i = 0; i + needle.Length <= n; i++)
            {
                bool hit = true;
                for (int j = 0; j < needle.Length; j++) if (buf[i + j] != needle[j]) { hit = false; break; }
                if (hit) return true;
            }
        }
        catch { }
        return false;
    }

    void Grid_DoubleClick(object sender, MouseButtonEventArgs e) => Attach_Click(sender, e);

    void Attach_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not ProcRow r) { return; }
        SelectedPid = r.Pid;
        SelectedPath = r.Path;
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
