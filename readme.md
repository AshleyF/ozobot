# Reverse Engineering the [Ozobot](http://ozobot.com)

This is a pretty cool little line-follower. They publish the ["Static Codes", but not the "Flash Codes"](http://ozobot.com/play/color-code-language) which are used with [OzoBlockly](http://ozoblockly.com/). There is no SDK. So, we must resort to reverse engineering ;) It's a fun toy and even more fun as an enigma to be unravelled.

## FlashForth

FlashForth is a simple programming "IDE" [available here](http://ashleyf.github.io/ozobot). Have fun!

Many "Words" in FlashForth correspond directly with Ozobot instructions (see Bytecodes section below). Some words though are macros. These are mainly to construct control structure without having to think about addresses and such.

For example the FlashForth construct `while` ... `do` ... `loop` translates into conditional and unconditional branches to relative addresses.

    while COLOR sensor RED = do 127 -127 wheels loop

Becomes literally:

| Addr |    |            |
|------|----|------------|
| 0000 | 0e | COLOR      |
| 0001 | 92 | sensor     |
| 0002 | 01 | RED        |
| 0003 | a4 | =          |
| 0004 | 80 | if         |
| 0005 | 0a | +10        |
| 0006 | 97 | unknown    |
| 0007 | 7f | 127        |
| 0008 | 7e | 126        |
| 0009 | 8b | not (-127) |
| 0010 | 9f | led        |
| 0011 | ba | jump       |
| 0012 | f5 | -11        |
| 0013 | 97 | unknown    |

Macros execute at compile-time. `while` merely pushes the current address (`0000`) to a compile-time stack. `do` emits an `if` bytecode, pushes the current address (`0005`), and emits a placeholder address along with the (still mysterious) `97` bytecode. Finally, `loop` does all the magic. Now that the extent of the loop body is known, it pops the addresses and emits a `jump` back (`-11` in this case) to the address of the point at which `while` was seen; causing reevaluation of the predicate expression (`COLOR sensor RED =`). It also patches the placeholder address for the `if` to skip over the body (`+10` in this case). It may sound like a funny name to call conditional branch on false, `if`. This comes from Forth and is because of the forms for which this instruction is used. It's essentially a branch on false over the body - meaning `if` true, fall through into the body.

## Reading Flash Codes

In [OzoBlockly](http://ozoblockly.com/), programs are transmitted to the robot through the color sensor by flashing colors on the screen.

* Colors: White, R, G, B, C, M, Y, K (black)
* Framerate: 20Hz (50ms apart)
* No repeating colors (robot detects *change* in color rather than timing)

Scrubbing through a high frame rate video, it's clear that the colors being used are primary White, Red, Green, Blue, Cyan, Magenta, Yellow and Black. Presumably it uses an RGB sensor and these are all composed of full on/off RGB channels - very easy to detect. Further analyzing the video (and logging a capture with [`FlashReader`](FlashReader)) it is clearly a 20Hz frame rate. A further observation is that colors never repeat. That is, a *different* color is shown every 50ms.

### Encoding Values

By flashing the same program with parameters in increasing values we can glean the numbering scheme. It appears to be a base-7 encoding (due to no repeats within set of 8 colors) and appears to line up on byte-sized boundaries encoded as sets of three colors. In BGR space, the colors are just 3-bit values:

* 0 000 Black
* 1 001 Red
* 2 010 Green
* 3 011 Yellow (R+G)
* 4 100 Blue
* 5 101 Magenta (R+B)
* 6 110 Cyan (G+B)

White (111) isn't used as a value. Instead it signifies "repeat last color". For example KWK is just 000.

### Framing

Programs are "framed" by:

* `CRY CYM CRW ... CMW`

The first three and the last "word" are CRY CYM CRW followed by a sequence of encoded bytes and finally CMW. These decode to values outside of a single byte range (hex 130, 140, 12E and 14E). Everything between however seems to always decode to bytes. We will consider this a "framing" protocol. Just a sequence the robot listens for to switch into "programming" mode.

### Envelope

The bytes within frames appear to be in the form:

* `VV UU XX YY ZZ ... CK`

Giving a version, length and checksum. Bytes within this "envelope" are program instructions.

#### Version?

`VV` and `UU` may be a version number? They have been observed to always be 1 and 3 currently.

#### Length

`XX`, `YY` and `ZZ` have to do with the length of the program.

Not fully understood yet, but `ZZ` (perhaps combined with `YY`) appears to be the length of the program instructions (up to the checksum). `XX` is, for some reason that's still a mystery, always 219-length. `YY` has only been observed to be zero, but likely it's the high bits of `ZZ` when programs longer than 255 (`FF`) are sent.

#### Checksum

`CK` is a checksum to detect misreading. It is constructed from the whole payload up to the checksum - version, length and program bytes.

After a bit of experimentation, the checksum has been found to be simply a single-byte (underflowed) running difference between the bytes of the envelope, starting with zero. That is, subtract the first byte from zero, subtract the second byte from this, the third from that, and so on; keeping a running value. The result is a byte. For example, this payload:

    01 03 CE 00 0D C7 2D 24 93 00 00 00 B8 00 1E 93 00 AE

Checksums to `5F` (95).

### Instructions

It appears to be a stack machine with operands sent before operations. For example, the instruction to "set LED color" is `B8` and takes three arguments for red, green and blue values. `7F 00 00 B8` sets the LED to red. The "wait N x 10ms" instruction is `9B` and takes a single argument (the number of centiseconds). `64 9B` waits for one second (`64` hex = 100 dec). These can be composed:

    7F 00 00 B8   64 9B   00 7F 00 B8   64 9B   00 00 7F B8   64 9B

This program fragment blinks red, then green, then blue, with one-second pauses.

#### Literals

Values less than 128 are considered literals and pushed to the stack. Values of 128 or higher are instructions. You may notice that in OzoBlockly negative values are supported. This is done by emitting a *positive* value (one less than desired) followed by a `not` (`8b` hex) instruction. This gives a range of -128 to +127. FlashForth converts negative literals for you.

## OzoBlockly Decoded

Going through the various constructs in OzoBlockly, here is the bytecode to which they compile.

### Movement

`Move distance D mm speed S mm/s` is a single bytecode (`9e`) of two parameters `D S move`.

`Rotate angle D deg speed S mm/s` is also a single bytecode (`98`) of two parameters `D S turn`.

`Set wheel speeds: left (mm/s) L right (mm/s) R` as well is a single bytecode (`9f`) of two parameters `L R wheels`.

`Stop motors` is really just `0 0 wheels`.

`Move forward at speed S mm/s until line is found, and then follow the line` is not primitive at all. It actually becomes a whole little program fragment.

    S dup dup wheels ac 08 sensor if -8 97 96 00 00 wheels c6 01 a0 ac ad 9a 10 = if -3 97 00 a0 01 25 93 // TODO: Figure this out!

`get surface color` and `surface color C` as used with a comparison operator (`=` for example) is `COLOR sensor C =`. FlashForth has constants for the single-byte values used for `C` (see the help tab [there](http://ashleyf.github.io/ozobot)).

### Line Navigation

None of these are primitive.

`Follow line to next intersection or line end`:

    call 00 09 00 end 01 a0 ac ad 9a 10 = if fd 00 a0 01 25 93 ; // TODO: Figure this out!

`Pick direction D`:

    D call 00 0b 96 00 end dup 10 92 81 not b7 25 92 not b7 1f 93 01 a0 ad 9a 14 = if fd 00 a0 00 25 93 ; // TODO: Figure this out!

Where `D` is `STRAIGHT` (1), `LFET` (2), `RIGHT` (4), or `BACK` (8).

`This is way D`:

    LINE sensor D 81 // TODO: Figure out what 81 is

Where `D` is `STRAIGHT` (1), `LFET` (2), `RIGHT` (4), or `END` (8).

`Set line-following speed S mm/s`:

    S 18 93 // TODO: Figure this out!

`Get line-following speed`:

    18 sensor

`Get intersection/line-end color`:

    0f sensor

`Intersection/line-end color C` is the same as `Surface color C` above. The same FlashForth single-byte values may be used.

### Light Effects

`Set LED color Red R Green G Blue B` is a single bytecode taking three parameters: `R G B led`

`Turn LED off` is just `0 0 0 led` followed by `1e 93`. // TODO: Figure out what `1e 93` means!

`Set random light color` isn't primative: `7f 00 rand 7f 00 rand 7f 00 rand led`

### Timing

`Wait T x 10 ms` is `T wait`.

`Wait S.T second(s)` is not primitive. If only `T` centiseconds are chosen then it's just `T wait`. If `S` seconds are chosen (even though 1 second could be done as a simple `wait`), it becomes a loop:

    S 100 wait 1 - dup 0 > not if -8 97 96 // TODO: Figure out what 96 is for

If both `S` and `T` are chosen, then it becomes a loop followed by another `wait`:

    S 100 wait 1 - dup 00 > not if -8 97 96 T wait // TODO: Figure out what 96 is for

### Terminate

`Terminate program and turn Ozobot off` is `00 end` (`00` = `OFF` in FlashForth)

`Terminate program and continue line following` is `01 end` (`01` = `FOLLOW` in FlashForth)

`Terminate program and switch to idle` is `02 end` (`02` = `IDLE` in FLashForth)

In `IDLE` mode, by the way, it is ready to accept a new program. If we can find a way to cause programs to execute without double pressing power then we could have a very nice, interactive experience with the Ozobot sitting on a tablet while being programmed from another tablet or laptop. Send a command and immediately see the result. Send another. And so on without load/run manual steps.

### Logic

`If P do A ...`. For example `If TRUE do Set light color RED then Set light color GREEN`:

    TRUE if +10 97 255 0 0 led jump +3 97 0 255 0 led

The `if` instruction consumes a predicate result (boolean) and branches on false over the body of the block (`+10` in this case). It's not clear yet what the `97` bytecode does, but apparently the body `jump`s over this to the following code (`0 255 0 led`).

The form `If P do A else B ...` (for example `If TRUE do Set light color RED else Set light color BLUE then Set light color GREEN` becomes:

    TRUE if +10 97 255 0 0 led jump +7 97 0 0 255 led 0 255 0 led

Again, `if` jumps over the main body (into the `else` clause in this case). However, this time the `jump` in the main body skips over the `else` clause. Maybe that's the purpose - the OzoBlockly compiler blindly puts in this jump even without an `else`. Meaningless, but harmless in that case.

The form `If P do A else if Q do B ...` is no different; just nested:

    TRUE if +10 97 255 0 0 led jump +14 97 TRUE if +10 97 0 0 255 led jump +3 97 0 255 0 led

The FlashForth form for this is normal Forth-like `P if A then ...` or `P if A else B then ...` or `P if A else Q if B then then ...`. TODO: Document these macros

The OzoBlockly form `test P if true T if false F` which results in a value (as opposed to `If P do A else B`) becomes:

    P if +7 97 T jump +4 97 F // TODO: Figure out what 97 is for

If FlashForth, there is no distinction between expresions and statements. This is just `P if T else F then` as usual.

#### Boolean & Comparison

The primitive boolean operations are `and` (`a2`), `or` (`a3`) and `not` (`8a`). These appear to be separate from the bitwise operations. A normal Forth would have avoided this by useing -1 (all bits set) for `TRUE`. Ozobot appears to use non-zero, like C. Poor design?

The primitive comparison operations are `=` (`a4`), `>=` (`9c`) and `>` (`9d`). The others are composed of these and `not` (`8a`). Not equal is `= not`, less-than is `>= not` and less-or-equal is `> not`. There are `<>`, `<` and `<=` macros that expand to these in FlashForth.

## Bytecodes

| Byte |        |                                       |
|------|--------|---------------------------------------|
| 0x80 | if     |                                       |
| 0x81 |        |                                       |
| 0x82 |        |                                       |
| 0x83 | ~      |                                       |
| 0x84 |        |                                       |
| 0x85 | +      |                                       |
| 0x86 | -      |                                       |
| 0x87 | *      |                                       |
| 0x88 | /      |                                       |
| 0x89 | mod    |                                       |
| 0x8a | not    |                                       |
| 0x8b | neg    |                                       |
| 0x8c | rand   |                                       |
| 0x8d |        |                                       |
| 0x8e |        |                                       |
| 0x8f |        |                                       |
| 0x90 | call   |                                       |
| 0x91 | ;      | Return                                |
| 0x92 | sensor |                                       |
| 0x93 | ?      | Set variable?                         |
| 0x94 | dup    |                                       |
| 0x95 |        |                                       |
| 0x96 | ?      |                                       |
| 0x97 | ?      | Unknown purpose. Used in while loops. |
| 0x98 | turn   |                                       |
| 0x99 |        |                                       |
| 0x9a | ?      |                                       |
| 0x9b | wait   |                                       |
| 0x9c | >=     |                                       |
| 0x9d | >      |                                       |
| 0x9e | move   |                                       |
| 0x9f | wheels |                                       |
| 0xa0 | ?      |                                       |
| 0xa1 |        |                                       |
| 0xa2 | and    |                                       |
| 0xa3 | or     |                                       |
| 0xa4 | =      |                                       |
| 0xa5 |        |                                       |
| 0xa6 | poke   |                                       |
| 0xa7 | peek   |                                       |
| 0xa8 | abs    |                                       |
| 0xa9 |        |                                       |
| 0xaa |        |                                       |
| 0xab |        |                                       |
| 0xac | ?      |                                       |
| 0xad | ?      |                                       |
| 0xae | end    |                                       |
| 0xaf |        |                                       |
| 0xb0 |        |                                       |
| 0xb1 |        |                                       |
| 0xb2 |        |                                       |
| 0xb3 |        |                                       |
| 0xb4 |        |                                       |
| 0xb5 |        |                                       |
| 0xb6 |        |                                       |
| 0xb7 |        |                                       |
| 0xb8 | led    |                                       |
| 0xb9 |        |                                       |
| 0xba | jump   |                                       |
| 0xbb |        |                                       |
| 0xbc |        |                                       |
| 0xbd |        |                                       |
| 0xbe |        |                                       |
| 0xbf |        |                                       |
| 0xc0 |        |                                       |
| 0xc1 |        |                                       |
| 0xc2 |        |                                       |
| 0xc3 |        |                                       |
| 0xc4 |        |                                       |
| 0xc5 |        |                                       |
| 0xc6 | ?      |                                       |
| 0xc7 |        |                                       |
| 0xc8 |        |                                       |
| 0xc9 |        |                                       |
| 0xca |        |                                       |
| 0xcb |        |                                       |
| 0xcc |        |                                       |
| 0xcd |        |                                       |
| 0xce |        |                                       |
| 0xcf |        |                                       |
| 0xd0 |        |                                       |
| 0xd1 |        |                                       |
| 0xd2 |        |                                       |
| 0xd3 |        |                                       |
| 0xd4 |        |                                       |
| 0xd5 |        |                                       |
| 0xd6 |        |                                       |
| 0xd7 |        |                                       |
| 0xd8 |        |                                       |
| 0xd9 |        |                                       |
| 0xda |        |                                       |
| 0xdb |        |                                       |
| 0xdc |        |                                       |
| 0xdd |        |                                       |
| 0xde |        |                                       |
| 0xdf |        |                                       |
| 0xe0 |        |                                       |
| 0xe1 |        |                                       |
| 0xe2 |        |                                       |
| 0xe3 |        |                                       |
| 0xe4 |        |                                       |
| 0xe5 |        |                                       |
| 0xe6 |        |                                       |
| 0xe7 |        |                                       |
| 0xe8 |        |                                       |
| 0xe9 |        |                                       |
| 0xea |        |                                       |
| 0xeb |        |                                       |
| 0xec |        |                                       |
| 0xed |        |                                       |
| 0xee |        |                                       |
| 0xef |        |                                       |
| 0xf0 |        |                                       |
| 0xf1 |        |                                       |
| 0xf2 |        |                                       |
| 0xf3 |        |                                       |
| 0xf4 |        |                                       |
| 0xf5 |        |                                       |
| 0xf6 |        |                                       |
| 0xf7 |        |                                       |
| 0xf8 |        |                                       |
| 0xf9 |        |                                       |
| 0xfa |        |                                       |
| 0xfb |        |                                       |
| 0xfc |        |                                       |
| 0xfd |        |                                       |
| 0xfe |        |                                       |
| 0xff |        |                                       |

### Full Example

This Forth-like code to blink red, green, blue with one-second delays:

    some_stuff // ignore
    x7F 0 0 led
    100 wait
    0 x7F 0 led
    100 wait
    0 0 x7F led
    100 wait
    off
    
Compiles to:

    2D 24 93 // ignore
    7F 00 00 B8
    64 9B
    00 7F 00 B8
    64 9B
    00 00 7F B8
    64 9B
    00 AE
    
The version (`01 03`) is prepended along with the length bytes (`C4 00 17`), becomming:

    01 03   C4 00 17   2D 24 93 7F 00 00 B8 64 9B 00 7F 00 B8 64 9B 00 00 7F B8 64 9B 00 AE
    
The checksum of this is `ED`:

    01 03 C4 00 17 2D 24 93 7F 00 00 B8 64 9B 00 7F 00 B8 64 9B 00 00 7F B8 64 9B 00 AE   ED

This is framed within `130 140 12E ... 14E`:

    130 140 12E   01 03 C4 00 17 2D 24 93 7F 00 00 B8 64 9B 00 7F 00 B8 64 9B 00 00 7F B8 64 9B 00 AE ED    14E
    
And encodes directly to color values:

    CRYCYMCRRKKRKKYBKKKKKKYGKCYKMRYKKGBRKKKKKKYMGGKGYRRKKKGBRKKKYMGGKGYRRKKKKKKGBRYMGGKGYRRKKKYYCBMCCMM
    
But the robots expects *different* colors for each frame (no repeats), so replacing repeats with white:

    CRYCYMCRWKWRKWYBKWKWKWYGKCYKMRYKWGBRKWKWKWYMGWKGYRWKWKGBRKWKYMGWKGYRWKWKWKWGBRYMGWKGYRWKWKYWCBMCWMW

This can be sent to the robot with [`FlashWriter`](FlashWriter) and viola!

Have fun!
