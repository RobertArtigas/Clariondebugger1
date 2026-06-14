"""Parser for Clarion/TopSpeed TSWD debug info embedded in debug-mode EXE/DLLs.

Layout discovered by reverse engineering Clarion 12 output:
  * A '.cwdebug' PE section acts as a LOCATOR:
        bytes 12..15 : 'TSWD' signature
        dword @16    : size of the debug blob
        dword @24    : file offset (== RVA-ish raw offset) of the blob
  * The blob itself starts with 'TSWD', a version dword, then a directory of
    dword offsets (relative to blob start) to the various streams:
        source-file name, line-number tables, name string table, symbol table,
        and an address->symbol map.
"""
import sys, struct

def u32(b, o): return struct.unpack_from('<I', b, o)[0]
def u16(b, o): return struct.unpack_from('<H', b, o)[0]

def find_cwdebug_blob(path):
    data = open(path, 'rb').read()
    # locate the PE section table to find .cwdebug
    e_lfanew = u32(data, 0x3c)
    nsec = u16(data, e_lfanew + 6)
    optsize = u16(data, e_lfanew + 20)
    sectbl = e_lfanew + 24 + optsize
    loc = None
    for i in range(nsec):
        off = sectbl + i*40
        name = data[off:off+8].rstrip(b'\x00')
        if name == b'.cwdebug':
            rawptr = u32(data, off+20)
            loc = data[rawptr:rawptr+32]
            break
    if loc is None:
        return None
    assert loc[12:16] == b'TSWD', "no TSWD signature in .cwdebug locator"
    blob_size = u32(loc, 16)
    blob_off = u32(loc, 24)
    blob = data[blob_off:blob_off+blob_size]
    return blob

def parse(path):
    blob = find_cwdebug_blob(path)
    if blob is None:
        print("no .cwdebug section -> not a debug build"); return
    assert blob[:4] == b'TSWD'
    version = u32(blob, 4)
    print(f"TSWD blob: {len(blob)} bytes, version={version}")
    # directory of dword offsets after sig+version
    dirvals = [u32(blob, 8 + 4*i) for i in range(11)]
    print("directory dwords:", [hex(v) for v in dirvals])

    # --- source file name (first stream that looks like a filename) ---
    # heuristic: the filename appears right after the directory
    src = blob[dirvals[1]:].split(b'\x00', 1)[0].decode('latin1', 'replace')
    print(f"\nsource file: {src}")

    # --- line table: dirvals[3] = offset, dirvals[4] = count ---
    lt_off, lt_cnt = dirvals[3], dirvals[4]
    print(f"\nline table @{lt_off:#x}, {lt_cnt} entries (line -> RVA):")
    o = lt_off
    lines = []
    for i in range(lt_cnt):
        line = u16(blob, o); addr = u32(blob, o+2); o += 6
        lines.append((line, addr))
    for line, addr in lines:
        print(f"   line {line:3d} -> {0x400000+addr:#010x}")

    # --- name string table: dirvals[6] = offset ---
    nt_off = dirvals[6]
    # read until we hit the symbol table (dirvals[8])
    nt_end = dirvals[8]
    names = blob[nt_off:nt_end].split(b'\x00')
    names = [n.decode('latin1','replace') for n in names if n]
    print(f"\nname string table @{nt_off:#x}:")
    print("  ", names)

    # --- address -> symbol map at the tail: dirvals[-1] ---
    am_off = dirvals[10]
    print(f"\naddress->symbol map @{am_off:#x} (RVA, ref):")
    o = am_off
    while o + 8 <= len(blob):
        addr = u32(blob, o); ref = u32(blob, o+4); o += 8
        if addr == 0: break
        print(f"   {0x400000+addr:#010x} -> ref {ref:#x}")

if __name__ == '__main__':
    parse(sys.argv[1])
