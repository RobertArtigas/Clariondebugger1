"""Locate specific local var records by name and reveal the local-storage layout."""
import sys, struct
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
def nameoff(name):
    t=name.encode(); o=NB
    res=[]
    while o<SB:
        e=blob.index(0,o)
        if blob[o:e]==t: res.append(o-NB)
        o=e+1
    return res

for nm in ["MYLOCALVAR1","MYLOCALVAR2","CURRENTTAB"]:
    offs=nameoff(nm)
    print(f"\n=== {nm}: nameOff(s)={[hex(x) for x in offs]} ===")
    for noff in offs:
        # scan stream for a 0x04 var record (tag at p) whose nameOff(@p+5)==noff
        p=SB
        while p+13<AM:
            if blob[p]==0x04 and u32(blob,p+5)==noff:
                typeRef=u32(blob,p+1); off=i32(blob,p+9)
                # what's right before this record (the previous local)?
                print(f"  rec@stream {p-SB:#x}: typeRef={typeRef:#x} off={off:#x} ({off})")
                print(f"     bytes[-4..+20]: {' '.join(f'{x:02x}' for x in blob[p-4:p+20])}")
            p+=1

# Now dump the contiguous local region around mylocalvar1 to see the record sequence
m1=nameoff("MYLOCALVAR1")
if m1:
    noff=m1[0]; p=SB
    while p+13<AM:
        if blob[p]==0x04 and u32(blob,p+5)==noff: break
        p+=1
    print(f"\n=== contiguous records around mylocalvar1 @ {p-SB:#x} (walking 0x04 records) ===")
    # walk backward/forward decoding 14-byte var records: 04|typeRef|nameOff|off
    start=p-14*6
    q=start
    for _ in range(24):
        if blob[q]==0x04:
            tr=u32(blob,q+1); no=u32(blob,q+5); of=i32(blob,q+9)
            if 0<no<nameSz:
                print(f"   @{q-SB:#06x} 04 name={cstr(no):<16} off={of:#x}({of}) typeRef={tr:#x}")
                q+=14; continue
        q+=1
