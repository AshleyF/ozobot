# Reverse Engineering the [Ozobot](http://ozobot.com)

This is a pretty cool little line-follower. They publish the ["Static Codes", but not the "Flash Codes"](http://ozobot.com/play/color-code-language) which are used with [OzoBlockly](http://ozoblockly.com/). There is no SDK. So, we must resort to reverse engineering ;) It's a fun toy and even more fun as an enigma to be unravelled.

## FlashForth

FlashForth is a simple programming "IDE" [available here](http://ashleyf.github.io/ozobot). Have fun!

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

#### Bytecodes

| Byte | |
|------|-|
| 0x80 | Test |

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
