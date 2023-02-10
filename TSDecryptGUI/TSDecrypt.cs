using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TSDecryptGUI
{
    internal class TSDecrypt
    {
        [DllImport("FFDecsa_128_2LONG", EntryPoint = "_Z15decrypt_packetsPPhP10csa_keys_t", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        static unsafe extern int decrypt_packets(byte*[] cluster, ref csa_keys_t csa_keys_t);
        [DllImport("FFDecsa_128_2LONG", EntryPoint = "_Z15get_keyset_sizev")]
        static extern int get_keyset_size();
        [DllImport("FFDecsa_128_2LONG", EntryPoint = "_Z24get_internal_parallelismv")]
        static extern int get_parallelism();
        [DllImport("FFDecsa_128_2LONG", EntryPoint = "_Z17set_control_wordsPhS_P10csa_keys_t", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        static unsafe extern void set_control_words(byte[] even, byte[] odd, ref csa_keys_t csa_keys_t);

        static csa_keys_t KEY_SET = new csa_keys_t();
        public int PARALL_SIZE = get_parallelism();

        /**
         * X64 PARALLEL_064_LONG
         */

        //[StructLayout(LayoutKind.Explicit)]
        //public struct csa_keys_t
        //{
        //    [FieldOffset(0)] public csa_key_t even;
        //    [FieldOffset(1600)] public csa_key_t odd;
        //}
        //
        //[StructLayout(LayoutKind.Explicit)]
        //public unsafe struct csa_key_t
        //{
        //    [FieldOffset(0)] public fixed byte ck[8];
        //    [FieldOffset(8)] public fixed int iA[8];
        //    [FieldOffset(40)] public fixed int iB[8];
        //    [FieldOffset(72)] public fixed int ck_g[64 * 2];
        //    [FieldOffset(584)] public fixed int iA_g[32 * 2];
        //    [FieldOffset(840)] public fixed int iB_g[32 * 2];
        //    [FieldOffset(1096)] public fixed byte kk[56];
        //    [FieldOffset(1152)] public fixed uint kkmulti[56 * 2];
        //}

        /**
         * X64 PARALLEL_128_2LONG
         */

        public struct csa_keys_t
        {
            public csa_key_t even;
            public csa_key_t odd;
        }
        
        public unsafe struct csa_key_t
        {
            public fixed byte ck[8];
            public fixed int iA[8];
            public fixed int iB[8];
            public fixed int ck_g[64 * 4];
            public fixed int iA_g[32 * 4];
            public fixed int iB_g[32 * 4];
            public fixed byte kk[56];
            public fixed uint kkmulti[56 * 4];
        }
        

        public void SetKey(string keyTxt)
        {
            var decKey = new byte[8];
            var bytes = Util.HexToBytes(keyTxt);
            if (bytes.Length == 6)
            {
                //计算hash
                decKey[0] = bytes[0];
                decKey[1] = bytes[1];
                decKey[2] = bytes[2];
                decKey[3] = (byte)((bytes[0] + bytes[1] + bytes[2]) % 256);
                decKey[4] = bytes[3];
                decKey[5] = bytes[4];
                decKey[6] = bytes[5];
                decKey[7] = (byte)((bytes[3] + bytes[4] + bytes[5]) % 256);
            }
            else
            {
                decKey = bytes;
            }
            set_control_words(decKey, decKey, ref KEY_SET);
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        public unsafe int DecryptBytes(int size, ref byte[] encBytes)
        {
            int result = 0;
            try
            {
                var cluster = new byte*[PARALL_SIZE];
                fixed (byte* pOneBuf = encBytes)
                {
                    cluster[0] = pOneBuf;
                    cluster[1] = pOneBuf + size;
                    cluster[2] = null;
                    result = decrypt_packets(cluster, ref KEY_SET);
                }
            }
            catch (System.AccessViolationException)
            {
                result = - 1;
            }
            return result;
        }
    }
}
