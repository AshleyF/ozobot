# FlashReader

This is a very simple app to read "flash codes" from the [OzoBlockly](http://ozoblockly.com) site. It has been tested under Windows and on Ubuntu with Mono.

It works by merely sampling the pixel color within a transparent "hole" in the window. Placing this hole over the loading pad in OzoBlockly allows it to read the colors. Colors are logged to a text box and also placed on the clipboard for convenience. The beginning and end of each sequence is detected by transistion from/to white and an ellapsed time of > 100ms (it appears that OzoBlockly uses 50ms "frames", BTW).

Very simple. Works perfectly, unlike the previous vision-based reader (now in `../archive`) and a Lego color sensor approach.
