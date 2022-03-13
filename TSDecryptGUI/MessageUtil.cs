using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TSDecryptGUI
{
    internal class MessageUtil
    {
        public static MessageBoxResult AlertInfo(string msg)
        {
            return MessageBox.Show(msg, "提示信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static MessageBoxResult AlertError(string msg)
        {
            return MessageBox.Show(msg, "错误信息", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool AlertConfirm(string msg)
        {
            return MessageBox.Show(msg, "请确认", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
        }
    }
}
