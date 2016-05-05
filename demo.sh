#!/bin/bash

CODE=$(mono ./FlashAsm/FlashAsm.exe); mono ./FlashWriter/FlashWriter.exe "${CODE}"
