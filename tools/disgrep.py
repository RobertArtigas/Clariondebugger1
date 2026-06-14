import sys, struct, re
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
def u32(b,o): return struct.unpack_from('<I',b,o)[0]
def u16(b,o): return struct.unpack_from('<H',b,o)[0]
path=sys.argv[1]; pat=sys.argv[2]
d=open(path,'rb').read()
lfa=u32(d,0x3c); nsec=u16(d,lfa+6); optsz=u16(d,lfa+20); st=lfa+24+optsz
va=rp=vs=0
for i in range(nsec):
    o=st+i*40
    if d[o:o+8].rstrip(b'\0')==b'.text': va=u32(d,o+12); vs=u32(d,o+8); rp=u32(d,o+20)
code=d[rp:rp+vs]
md=Cs(CS_ARCH_X86, CS_MODE_32)
rx=re.compile(pat)
hits=0
for insn in md.disasm(code, va):
    line=f"{insn.address:#08x}: {insn.mnemonic} {insn.op_str}"
    if rx.search(line):
        print("  "+line); hits+=1
        if hits>60: break
