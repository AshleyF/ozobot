# Reverse Engineering the [Ozobot](http://ozobot.com)

This is a pretty cool little line-follower. They publish the ["Static Codes", but not the "Flash Codes"](http://ozobot.com/play/color-code-language) which are used with [OzoBlockly](http://ozoblockly.com/). There is no SDK. So, I must resort to reverse engineering. :)

## Reading Flash Codes

In [OzoBlockly](http://ozoblockly.com/), programs are transmitted to the robot through the color sensor by flashing colors on the screen.

* Colors: White, R, G, B, C, M, Y, K (black)
* Framerate: 20Hz (50ms apart)
* No repeating colors (robot detects *change* in color rather than timing)

Scrubbing through a high frame rate video, it's clear that the colors being used are primary White, Red, Green, Blue, Cyan, Magenta, Yellow and Black. Presumably it uses an RGB sensor and these are all composed of full on/off RGB channels - very easy to detect. Further analyzing the video (and logging a capture with [`FlashReader`](FlashReader)) it is clearly a 20Hz framerate. A further observation is that colors never repeat. That is, a *different* color is shown every 50ms.

### Encoding Values

By flashing the same program with parameters in increasing values we can glean the numbering scheme. It appears to be a base-7 encoding (due to no repeats within set of 8 colors) and appears to line up on byte-sized boundaries encoded as sets of three colors. In BGR space, the colors or just 3-bit values:

* 0 000 Black
* 1 001 Red
* 2 010 Green
* 3 011 Yellow (R+G)
* 4 100 Blue
* 5 101 Magenta (R+B)
* 6 110 Cyan (G+B)

White (111) isn't used as a value. Instead it signifies "repeat last color". For example KWK is just 000.


