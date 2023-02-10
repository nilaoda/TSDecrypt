using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace TSDecryptGUI
{
    internal class Util
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        public static byte[] HexToBytes(string hex)
        {
            hex = hex.Trim();
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        public static String FormatFileSize(Double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00}GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00}MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00}KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0}bytes", fileSize);
            }
        }

        //此函数用于格式化输出时长  
        public static String FormatTime(Int32 time)
        {
            TimeSpan ts = new TimeSpan(0, 0, time);
            string str = "";
            str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            return str;
        }

        /// <summary>
        /// 获取采样数据
        /// </summary>
        /// <param name="inputPid"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static Tuple<string, string, string> GetTsSamples(int inputPid, byte[] data)
        {
            var list = new List<string>();
            var offset = 0;
            using (var stream = new MemoryStream(data))
            {
                //确定起始位置
                while (true)
                {
                    var buffer = new byte[188];
                    var buffer2 = new byte[188];
                    stream.Read(buffer, 0, buffer.Length);
                    stream.Read(buffer2, 0, buffer.Length);
                    if (buffer[0] == 0x47 && buffer2[0] == 0x47)
                        break;
                    stream.Position = ++offset;
                }
                var tsData = new byte[188];
                var size = 0;
                while ((size = stream.Read(tsData, 0, tsData.Length)) > 0)
                {
                    var tsHeaderInt = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? tsData.Take(4).Reverse().ToArray() : tsData.Take(4).ToArray(), 0);
                    var pid = (tsHeaderInt & 0x1fff00) >> 8;
                    if (pid != inputPid)
                        continue;
                    var adaptationControl = (tsHeaderInt & 0x30) >> 4;
                    var payloadUnitStart = (tsHeaderInt & 0x400000) >> 22;
                    if (adaptationControl != 1 || payloadUnitStart != 1)
                        continue;
                    var hexString = BitConverter.ToString(tsData).Replace("-", "");
                    list.Add(hexString);
                    if (list.Count >= 3)
                        break;
                }
            }
            if (list.Count < 3)
                throw new Exception("获取采样数据异常!");
            return new Tuple<string, string, string>(list[0], list[1], list[2]);
        }

        /// <summary>
        /// 判断是否加密并返回PID
        /// </summary>
        /// <param name="data"></param>
        /// <param name="count">置信次数</param>
        /// <returns></returns>
        private static Tuple<bool, int> CheckIsEncrypted(byte[] data, int count = 20)
        {
            int counter = 0;
            bool enc = false;
            int encPid = -1;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x47 && (i + 188) < data.Length && data[i + 188] == 0x47)
                {
                    var tsData = data.Skip(i).Take(188);
                    var tsHeaderInt = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? tsData.Take(4).Reverse().ToArray() : tsData.Take(4).ToArray(), 0);
                    var pid = (tsHeaderInt & 0x1fff00) >> 8;
                    var adaptationControl = (tsHeaderInt & 0x30) >> 4;
                    if (pid > 8191 || pid <= 32 || adaptationControl != 1)
                        continue;
                    var encTag = (tsHeaderInt & 0xc0) >> 6;
                    /**
                     * '00' = Not scrambled.
                     * '01' (0x40) = Reserved for future use
                     * '10' (0x80) = Scrambled with even key
                     * '11' (0xC0) = Scrambled with odd key
                     */
                    if (encTag == 2 || encTag == 3)
                    {
                        if (++counter > count)
                        {
                            enc = true;
                            encPid = (int)pid;
                            break;
                        }
                    }
                    else
                    {
                        counter = 0;
                    }
                }
            }
            return new Tuple<bool, int>(enc, encPid);
        }

        static TSDecrypt tsdecrypt = null;
        public static async Task<bool> CheckCWAsync(string keyTxt, string file)
        {
            var data = new byte[20 * 1024 * 1024];
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                await stream.ReadAsync(data, 0, data.Length);
            }
            var isEnc = CheckIsEncrypted(data);
            if (isEnc.Item1 == false) return false;
            var pid = isEnc.Item2;
            var samples = GetTsSamples(pid, data);
            var ts1 = HexToBytes(samples.Item1);
            var ts2 = HexToBytes(samples.Item2);
            var ts3 = HexToBytes(samples.Item3);
            if (tsdecrypt == null) tsdecrypt = new TSDecrypt();
            tsdecrypt.SetKey(keyTxt);
            tsdecrypt.DecryptBytes(ts1.Length, ref ts1);
            if (!(ts1[4] == 0x00 && ts1[5] == 0x00 && ts1[6] == 0x01)) return false;
            tsdecrypt.DecryptBytes(ts2.Length, ref ts2);
            if (!(ts2[4] == 0x00 && ts2[5] == 0x00 && ts2[6] == 0x01)) return false;
            tsdecrypt.DecryptBytes(ts3.Length, ref ts3);
            if (!(ts3[4] == 0x00 && ts3[5] == 0x00 && ts3[6] == 0x01)) return false;
            return true;
        }
    }
}
