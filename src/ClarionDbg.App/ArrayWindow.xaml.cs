using System.Windows;
using ClarionDbg.Engine;

namespace ClarionDbg.App;

public partial class ArrayWindow : Window
{
    public ArrayWindow(DebugSession session, string name, string typeName,
                       uint baseAddr, int elemSize, int count)
    {
        InitializeComponent();
        TxtHeader.Text = $"{name}   {typeName}   —   {count} × {elemSize} bytes @ 0x{baseAddr:X8}";
        var rows = new List<Row>();
        for (int i = 0; i < count && i < 4096; i++)
        {
            uint a = (uint)(baseAddr + (long)i * elemSize);
            var v = session.ReadValueAt($"[{i}]", a, elemSize);
            rows.Add(new Row { Index = $"[{i + 1}]", Address = $"0x{a:X8}", Value = v.Display });
        }
        Grid.ItemsSource = rows;
    }

    sealed class Row
    {
        public string Index { get; set; } = "";
        public string Address { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
