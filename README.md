# ProllE2
Simple 8-bit architecture emulator

An emulator written in C# for an unnamed 8-bit architecture I came up with a couple of years ago. It features an interpreter and a recompiler emitting CIL code. As of yet there is no public assembler available but I plan to release one at some point.
## Architecture
The architecture features 256 bytes of memory, 8 general purpose 8-bit registers and 16 instructions. An instructions consists of the following:
- 4 bits of opcode
- 2 bits of destination addressing mode
- 8 bits of destination address/value
- 2 bits of source addressing mode
- 8 bits of source address/value

This works out to 24 bits aka 3 bytes for each instruction. For compatibility with the recompiler you'll want your instructions to be 3-byte aligned.

There are 4 addressing modes:
- `0x0 - register`
- `0x1 - memory`
- `0x2 - immediate`
- `0x3 - memory at register value`

And 16 opcodes:
- `0x0 - nop`
- `0x1 - add`
- `0x2 - sub`
- `0x3 - mul`
- `0x4 - div`
- `0x5 - not`
- `0x6 - or`
- `0x7 - and`
- `0x8 - xor`
- `0x9 - je`
- `0xa - jne`
- `0xb - jg`
- `0xc - jl`
- `0xd - jmp`
- `0xe - mov`
- `0xf - cmp`

There are 8 registers (`0x0` - `0x7`), register `0x0` is the program counter in interpreter mode. In recompiler mode however this register is not used by the system.
## Memory
The last 2 addresses in memory are reserved for printing to the console, this is the only output implemented right now. The last address (`0xff`) holds the integer to print and it is printed when you write a non-zero value to the second last address (`0xfe`). The value at `0xfe` is reset to 0 after every write to it while the value at `0xff` is not. This area of memory is non-executable which means that in a 3-byte aligned program, `0xf9` is the last executable instruction. Branching to a non-executable area exits the program noramlly.
## Examples
##### Single instruction example:
As an example, lets look at the simple instruction `mov r1, 1`. This instruction moves the value 1 into register 1. This instruction can be assembled to the following hexadecimal value: `0xE00601`. If we convert this to binary and divide it up: `1110 00 00000001 10 00000001`. Lets go through these values:
- `1110` - opcode: `0xe`, the mov opcode.
- `00` - destination addressing mode: `0x0`, register addressing mode.
- `00000001` - destination value: `0x1`, register 1.
- `10` - source addressing mode: `0x2`, immediate value
- `00000001` - source value: `0x1`, the immediate value 1
##### Program example:
Here's a simple unrolled fibonacci program I wrote:
```x86asm
mov r1 0x1      // first fibonacci value
mov [0xFF] r1
mov [0xFE] 0x1  // print r1
cmp r1 0xe9
je 0xfb         // exit before we overflow the register
add r2 r1
mov [0xFF] r2
mov [0xFE] 0x1  // print r2
cmp r2 0xe9
je 0xfb         // same as last exit
add r1 r2
jmp 0x3         // loop
```
Assembled to a hex program you can run on the emulator:
```
E00601
E7FC01
E7FA01
F006E9
9BEC00
100801
E7FC02
E7FA01
F00AE9
9BEC00
100402
D80C00
```
