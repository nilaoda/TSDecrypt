# TSDecrypt
一个Windows GUI软件，通过调用 [FFdecsa](https://www.altx.ro/projects/ffdecsa/) 来实现CSA加扰TS文件的高性能解扰. 尚在开发中.

# Performance
* `x86` 机器所用模型为 `PARALLEL_064_MMX` ，在主流CPU上解密速度可达**110MB/s**.
* `x64` 机器所用模型为 `PARALLEL_064_LONG` ，在主流CPU上解密速度可达**180MB/s**.

# Compile on Windows
* `x86` https://sourceforge.net/projects/mingw/
* `x64` https://winlibs.com/

# Command Line
```
TSDecryptGUI <INPUT_FILE> [OPTIONS]
--output-file <str>               Set output file
--output-dir <str>                Set output directory
--key <str>                       Set decryption key
--auto                            Auto decrypt, then close
```