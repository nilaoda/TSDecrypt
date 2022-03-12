## FFDecsa
FFdecsa is a fast implementation of a CSA decryption algorithm for MPEG TS packets. 
It is shockingly fast, more than 800% the speed of the standard single packet implementation.
Thanks faith89r for your exceptional work !

## Performance
* AMD Duron @ 1.6 GHz : 160 Mbit/s (Mode: 128_SSE)
* Intel P4 @ 3.0 GHz : 310 Mbit/s (Mode: 128_SSE2)
* Intel E8400 @ 3.0 GHz : 668 Mbit/s (Mode: 128_SSE2)
* Intel Core i3 @ 3.4 GHz : 1130 Mbit/s (Mode: 128_SSE2)
 
## How to compile on Windows:
1. Download installer from www.mingw.org and install packages:
“mingw32-base”, “mingw32-gcc-g++”, “msys-base”
2. Run **StartShell.cmd** from FFDecsa sources directory
3. Optional: edit make_dlls.sh and makefile with your options
4. From shell, run **make_dlls.sh**

## Changelog
* v1.0.0
  * public release by fatih89r
* v1.0.1-Altx
  * added windows compatibility
  * removed FFdecsa_test.c
  * updated makefile
  * batch build script (make_dlls.sh)
* v1.0.2-Altx
  * added MEMALIGN_16 & SSE2

## Legal
 As FFdecsa implements a standard, it is completly legal to use it.
 
 Copyright 2003-2004  fatih89r
 Copyright 2004-2015  altxro
 Released under GPL

## Website
https://www.altx.ro/projects/ffdecsa/