# Clarion Debugger — Reverse-Engineering Findings

Target install: `C:\Clarion1213999` (Clarion 12.0.14000). Build toolchain confirmed
working from the command line; debug-info format fully located and largely decoded.

## 1. The existing toolchain

| Piece | What it is |
|-------|-----------|
| `bin\Clarion.exe`, `bin\ClarionCL.exe` | .NET IDE / command-line driver (thin) |
| `bin\Cladb.exe` | **The existing debugger** — a tiny .NET launcher (`SoftVelocity.Debugger.RunDebugger`) that calls native export `D32$StartDebugger`. The real engine is native. |
| `bin\Cla*.dll` | Native compiler front-ends: `Claclw`=Clarion, `Clacpp`=C/C++, `Clamod`=Modula-2, `Claasm`=assembler — the classic **TopSpeed** multi-language suite sharing one back-end + debug format |
| `bin\ClaRUN.dll` + `bin\debug\ClaRUN.dll` | Release vs **debug-instrumented** runtime |
| `bin\SoftVelocity.Build.Clarion.targets` | MSBuild target; the `CW` task (in `SoftVelocity.CW.Build.Tasks.dll`) invokes the compiler in-process |

### How to build from the command line (verified)
```
MSBuild.exe project.cwproj /p:Configuration=Debug ^
  /p:ClarionBinPath=C:\Clarion12\bin ^
  "/p:clarion_version=Clarion 12.0.13941" ^
  "/p:ConfigDir=%APPDATA%\SoftVelocity\Clarion\12.0"
```
Notes: source files **must be CRLF**; `clarion_version` must match a name registered in
`%APPDATA%\SoftVelocity\Clarion\12.0\ClarionProperties.xml` (`Clarion.Versions`).

Debug is controlled by the project property **`vid`** (`full` | `off`) plus
`line_numbers` and `GenerateMap`.

## 2. How debug information is stored

A debug build adds an extra PE section **`.cwdebug`** and appends a **TSWD** blob as an
overlay after the last section. (Release builds have neither — confirmed by diffing.)

### `.cwdebug` section = locator (32 bytes)
```
+12  'TSWD'         signature
+16  dword  size    size of the debug blob   (0x378 in sample)
+24  dword  offset  file offset of the blob  (0x1000 in sample)
```

### TSWD blob (the actual debug info)
```
+0   'TSWD'
+4   dword version (=1)
+8   directory of dword stream-offsets (relative to blob start):
       [0]=0x38  module record
       [1]=0x3c  source-file name  ("dbgtest.clw\0")
       [3]=0x50  line table offset
       [4]=15    line table entry count
       [6]=0x124 name string table
       [8]=0x1a0 symbol/type record stream
       [11]=0x340 address->symbol map   [10]=7 entries
```

**Line table** — array of `{ u16 line; u32 rva }`, decoded and verified identical to the
`.MAP` file:
```
line 16 -> 0x0040104d   line 24 -> 0x004010dc   ...
```

**Name string table** — null-terminated symbol names:
`_main`, `COMPUTE@Fl` (mangled: F=function, l=long arg), `PCOUNT`, `LOCIDX`,
`LOCSUM`, `PERSON`, `AGE`, `PERSONNAME`, `GBLPRICE`, `GBLNAME`, `GBLCOUNT` ...

**Symbol/type records** (`0x1a0`) + **address→symbol map** (`0x340`, 7 entries) tie each
code/data address to its name + type. Decoded by hand:
```
0x401030 _main        0x4010dc COMPUTE      0x402080 GBLNAME (STRING)
0x4020a0 PERSON(GROUP) 0x4020b8 GBLCOUNT(LONG) 0x4020bc GBLPRICE(DECIMAL)
```
Full type-byte decoding is the main remaining reverse-engineering task; everything
needed for source/line stepping and naming is already decoded.

### Symbol & runtime conventions learned from the `.MAP`
- Globals are exported as `$NAME`; procedures are name-mangled `NAME@F<args>`.
- Custom segments: `*_TEXT`, `*_CONST`, `*_BSS`, `__T_L_S__DATA` (the `.cwtls` section is
  the fingerprint of any Clarion/TopSpeed-compiled binary).
- Runtime entry: imports `ClaRUN.dll:__sysinit/__sysstart`, program start `_main`,
  `Cla$HALT`, decimal ops `Cla$DPushConstant/DPopDec`, string ops `Cla$storestr`.

## 3. Implication for a modern debugger

We do **not** need the proprietary `D32` engine. With (a) the decoded TSWD symbol/line
data and (b) the standard **Win32 Debugging API**, we can build a clean modern debugger:

- Launch debuggee: `CreateProcess(..., DEBUG_ONLY_THIS_PROCESS)`, pump `WaitForDebugEvent`.
- Breakpoints: write `0xCC`, restore + single-step to re-arm.
- Registers/stack: `GetThreadContext` / `SetThreadContext`.
- Memory & variable read/write: `ReadProcessMemory` / `WriteProcessMemory` at the RVA
  (+ runtime image base) from the symbol map — powers Watch / Globals / "edit value".
- Source↔address mapping: the TSWD line table.
- Disassembly window: any x86 disassembler over `.text`.

This reproduces every window the old debugger had (Procedures, Globals, Stack Trace,
Source, Disassembly, Memory, Threads) on a modern, non-crashing foundation.

## 4. Artifacts produced
- `tools/binutils.py` — PE section + strings dump
- `tools/hexdump.py`  — raw region dump
- `tools/tswd.py`     — **working TSWD parser** (source file, line table, names)
- `sample/dbgtest/`   — minimal Clarion program built in Debug + Release for study
