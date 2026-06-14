"""Small binary inspection helpers: strings + PE section dump."""
import sys, re, struct

def strings(path, minlen=5, encoding='ascii'):
    with open(path, 'rb') as f:
        data = f.read()
    out = []
    if encoding == 'ascii':
        for m in re.finditer(rb'[\x20-\x7e]{%d,}' % minlen, data):
            out.append((m.start(), m.group().decode('ascii', 'replace')))
    else:  # utf-16le
        for m in re.finditer((rb'(?:[\x20-\x7e]\x00){%d,}' % minlen), data):
            out.append((m.start(), m.group().decode('utf-16le', 'replace')))
    return out

def pe_sections(path):
    with open(path, 'rb') as f:
        data = f.read()
    if data[:2] != b'MZ':
        return None
    e_lfanew = struct.unpack_from('<I', data, 0x3c)[0]
    if data[e_lfanew:e_lfanew+4] != b'PE\x00\x00':
        return None
    coff = e_lfanew + 4
    machine, nsec, _, _, _, optsize, chars = struct.unpack_from('<HHIIIHH', data, coff)
    opt = coff + 20
    magic = struct.unpack_from('<H', data, opt)[0]
    sectbl = opt + optsize
    secs = []
    for i in range(nsec):
        off = sectbl + i*40
        name = data[off:off+8].rstrip(b'\x00').decode('latin1')
        vsize, vaddr, rawsize, rawptr = struct.unpack_from('<IIII', data, off+8)
        characteristics = struct.unpack_from('<I', data, off+36)[0]
        secs.append(dict(name=name, vsize=vsize, vaddr=vaddr, rawsize=rawsize,
                         rawptr=rawptr, chars=characteristics))
    return dict(machine=machine, magic=magic, nsec=nsec, secs=secs, size=len(data))

if __name__ == '__main__':
    cmd = sys.argv[1]
    path = sys.argv[2]
    if cmd == 'sections':
        info = pe_sections(path)
        if not info:
            print("not a PE")
        else:
            print(f"machine={info['machine']:#x} magic={info['magic']:#x} nsec={info['nsec']} filesize={info['size']}")
            for s in info['secs']:
                print(f"  {s['name']:10s} va={s['vaddr']:#010x} vsize={s['vsize']:#x} rawptr={s['rawptr']:#010x} rawsize={s['rawsize']:#x} chars={s['chars']:#010x}")
    elif cmd == 'strings':
        minlen = int(sys.argv[3]) if len(sys.argv) > 3 else 5
        enc = sys.argv[4] if len(sys.argv) > 4 else 'ascii'
        pat = sys.argv[5].lower() if len(sys.argv) > 5 else None
        for off, s in strings(path, minlen, enc):
            if pat is None or pat in s.lower():
                print(f"{off:#010x}  {s}")
