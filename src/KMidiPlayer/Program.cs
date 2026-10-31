using System;
using System.Windows.Forms;

namespace KMidiPlayer
{
    internal static class Program
    {
        /// <summary>
        /// アプリケーションのエントリポイント。
        /// </summary>
        /// <param name="args">
        /// 起動時に開くファイル（MIDI ファイル・フォルダ・プレイリストファイル）。
        /// エクスプローラーの「プログラムから開く」や、exe へのドラッグ＆ドロップで渡される。
        /// </param>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }
}
