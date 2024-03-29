# TSDecrypt
一个Windows GUI软件，通过调用 [FFdecsa](https://github.com/nilaoda/TSDecrypt/tree/main/FFDeCsa-1.0.2-Altx) 来实现CSA加扰TS文件的高性能解扰. 目前已初步实现基本功能.

![image](Screens/Snipaste_2023-02-11_01-53-41.jpg)


# Performance
* `x86` 机器使用 `PARALLEL_064_MMX` ，在主流CPU上解密速度可达**110MB/s**.
* `x64` 机器使用 `PARALLEL_128_2LONG` ，在主流CPU上解密速度可达**190MB/s**.

输入`benchmark`以测试当前机器最大解密速度

![image](Screens/Snipaste_2023-02-12_02-19-06.jpg)

# Compile dll on Windows
* `x86` https://sourceforge.net/projects/mingw/
* `x64` https://winlibs.com/

# Command Line
```
TSDecryptGUI <INPUT_FILE> [OPTIONS]
  --output-file <str>               Set output file
  --output-dir <str>                Set output directory
  --key <str>                       Set decryption key
  --auto                            Auto decrypt, then close
  --del                             Delete source after done
  --no-check                        Do not check CW
```

# Batch Jobs
Create `.bat` file, write command one by one:
```bat
:: Target file: D:\FEED_1_dec.ts
start /wait TSDecryptGUI.exe D:\FEED_1.ts --key 2021084925aaaa79 --auto

:: Delete source after decryption. Target file: G:\DEC\FEED_2_dec.ts
start /wait TSDecryptGUI.exe D:\FEED_2.ts --key 2022014317aacc8d --output-dir G:\DEC --auto --del

:: Set output file. Target file: G:\DEC\3.ts
start /wait TSDecryptGUI.exe D:\FEED_3.ts --key 2022014317aacc8d --output-file G:\DEC\3.ts --auto --del
```
Run `.bat`


# Thanks

**fatih89r**, 
**Altx**, 
**hez2010** 
