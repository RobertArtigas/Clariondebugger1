"""Dump the type records referenced by BrowseStudents locals (known source types)."""
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

# known: name -> source type
known = {"CURRENTTAB":"STRING(80)","LOCALREQUEST":"LONG","ORIGINALREQUEST":"LONG",
         "LOCALRESPONSE":"LONG","FILESOPENED":"BYTE","WINDOWOPENED":"LONG",
         "FORCEREFRESH":"LONG","JUNK":"STRING(20)","MYLOCALVAR1":"STRING(20)","MYLOCALVAR2":"STRING(20)"}
scope=0x91a0d
p=SB
while p+17<AM:
    if blob[p]==0x04:
        nameOff=u32(blob,p+5); off=i32(blob,p+9); sc=u32(blob,p+13); tr=u32(blob,p+1)
        if sc==scope and 0<nameOff<nameSz:
            nm=cstr(nameOff)
            if nm in known:
                t=SB+tr
                raw=' '.join(f'{x:02x}' for x in blob[t:t+22])
                print(f"{nm:14} ({known[nm]:10}) typeRef={tr:#x}  tag@0={blob[t]:#04x} tag@4={blob[t+4]:#04x}")
                print(f"     bytes@typeRef: {raw}")
            p+=17; continue
    p+=1
