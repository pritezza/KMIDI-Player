using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 音源定義ファイルのフォルダ（Modules）と、選んでいる音源定義ファイル。チャンネルパネルの音色名を決める。
    /// </summary>
    /// <remarks>
    /// フォルダには、同梱の GM2 の定義（GM2.xml）を書き出しておく（無ければ作り直す）。
    /// 名前は、選んでいるファイルで探し、見つからなければ GM2 の名前（<see cref="Gm2Sounds"/>）にする。
    /// </remarks>
    public sealed class InstrumentLibrary
    {
        public const string FolderName = "Modules";
        public const string BuiltInFileName = "GM2.xml";

        /// <summary>読み込んだときの、選んでいるファイルの更新日時（書き換えられたら読み込み直すため）。</summary>
        private DateTime _loadedWriteTime;

        public InstrumentLibrary(string folderPath)
        {
            FolderPath = folderPath;
        }

        /// <summary>音源定義ファイルを置くフォルダ。</summary>
        public string FolderPath { get; }

        /// <summary>選んでいる音源定義。読み込んでいなければ null（GM2 の名前だけを使う）。</summary>
        public InstrumentDefinition Current { get; private set; }

        /// <summary>選んでいる音源の名前（読み込んでいなければ「GM2」）。</summary>
        public string CurrentName => Current?.Name ?? "GM2";

        /// <summary>選んでいる音源定義が変わる（読み込み直しも含む）たびに増える。名前を覚えておく側が、探し直すかどうかの判定に使う。</summary>
        public int Version { get; private set; }

        /// <summary>フォルダと、同梱の GM2 の定義ファイルが無ければ作る。作れなくても何もしない（GM2 の名前は使える）。</summary>
        public void EnsureFolder()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                string builtIn = Path.Combine(FolderPath, BuiltInFileName);
                if (!File.Exists(builtIn))
                {
                    Gm2Sounds.WriteDefinitionFile(builtIn);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 書き込めない場所でも、名前は GM2 のものを出せる
            }
        }

        /// <summary>フォルダにある音源定義ファイルの一覧（音源名の順）。音源定義ファイルでない XML は除く。</summary>
        public IReadOnlyList<DefinitionFile> ListFiles()
        {
            string[] paths;
            try
            {
                paths = Directory.GetFiles(FolderPath, "*.xml");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return new DefinitionFile[0];
            }
            return paths
                .Select(path => new DefinitionFile(path, InstrumentDefinition.ReadName(path)))
                .Where(file => file.Name != null)
                .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// フォルダの中のファイル（ファイル名）を選んで読み込む。読み込めなければ例外を投げ、今の選択はそのままにする。
        /// </summary>
        /// <exception cref="IOException">ファイルを読めない（ファイル名に使えない文字があるときも）。</exception>
        /// <exception cref="UnauthorizedAccessException">ファイルを読む権限が無い。</exception>
        /// <exception cref="InvalidDataException">音源定義ファイルとして読めない。</exception>
        public void Load(string fileName)
        {
            string path;
            try
            {
                path = Path.Combine(FolderPath, Path.GetFileName(fileName));
            }
            catch (ArgumentException ex)
            {
                // 設定ファイルを書き換えて、ファイル名に使えない文字（| など）が入っている場合
                throw new IOException(ex.Message, ex);
            }
            DateTime writeTime = File.GetLastWriteTimeUtc(path);
            Current = InstrumentDefinition.Load(path);
            _loadedWriteTime = writeTime;
            Version++;
        }

        /// <summary>選んでいるファイルが書き換えられていたら、読み込み直す。読み込めなければ、今の内容のままにする。</summary>
        public void ReloadIfChanged()
        {
            if (Current == null || !File.Exists(Current.FilePath) || File.GetLastWriteTimeUtc(Current.FilePath) == _loadedWriteTime)
            {
                return;
            }
            try
            {
                Load(Current.FilePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                // 書き換えの途中などで読めなければ、前の内容で表示を続ける
            }
        }

        /// <summary>音色の名前（選んでいる音源定義で見つからなければ GM2 の名前）。</summary>
        public string GetName(InstrumentState state)
        {
            return Current?.FindName(state) ?? Gm2Sounds.GetName(state);
        }

        /// <summary>フォルダの中の音源定義ファイル。</summary>
        public sealed class DefinitionFile
        {
            public DefinitionFile(string path, string name)
            {
                Path = path;
                Name = name;
            }

            public string Path { get; }

            public string FileName => System.IO.Path.GetFileName(Path);

            /// <summary>音源名（ModuleData の Name）。</summary>
            public string Name { get; }
        }
    }
}
