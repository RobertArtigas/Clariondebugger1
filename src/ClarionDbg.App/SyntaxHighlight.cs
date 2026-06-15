using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ClarionDbg.App;

/// <summary>
/// Attached property that renders a line of Clarion source into colored runs on a TextBlock:
/// keywords, strings, comments (<c>!</c>), numbers, and column-1 labels. Professional VS-style
/// palette, no purple.
/// </summary>
public static class SyntaxHighlight
{
    static readonly Brush Keyword = Frozen("#FF569CD6");   // blue
    static readonly Brush Str     = Frozen("#FFCE9178");   // soft orange
    static readonly Brush Comment = Frozen("#FF6A9955");   // green
    static readonly Brush Number  = Frozen("#FFB5CEA8");   // pale green
    static readonly Brush Label   = Frozen("#FF4EC9B0");   // teal
    static readonly Brush Plain   = Frozen("#FFDCDCDC");   // default text

    static Brush Frozen(string hex)
    { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }

    static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "PROGRAM","MEMBER","MAP","MODULE","CODE","DATA","PROCEDURE","FUNCTION","ROUTINE","CLASS",
        "INTERFACE","APPLICATION","WINDOW","REPORT","QUEUE","GROUP","FILE","RECORD","VIEW","KEY",
        "INDEX","BLOB","MEMO","IF","THEN","ELSE","ELSIF","END","CASE","OF","OROF","LOOP","WHILE",
        "UNTIL","BREAK","CYCLE","DO","EXIT","RETURN","STOP","HALT","ACCEPT","NEW","DISPOSE","NULL",
        "SELF","PARENT","BEGIN","SECTION","INCLUDE","ONCE","OMIT","COMPILE","EQUATE","ITEMIZE",
        "AND","OR","XOR","NOT","BAND","BOR","BXOR","CHOOSE","TRUE","FALSE","LONG","ULONG","SHORT",
        "USHORT","BYTE","SIGNED","UNSIGNED","STRING","CSTRING","PSTRING","DECIMAL","PDECIMAL",
        "REAL","SREAL","DATE","TIME","BFLOAT4","BFLOAT8","ANY","LIKE","DIM","OVER","NAME","PRE",
        "THREAD","STATIC","PRIVATE","PROTECTED","VIRTUAL","DERIVED","TYPE","C","PASCAL","RAW"
    };

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(SyntaxHighlight),
        new PropertyMetadata(null, OnTextChanged));

    public static void SetText(DependencyObject o, string v) => o.SetValue(TextProperty, v);
    public static string? GetText(DependencyObject o) => (string?)o.GetValue(TextProperty);

    static void OnTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is not TextBlock tb) return;
        tb.Inlines.Clear();
        var line = e.NewValue as string ?? "";
        foreach (var (text, brush) in Tokenize(line))
            tb.Inlines.Add(new Run(text) { Foreground = brush });
    }

    static IEnumerable<(string, Brush)> Tokenize(string s)
    {
        int i = 0, n = s.Length;
        // a label (or statement) that begins at column 1 with no leading whitespace
        bool col1 = n > 0 && !char.IsWhiteSpace(s[0]);
        bool firstToken = true;
        while (i < n)
        {
            char c = s[i];
            if (c == '!')                                  // comment to end of line
            { yield return (s[i..], Comment); yield break; }
            if (c == '\'')                                 // string literal
            {
                int j = i + 1;
                while (j < n && s[j] != '\'') j++;
                if (j < n) j++;                            // include closing quote
                yield return (s[i..j], Str); i = j; firstToken = false; continue;
            }
            if (char.IsLetter(c) || c == '_')              // identifier / keyword / label
            {
                int j = i;
                while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '_' || s[j] == ':')) j++;
                string word = s[i..j];
                Brush b = Keywords.Contains(word) ? Keyword
                        : (firstToken && col1 ? Label : Plain);
                yield return (word, b); i = j; firstToken = false; continue;
            }
            if (char.IsDigit(c))                           // number
            {
                int j = i;
                while (j < n && (char.IsLetterOrDigit(s[j]) || s[j] == '.')) j++;
                yield return (s[i..j], Number); i = j; firstToken = false; continue;
            }
            if (!char.IsWhiteSpace(c)) firstToken = false;
            yield return (c.ToString(), Plain); i++;
        }
    }
}
