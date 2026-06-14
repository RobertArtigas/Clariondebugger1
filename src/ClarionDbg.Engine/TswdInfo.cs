using System.Buffers.Binary;

namespace ClarionDbg.Engine;

/// <summary>
/// Parses the Clarion/TopSpeed 'TSWD' debug blob.
///
/// Blob layout (relative to blob start), discovered by reverse engineering Clarion 12:
///   +0   'TSWD'
///   +4   u32 version
///   +8   directory of u32 fields:
///          [1]=src-file name off, [3]=line-table off, [4]=line count,
///          [6]=name-string-table off, [8]=symbol-record-stream off,
///          [11]=address-map off, [10]=address-map count
///   line table : { u16 line; u32 rva } * count
///   addr map   : { u32 rva; u32 symRef } * count  (symRef is relative to symbol-record stream)
///   each symbol record contains, among its type bytes, the pair { u32 nameOff; u32 rva }.
/// </summary>
public sealed class TswdInfo
{
    public string SourceFile { get; private set; } = "";
    public List<(int Line, uint Rva)> Lines { get; } = new();
    public List<Symbol> Globals { get; } = new();
    public List<Symbol> Procedures { get; } = new();

    public record Symbol(string Name, uint Rva);

    static uint U32(byte[] b, int o) => BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o));
    static ushort U16(byte[] b, int o) => BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(o));

    public static TswdInfo? Load(PeImage pe)
    {
        var blob = pe.ReadCwDebugBlob();
        if (blob == null) return null;
        if (!blob.AsSpan(0, 4).SequenceEqual("TSWD"u8)) return null;

        var info = new TswdInfo();
        int[] dir = new int[12];
        for (int i = 0; i < 12; i++) dir[i] = (int)U32(blob, 8 + 4 * i);

        // --- source file name ---
        int s = dir[1];
        int e = Array.IndexOf(blob, (byte)0, s);
        info.SourceFile = System.Text.Encoding.Latin1.GetString(blob, s, e - s);

        // --- line table ---
        int ltOff = dir[3], ltCnt = dir[4], o = ltOff;
        for (int i = 0; i < ltCnt; i++)
        {
            info.Lines.Add((U16(blob, o), U32(blob, o + 2)));
            o += 6;
        }

        // --- name string table base ---
        int nameBase = dir[6];

        // --- address map -> symbols; resolve name via symbol record ---
        int symStream = dir[8];
        int amOff = dir[11], amCnt = dir[10];
        for (int i = 0; i < amCnt; i++)
        {
            uint rva = U32(blob, amOff + i * 8);
            int symRef = (int)U32(blob, amOff + i * 8 + 4);
            string name = ResolveName(blob, symStream + symRef, rva, nameBase);
            var sym = new Symbol(name, rva);
            if (pe.IsCodeRva(rva)) info.Procedures.Add(sym);
            else info.Globals.Add(sym);
        }
        return info;
    }

    /// <summary>
    /// Within a symbol record we look for the dword equal to the symbol's RVA; the dword
    /// immediately preceding it is the offset into the name string table.
    /// </summary>
    static string ResolveName(byte[] blob, int recStart, uint rva, int nameBase)
    {
        for (int o = recStart; o + 8 <= blob.Length && o < recStart + 64; o += 1)
        {
            if (U32(blob, o + 4) == rva)
            {
                int nameOff = (int)U32(blob, o);
                if (nameBase + nameOff < blob.Length)
                {
                    int s = nameBase + nameOff;
                    int e = Array.IndexOf(blob, (byte)0, s);
                    if (e > s) return System.Text.Encoding.Latin1.GetString(blob, s, e - s);
                }
            }
        }
        return $"sym_{rva:X}";
    }

    public int? RvaToLine(uint rva)
    {
        int? best = null; uint bestRva = 0;
        foreach (var (line, r) in Lines)
            if (r <= rva && r >= bestRva) { bestRva = r; best = line; }
        return best;
    }

    public uint? LineToRva(int line)
    {
        foreach (var (l, r) in Lines) if (l == line) return r;
        // nearest line at or after requested
        uint? best = null; int bestLine = int.MaxValue;
        foreach (var (l, r) in Lines) if (l >= line && l < bestLine) { bestLine = l; best = r; }
        return best;
    }
}
