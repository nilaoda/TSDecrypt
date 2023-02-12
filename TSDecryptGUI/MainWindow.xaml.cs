using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace TSDecryptGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PACKET_SIZE = 188;


        bool DONE = false;
        long TOTOAL_SIZE = 0; //文件总大小
        long DE_SIZE = 0; //已解密大小
        long LAST_SIZE = 0;
        double TIME_SPAN = 0; //耗时
        long MESS_COUNT = 0; //解密错误时此值会增加
        Timer tmr = new Timer()
        {
            AutoReset = true
        };


        bool EXIT_AFTER_DONE = false;
        bool DELETE_AFTER_DONE = false;
        bool OVER_WRITTEN = false;


        public MainWindow()
        {
            InitializeComponent();
            try
            {
                /**
                 * COMMAND LINE:
                 *      TSDecryptGUI <INPUT_FILE> [OPTIONS]
                 *      --output-file <str>               Set output file
                 *      --output-dir <str>                Set output directory
                 *      --key <str>                       Set decryption key
                 *      --auto                            Auto decrypt, then close
                 *      --del                             Delete source after done
                 *      --no-check                        Do not check CW
                 */
                #region 解密命令行
                var args = new List<string>(Environment.GetCommandLineArgs());
                if (args.Count > 1 && File.Exists(args[1]) && Path.GetExtension(args[1]).ToLower() == ".ts") 
                {
                    //输入文件
                    Txt_InputFile.Text = Path.GetFullPath(args[1]);
                    //输出文件
                    var i1 = args.IndexOf("--output-file");
                    var ii1 = args.IndexOf("--output-dir");
                    if (i1 >= 0)
                    {
                        Txt_OutputFile.Text = Path.GetFullPath(args[i1 + 1]);
                    }
                    //输出文件夹
                    else if (ii1 > 0 && Directory.Exists(args[ii1 + 1]))
                    {
                        ChangeOutputFile(args[ii1 + 1]);
                    }
                    else
                    {
                        ChangeOutputFile(Path.GetDirectoryName(Txt_InputFile.Text));
                    }
                    //KEY
                    var i2 = args.IndexOf("--key");
                    if (i2 > 0)
                    {
                        var key = args[i2 + 1];
                        Txt_InputKey.Text = key;
                    }
                    //自动删除
                    if (args.Contains("--del"))
                    {
                        DELETE_AFTER_DONE = true;
                    }
                    //自动关闭
                    if (args.Contains("--auto"))
                    {
                        EXIT_AFTER_DONE = true;
                        OVER_WRITTEN = true;
                        Btn_DoDecrypt.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    //不检测
                    if (args.Contains("--no-check"))
                    {
                        Chk_CheckCW.IsChecked = false;
                    }
                }
                #endregion
            }
            catch (Exception) { }
        }

        public delegate void Handler();

        /// <summary>
        /// 根据输入文件设置输出文件全路径
        /// </summary>
        /// <param name="dir">输出文件目录</param>
        private void ChangeOutputFile(string dir)
        {
            var input = Txt_InputFile.Text.Split(';').First();
            Txt_OutputFile.Text = Path.Combine(dir, Path.GetFileNameWithoutExtension(input) + "_dec" + Path.GetExtension(input));
        }

        private void SelectedFile(UIElement ele, Handler any = null, bool multiFile = false)
        {
            var textbox = ele as TextBox;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MPEG-TS文件(*.ts)|*.ts";
            openFileDialog.Multiselect = multiFile;
            if (openFileDialog.ShowDialog() == true)
            {
                textbox.Text = openFileDialog.FileName;
                if (any != null) any();
            }
        }

        private void Btn_SelectFile_Click(object sender, RoutedEventArgs e)
        {
            SelectedFile(Txt_InputFile, () =>
            {
                ChangeOutputFile(Path.GetDirectoryName(Txt_InputFile.Text.Split(';').First()));
            }, true);
        }

        private void Btn_SelectOutputFile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Txt_InputFile.Text) || !File.Exists(Txt_InputFile.Text))
            {
                MessageUtil.AlertInfo("请确保输入文件正确!");
                return;
            }
            SelectedFile(Txt_OutputFile);
        }

        private void GroupBox_Input_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void GroupBox_Input_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var ts = files.Where(f => Path.GetExtension(f).ToUpper() == ".TS");
                if (ts.Any())
                {
                    //按住Ctrl键时 新增文件
                    if (e.KeyStates == DragDropKeyStates.ControlKey)
                    {
                        Txt_InputFile.Text = string.Join(";", Txt_InputFile.Text.Split(';').Concat(ts));
                    }
                    else
                    {
                        Txt_InputFile.Text = string.Join(";", ts);
                        ChangeOutputFile(Path.GetDirectoryName(Txt_InputFile.Text.Split(';').First()));
                    }
                }
            }
        }

        private void GroupBox_Output_Drop(object sender, DragEventArgs e)
        {
            if (string.IsNullOrEmpty(Txt_InputFile.Text) || !File.Exists(Txt_InputFile.Text))
            {
                MessageUtil.AlertInfo("请确保输入文件正确!");
                return;
            }
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string path = ((string[])e.Data.GetData(DataFormats.FileDrop)).First();
                if (File.Exists(path))
                {
                    var dir = Path.GetDirectoryName(path);
                    ChangeOutputFile(dir);
                }
                else if (Directory.Exists(path))
                {
                    ChangeOutputFile(path);
                }
            }
        }

        private void GroupBox_Output_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private async void Btn_DoDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //测试速度
                if (Txt_InputFile.Text.ToLower() == "benchmark")
                {
                    BenchmarkUtil.Run((size, time) =>
                    {
                        var speed = (double)size / time * 1000;
                        MessageBox.Show($"{Util.FormatFileSize(speed)}/s", "最大解密速度");
                    });
                    return;
                }

                if (tmr != null)
                {
                    tmr.Dispose();
                    tmr = new Timer()
                    {
                        AutoReset = true
                    };
                }

                if (Btn_DoDecrypt.Content.ToString() == "停止")
                {
                    DONE = true;
                    tmr.Elapsed -= ReportStatus;
                    tmr.Stop();
                    Btn_DoDecrypt.Content = "开始解密";
                    return;
                }

                var keyTxt = Txt_InputKey.Text.Trim();
                if (keyTxt.Contains(" ")) keyTxt = string.Join("", keyTxt.Split(' ').Select(s => s.PadLeft(2, '0')));
                if (keyTxt.Length != 12 && keyTxt.Length != 16) 
                    throw new Exception("KEY格式有误!");

                tmr.Interval = Convert.ToDouble(Txt_TimerInterval.Text);
                var offset = 0L;
                var limit = 0L;
                if (Txt_PktsOffset.Text.StartsWith("0x"))
                {
                    offset = Convert.ToInt64(Txt_PktsOffset.Text.Substring(2), 16) * PACKET_SIZE;
                }
                else
                {
                    offset = Convert.ToInt64(Txt_PktsOffset.Text) * PACKET_SIZE;
                }

                if (!string.IsNullOrEmpty(Txt_PktsLimit.Text))
                {
                    if (Txt_PktsLimit.Text.StartsWith("0x"))
                    {
                        limit = Convert.ToInt64(Txt_PktsLimit.Text.Substring(2), 16) * PACKET_SIZE;
                    }
                    else
                    {
                        limit = Convert.ToInt64(Txt_PktsLimit.Text) * PACKET_SIZE;
                    }
                }

                TOTOAL_SIZE = Txt_InputFile.Text.Split(';').Sum(f => new FileInfo(f).Length) - offset;
                if (limit != 0) TOTOAL_SIZE = limit;
                if (TOTOAL_SIZE == 0)
                {
                    throw new Exception("输入文件为空!!");
                }
                if (File.Exists(Txt_OutputFile.Text))
                {
                    if (!OVER_WRITTEN && !MessageUtil.AlertConfirm("输出文件已存在! 要删除并继续吗?")) return;
                    else File.Delete(Txt_OutputFile.Text);
                }
                if (Util.GetDiskFreeSpaceEx(Path.GetDirectoryName(Txt_OutputFile.Text), out ulong freeSize, out _, out _) && (long)freeSize < TOTOAL_SIZE)
                {
                    throw new Exception("输出磁盘空间不足!!");
                }

                //检测KEY是否正确
                if (Chk_CheckCW.IsChecked == true)
                {
                    Btn_DoDecrypt.IsEnabled = false;
                    if (!await Util.CheckCWAsync(keyTxt, Txt_InputFile.Text.Split(';').First(), offset)) 
                        throw new Exception("CW错误或非加密文件!!");
                }


                Btn_DoDecrypt.IsEnabled = true;
                DE_SIZE = LAST_SIZE = MESS_COUNT = 0;
                TIME_SPAN = 0d;
                DONE = false;
                ProBar.Value = 0;
                var tsdecrypt = new TSDecrypt();
                tsdecrypt.SetKey(keyTxt);
                tmr.Elapsed += ReportStatus;
                Btn_DoDecrypt.Content = "停止";
                await DecryptTaskAsync(tsdecrypt, offset, limit, Txt_InputFile.Text, Txt_OutputFile.Text);
                DONE = true;
                if (DELETE_AFTER_DONE)
                {
                    if (File.Exists(Txt_InputFile.Text) && File.Exists(Txt_OutputFile.Text))
                    {
                        var sizeDiff = Math.Abs(new FileInfo(Txt_InputFile.Text).Length - new FileInfo(Txt_OutputFile.Text).Length);
                        if (sizeDiff <= 10 * 1024 * 1024) File.Delete(Txt_InputFile.Text);
                    }
                }
                if (EXIT_AFTER_DONE)
                {
                    //退出程序
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Btn_DoDecrypt.IsEnabled = true;
                Btn_DoDecrypt.Content = "开始解密";
                tmr.Stop();
                MessageUtil.AlertInfo(ex.Message);
            }
        }

        /// <summary>
        /// 汇报进度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportStatus(object sender, ElapsedEventArgs e)
        {
            this.ProBar.Dispatcher.Invoke(new Action(() =>
            {
                if (DE_SIZE == TOTOAL_SIZE || DONE)
                {
                    //ProBar.Value = ProBar.Maximum;
                    tmr.Stop();
                    Btn_DoDecrypt.Content = "开始解密";
                }
                TIME_SPAN += tmr.Interval;
                var speed = (DE_SIZE - LAST_SIZE) / tmr.Interval * 1000;
                Txt_Status.Text = $"耗时: {Util.FormatTime((int)(TIME_SPAN / 1000))}, " +
                    $"速度: {Util.FormatFileSize(speed)}/s, " +
                    $"进度: {Util.FormatFileSize(DE_SIZE)}/{Util.FormatFileSize(TOTOAL_SIZE)} ({((double)DE_SIZE / TOTOAL_SIZE * 100).ToString("0.00")}%), " +
                    $"预计剩余时间: {Util.FormatTime((int)((TOTOAL_SIZE - DE_SIZE) / (speed == 0 ? 1 : speed)))}" +
                    $"{(MESS_COUNT > 0 ? ", Errors: " + MESS_COUNT : "")}";
                LAST_SIZE = DE_SIZE;
                ProBar.Value = Math.Ceiling((double)DE_SIZE / TOTOAL_SIZE * 1000);
                //完成后报告
                if (ProBar.Value == ProBar.Maximum)
                {
                    ProBar.Value = 0;
                    Txt_Status.Text = $"耗时: {Util.FormatTime((int)(TIME_SPAN / 1000))}, " +
                    $"平均速度: {Util.FormatFileSize(DE_SIZE / TIME_SPAN * 1000)}/s, " +
                    $"进度: {Util.FormatFileSize(DE_SIZE)}/{Util.FormatFileSize(TOTOAL_SIZE)} ({((double)DE_SIZE / TOTOAL_SIZE * 100).ToString("0.00")}%)";
                }
            }));
        }

        async Task DecryptTaskAsync(TSDecrypt tsdecrypt, long offset, long limit, string input, string outpout)
        {
            await Task.Run(() => DecryptTask(tsdecrypt, offset, limit, input, outpout));
        }

        void DecryptTask(TSDecrypt tsdecrypt, long offset, long limit, string input, string outpout)
        {
            var buffer = new byte[PACKET_SIZE * tsdecrypt.PARALL_SIZE]; // 64 TS Packets
            int readSize;
            var errorDic = new Dictionary<long, int>(); //Position == Counter
            using (var output = new BufferedStream(new FileStream(outpout, FileMode.Create)))
            {
                foreach (var oneFile in input.Split(';'))
                {
                    using (var stream = new FileStream(oneFile, FileMode.Open, FileAccess.Read))
                    {
                        //偏移
                        if (offset < stream.Length) stream.Position = offset;
                        tmr.Start(); //开始刷新状态
                        while (!DONE && (readSize = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            //解密限制
                            if (limit != 0 && DE_SIZE > limit)
                            {
                                TOTOAL_SIZE = DE_SIZE;
                                break;
                            }
                            if (buffer[0] != 0x47)
                            {
                                stream.Position = stream.Position - readSize + 1;
                                continue;
                            }
                            //var before = new byte[buffer.Length];
                            //Array.Copy(buffer, 0, before, 0, buffer.Length);
                            //解密buffer中的数据
                            var result = tsdecrypt.DecryptBytes(readSize, ref buffer);
                            if (result != -1)
                            {
                                if (result == (readSize / PACKET_SIZE)) 
                                {
                                    output.Write(buffer, 0, readSize);
                                    DE_SIZE += readSize;
                                }
                                else
                                {
                                    //仅解密了部分，需要回退再解密
                                    var toWriteSize = result * PACKET_SIZE;
                                    output.Write(buffer, 0, toWriteSize);
                                    DE_SIZE += toWriteSize;
                                    stream.Position -= readSize - toWriteSize;
                                }
                            }
                            else
                            {
                                MESS_COUNT++;
                                TOTOAL_SIZE -= 188;
                                stream.Position = stream.Position - readSize + 188;
                                if (errorDic.Keys.Count > 0 && stream.Position == errorDic.Keys.Last() + 188 * errorDic[errorDic.Keys.Last()])
                                {
                                    errorDic[errorDic.Keys.Last()]++;
                                }
                                else
                                {
                                    errorDic[stream.Position] = 1;
                                }
                                continue;
                            }
                        }
                    }
                }
                DONE = true;
            }
            if (errorDic.Count > 0)
            {
                File.WriteAllText(outpout + ".errors.log",
                    $"Source: {input}{Environment.NewLine}{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errorDic.Select((k, v) => $"skipped {k.Value} packets after pointer: #{k.Key}")) +
                    $"{Environment.NewLine}{Environment.NewLine}Total: {errorDic.Values.Sum()} packets skipped"
                    );
            }
        }

        private void Chk_Del_Checked(object sender, RoutedEventArgs e)
        {
            DELETE_AFTER_DONE = Chk_Del.IsChecked == true;
        }
    }
}
