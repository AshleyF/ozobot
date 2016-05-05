#!/bin/bash

CODE=$(mono ./bin/FlashAsm.exe); mono ./bin/FlashWriter.exe "${CODE}"
