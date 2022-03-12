# This script will generate multiple dlls by combining values 
# from LIST_PROC, LIST_MODE and LIST_COMP
#
# LIST_PROC: processor optimisation flags passed to compiler
#  C2 : -march=core2
#  P4 : -march=pentium4
#  AX : -march=athlon-xp 
# LIST_MODE: see PARALLEL_??? from ffdesca.c
# LIST_COMP: compilers to use gcc or g++
#-------------------------------------------------------------------


LIST_PROC="CI"
LIST_MODE="064_MMX"
LIST_COMP="g++"

rm -f *.dll

for P in $LIST_PROC
do 
 for M in $LIST_MODE 
 do
  for C in $LIST_COMP
  do
   echo "---------------------------------------------------------"
   make clean && make dll -e DPROC=$P FFMODE=$M COMPILER=$C 
  done 
 done 
done 

make clean

