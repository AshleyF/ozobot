# FlashReader

This is a very simple (Windows, sorry) app to read "flash codes" from the [OzoBlockly](http://ozoblockly.com) site.

It works by merely sampling the pixel color under the cursor, logging color changes to one of red, green, blue, cyan, magenta, black or white.

To use it, click load in OzoBlockly and quickly double click the FlashReader to clear. You'll see the color sequence accumulate in the text box (K for black, B for blue, by the way). Very simple. Works perfectly, unlike the previous vision-based reader (now in `../archive`) and a Lego color sensor approach.
