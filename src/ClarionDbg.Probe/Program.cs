using ClarionDbg.Engine;

string exe = args.Length > 0 ? args[0] : @"C:\ai\debuger\sample\dbgtest\dbgtest_dbg.exe";
int bpLine = args.Length > 1 ? int.Parse(args[1]) : 21;

var pe = new PeImage(exe);
var info = TswdInfo.Load(pe) ?? throw new Exception("not a debug build");

Console.WriteLine($"source   : {info.SourceFile}");
Console.WriteLine($"globals  : {string.Join(", ", info.Globals.OrderBy(g => g.Rva).Select(g => $"{g.Name}@0x{g.Rva:X}"))}");
Console.WriteLine($"procs    : {string.Join(", ", info.Procedures.OrderBy(p => p.Rva).Select(p => $"{p.Name}@0x{p.Rva:X}"))}");
Console.WriteLine($"break    : line {bpLine} -> rva 0x{info.LineToRva(bpLine):X}");
Console.WriteLine(new string('-', 60));

var done = new ManualResetEventSlim();
var sess = new DebugSession(exe, pe, info);
sess.Log += s => Console.WriteLine("[engine] " + s);
sess.Exited += c => { Console.WriteLine($"[exit] code {c}"); done.Set(); };
sess.Stopped += info2 =>
{
    Console.WriteLine($"\n*** STOPPED: {info2.Reason} at EIP 0x{info2.Eip:X8} (line {info2.Line}) ***");
    Console.WriteLine("call stack:");
    foreach (var f in info2.Stack) Console.WriteLine($"   {f.Proc,-12} 0x{f.Addr:X8} line {f.Line}");
    Console.WriteLine("globals (live values read from the debuggee):");
    foreach (var v in info2.Globals)
    {
        string ascii = new string(v.Raw.Select(b => b >= 32 && b < 127 ? (char)b : '.').ToArray());
        Console.WriteLine($"   {v.Name,-10} @0x{v.Addr:X8}  long={(int)v.AsLong,-6} ascii='{ascii}'  {v.Hex}");
    }
    Console.WriteLine("\n(continuing…)");
    sess.Continue();
};

sess.Start(new[] { bpLine });
done.Wait(8000);
Console.WriteLine("probe finished.");
