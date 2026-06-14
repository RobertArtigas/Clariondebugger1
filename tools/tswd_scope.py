"""Collect all 17-byte local var records grouped by scopeRef (dword at +13)."""
import sys, struct, collections
def u32(b,o): return struct.unpack_from('<I',b,o)[0]
def i32(b,o): return struct.unpack_from('<i',b,o)[0]
def u16(b,o): return struct.unpack_from('<H',b,o)[0]
def find(path):
    d=open(path,'rb').read(); lfa=u32(d,0x3c); nsec=u16(d,lfa+6); optsz=u16(d,lfa+20); st=lfa+24+optsz
    for i in range(nsec):
        o=st+i*40
        if d[o:o+8].rstrip(b'\0')==b'.cwdebug':
            rp=u32(d,o+20); loc=d[rp:rp+32]; return d[u32(loc,24):u32(loc,24)+u32(loc,16)]
blob=find(sys.argv[1])
dirv=[u32(blob,8+4*i) for i in range(12)]
NB=dirv[6]; SB=dirv[8]; AM=dirv[11]; nameSz=SB-NB
def cstr(noff):
    try: e=blob.index(0,NB+noff); return blob[NB+noff:e].decode('latin1')
    except: return '?'

scope=int(sys.argv[2],16) if len(sys.argv)>2 else 0x91a0d
print(f"=== var records with scopeRef={scope:#x} ===")
p=SB; found=[]
while p+17<AM:
    if blob[p]==0x04:
        typeRef=u32(blob,p+1); nameOff=u32(blob,p+5); off=i32(blob,p+9); sc=u32(blob,p+13)
        if sc==scope and 0<nameOff<nameSz and -0x40000<off<0x40000:
            nm=cstr(nameOff)
            if nm and nm[0]!='?':
                found.append((p-SB,nm,off,typeRef))
                p+=17; continue
    p+=1
for so,nm,off,tr in found:
    print(f"   @{so:#06x} {nm:<20} frame={off:<6} typeRef={tr:#x}")
print(f"total: {len(found)}")
