################ FFDecsa mode
#FFMODE=064_MMX
FFMODE=128_SSE2

################ Processor
DPROC=C2

################ Compiling with g++ gives a little more speed
#COMPILER=gcc
COMPILER=g++

# Compiler CFLAGS 
CFLAGS = -Wall -O2 -fomit-frame-pointer -fexpensive-optimizations -funroll-loops 

ifeq ($(COMPILER),gcc)
  CFLAGS += -std=c99
endif  

ifeq ($(DPROC),AX)  
  CFLAGS += -march=athlon-xp
endif  

ifeq ($(DPROC),P4)
  CFLAGS += -march=pentium4
endif

ifeq ($(DPROC),C2)
  CFLAGS += -march=core2
endif

ifeq ($(DPROC),CI)
  CFLAGS += -march=core-avx2
endif




 
  
# If compiled with Gcc 4.4.0 use -O2 and the following CFLAGS:
CFLAGS += -fgcse-after-reload -finline-functions -fpredictive-commoning -ftree-vectorize -funswitch-loops


H_FILES = parallel_generic.h parallel_std_def.h fftable.h \
          parallel_032_4char.h \
          parallel_032_int.h \
          parallel_064_2int.h \
          parallel_064_8charA.h \
          parallel_064_8char.h \
          parallel_064_long.h \
          parallel_064_mmx.h \
          parallel_128_16charA.h \
          parallel_128_16char.h \
          parallel_128_2long.h \
          parallel_128_2mmx.h \
          parallel_128_4int.h \
          parallel_128_sse2.h \
          parallel_128_sse.h

all:FFdecsa_dll

# Discards all the debugging stuff before compilation
tmp_FFdecsa.c: FFdecsa.c
	echo "#define PARALLEL_MODE PARALLEL_$(FFMODE)" >tmp_ffmode.h
	grep -v DBG FFdecsa.c >tmp_FFdecsa.c

tmp_stream.c: stream.c
	grep -v DBG stream.c >tmp_stream.c

###############################################################################
# Warning: .DEF file must be edited to fix exported function names (altx)!

def:FFdecsa_def
dll:FFdecsa_dll

FFdecsa_def: FFdecsaDll.o
	$(COMPILER) -shared -o FFdecsa.dll FFdecsaDll.o -Wl,--kill-at,--output-def,FFDecsa_gxx.def

FFdecsa_dll: FFdecsaDll.o
ifeq ($(COMPILER),gcc)
	dllwrap -o FFDecsa_v2_$(FFMODE)_$(DPROC)_Gcc.dll -Wl,--strip-all,--gc-sections --def FFdecsa_gcc.def FFdecsaDll.o
else
	dllwrap -o FFDecsa.dll -Wl,--strip-all,--gc-sections --def FFdecsa_gpp.def FFdecsaDll.o
endif	
	

#Compile object file with flag "MAKE_DLL" 
FFdecsaDll.o: tmp_FFdecsa.c tmp_stream.c  $(H_FILES)
	$(COMPILER) $(CFLAGS) -DMAKE_DLL  -c tmp_FFdecsa.c -o FFdecsaDll.o
	
	
###############################################################################

clean:
	rm -f *.o tmp_*.c tmp_*.h || true
