using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// 音源定義ファイル（Domino の形式の XML）から読み込んだ、音色名とドラムセット名の一覧。
    /// </summary>
    /// <remarks>
    /// 使うのは、ModuleData の Name と、InstrumentList・DrumSetList の Map / PC / Bank だけ（他の要素は読み飛ばす）。
    /// Domino と同じく、タグ名・属性名の大文字・小文字は区別しない。
    /// <code>
    /// &lt;ModuleData Name="音源名"&gt;
    ///   &lt;InstrumentList&gt;
    ///     &lt;Map Name="マップ名"&gt;
    ///       &lt;PC Name="メインの音色名" PC="1～128"&gt;
    ///         &lt;Bank Name="音色名" MSB="0～127" LSB="0～127" /&gt;  （MSB・LSB は省略、または 255 で「指定なし」）
    ///   &lt;DrumSetList&gt; …（同じ形。Bank の中に各キーの Tone がある）
    /// </code>
    /// </remarks>
    public sealed class InstrumentDefinition
    {
        private const int NotSpecified = -1;

        // マップごとの、プログラム番号（0～127）から PC の定義（ファイルに書かれた順）
        private readonly List<Dictionary<int, ProgramEntry>> _instrumentMaps;
        private readonly List<Dictionary<int, ProgramEntry>> _drumMaps;

        private InstrumentDefinition(string name, string filePath, List<Dictionary<int, ProgramEntry>> instrumentMaps, List<Dictionary<int, ProgramEntry>> drumMaps)
        {
            Name = name;
            FilePath = filePath;
            _instrumentMaps = instrumentMaps;
            _drumMaps = drumMaps;
        }

        /// <summary>音源名（ModuleData の Name）。</summary>
        public string Name { get; }

        public string FilePath { get; }

        /// <summary>
        /// 音源定義ファイルを読み込む。
        /// </summary>
        /// <exception cref="IOException">ファイルを読めない。</exception>
        /// <exception cref="UnauthorizedAccessException">ファイルを読む権限が無い。</exception>
        /// <exception cref="InvalidDataException">XML として正しくない、または音源定義ファイルではない。</exception>
        public static InstrumentDefinition Load(string filePath)
        {
            XElement root;
            try
            {
                root = XDocument.Load(filePath).Root;
            }
            catch (XmlException)
            {
                // 配布されているファイルには、XML として少し崩れたもの（値の無い属性など）があるが、Domino では読める。
                // 同じように読めるよう、タグと属性だけを拾う読み方で読み直す
                root = TolerantXml.Parse(File.ReadAllBytes(filePath));
            }
            if (root == null || !IsNamed(root, "ModuleData"))
            {
                throw new InvalidDataException("<ModuleData> not found.");
            }

            string name = Attribute(root, "Name");
            return new InstrumentDefinition(
                string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(filePath) : name.Trim(),
                filePath,
                ReadMaps(root, "InstrumentList"),
                ReadMaps(root, "DrumSetList"));
        }

        /// <summary>
        /// ファイルの先頭だけを読んで、音源名（ModuleData の Name）を返す（一覧を作る用。中身全体は読まない）。
        /// 音源定義ファイルでなければ null。
        /// </summary>
        public static string ReadName(string filePath)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(filePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true }))
                {
                    if (reader.MoveToContent() != XmlNodeType.Element || !string.Equals(reader.LocalName, "ModuleData", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                    while (reader.MoveToNextAttribute())
                    {
                        if (string.Equals(reader.LocalName, "Name", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(reader.Value))
                        {
                            return reader.Value.Trim();
                        }
                    }
                    return Path.GetFileNameWithoutExtension(filePath);
                }
            }
            catch (XmlException)
            {
                // 先頭のタグが崩れているなど。読み込み（Load）と同じく、タグと属性だけを拾う読み方で読み直す
                return ReadNameTolerantly(filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// ファイルの先頭（<see cref="NameProbeBytes"/> バイト）を、崩れた XML でも読める読み方で読んで、音源名を返す。
        /// 音源定義ファイルでなければ null。
        /// </summary>
        private static string ReadNameTolerantly(string filePath)
        {
            byte[] head;
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    head = new byte[Math.Min(stream.Length, NameProbeBytes)];
                    int read = 0;
                    while (read < head.Length)
                    {
                        int count = stream.Read(head, read, head.Length - read);
                        if (count == 0)
                        {
                            break;
                        }
                        read += count;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }

            // 途中で切れていても、閉じていない要素は閉じたものとして読める
            XElement root = TolerantXml.Parse(head);
            if (root == null || !IsNamed(root, "ModuleData"))
            {
                return null;
            }
            string name = Attribute(root, "Name");
            return string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(filePath) : name.Trim();
        }

        /// <summary>音源名を探すときに読む、ファイルの先頭の大きさ（ModuleData の開始タグが入れば足りる）。</summary>
        private const int NameProbeBytes = 16 * 1024;

        /// <summary>
        /// 音色の名前を探す。見つからなければ null。
        /// </summary>
        /// <remarks>
        /// 次の順に探し、どれもファイルに書かれた順（マップの順、その中の Bank の順）で最初に見つかったものを使う。
        /// <list type="number">
        /// <item>バンクセレクトの値が合う Bank（定義で MSB・LSB を指定していなければ、どの値にも合うものとする。
        /// 曲が LSB を送っていなければ、MSB だけで探す。MSB を送っていなければ 0 とする）</item>
        /// <item>その PC のメインの名前（PC の Name）</item>
        /// </list>
        /// </remarks>
        public string FindName(InstrumentState state)
        {
            List<Dictionary<int, ProgramEntry>> maps = state.IsDrum ? _drumMaps : _instrumentMaps;
            if (state.BankMsb >= 0 || state.BankLsb >= 0)
            {
                int msb = Math.Max(0, state.BankMsb);
                foreach (Dictionary<int, ProgramEntry> map in maps)
                {
                    if (map.TryGetValue(state.Program, out ProgramEntry entry))
                    {
                        foreach (BankEntry bank in entry.Banks)
                        {
                            if (bank.Matches(msb, state.BankLsb))
                            {
                                return bank.Name;
                            }
                        }
                    }
                }
            }
            foreach (Dictionary<int, ProgramEntry> map in maps)
            {
                if (map.TryGetValue(state.Program, out ProgramEntry entry) && !entry.IsPlaceholder)
                {
                    return entry.Name;
                }
            }
            return null;
        }

        private static List<Dictionary<int, ProgramEntry>> ReadMaps(XElement root, string listName)
        {
            var maps = new List<Dictionary<int, ProgramEntry>>();
            foreach (XElement list in Children(root, listName))
            {
                foreach (XElement mapElement in Children(list, "Map"))
                {
                    var map = new Dictionary<int, ProgramEntry>();
                    foreach (XElement pc in Children(mapElement, "PC"))
                    {
                        int number = Number(pc, "PC");
                        string name = Attribute(pc, "Name")?.Trim();
                        if (number < 1 || number > 128 || string.IsNullOrEmpty(name) || map.ContainsKey(number - 1))
                        {
                            continue;
                        }
                        List<XElement> bankElements = Children(pc, "Bank").ToList();
                        List<BankEntry> banks = bankElements
                            .Select(bank => new BankEntry(Attribute(bank, "Name")?.Trim(), Number(bank, "MSB"), Number(bank, "LSB")))
                            .Where(bank => !string.IsNullOrEmpty(bank.Name))
                            .ToList();

                        // 「All Program Change」のような、どのプログラム番号でも選べるようにするための項目
                        // （どの Bank も MSB・LSB に 255 = 送らない、をわざわざ書いている）は、音色名としては使わない
                        bool placeholder = bankElements.Count > 0 && bankElements.All(bank =>
                            Attribute(bank, "MSB")?.Trim() == "255" && Attribute(bank, "LSB")?.Trim() == "255");
                        map.Add(number - 1, new ProgramEntry(name, banks, placeholder));
                    }
                    if (map.Count > 0)
                    {
                        maps.Add(map);
                    }
                }
            }
            return maps;
        }

        private static bool IsNamed(XElement element, string name)
        {
            return string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<XElement> Children(XElement parent, string name)
        {
            return parent.Elements().Where(element => IsNamed(element, name));
        }

        private static string Attribute(XElement element, string name)
        {
            return element.Attributes().FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        /// <summary>0～127 の数の属性。無い・255（指定なし）・範囲外・数でないときは <see cref="NotSpecified"/>。PC は 1～128。</summary>
        private static int Number(XElement element, string name)
        {
            string text = Attribute(element, name);
            if (!int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return NotSpecified;
            }
            int max = name == "PC" ? 128 : 127;
            return value >= 0 && value <= max ? value : NotSpecified;
        }

        private sealed class ProgramEntry
        {
            public ProgramEntry(string name, List<BankEntry> banks, bool isPlaceholder)
            {
                Name = name;
                Banks = banks;
                IsPlaceholder = isPlaceholder;
            }

            public string Name { get; }

            public List<BankEntry> Banks { get; }

            /// <summary>どのプログラム番号でも選べるようにするための項目（名前を音色名として使わない）。</summary>
            public bool IsPlaceholder { get; }
        }

        private sealed class BankEntry
        {
            private readonly int _msb;
            private readonly int _lsb;

            public BankEntry(string name, int msb, int lsb)
            {
                Name = name;
                _msb = msb;
                _lsb = lsb;
            }

            public string Name { get; }

            /// <param name="lsb">曲の LSB。送っていなければ -1（LSB は比べない）。</param>
            public bool Matches(int msb, int lsb)
            {
                if (_msb == NotSpecified && _lsb == NotSpecified)
                {
                    return false; // バンクを持たない音源の書き方。メインの名前と同じなので、PC の Name で探す
                }
                return (_msb == NotSpecified || _msb == msb) && (_lsb == NotSpecified || lsb < 0 || _lsb == lsb);
            }
        }
    }
}
