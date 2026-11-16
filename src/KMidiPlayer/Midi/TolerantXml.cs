using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace KMidiPlayer.Midi
{
    /// <summary>
    /// XML として少し崩れたファイルを、タグと属性だけを拾って要素の木にする（音源定義ファイルの読み直し用）。
    /// </summary>
    /// <remarks>
    /// 値の無い属性（<c>Name="Timbale H" V Key="65"</c> の <c>V</c>）など、形の崩れた部分は読み飛ばす。
    /// 閉じタグが無い要素は、親の閉じタグか、ファイルの終わりで閉じたものとする。対応する開始タグの無い閉じタグは無視する。
    /// </remarks>
    internal static class TolerantXml
    {
        private static readonly Regex CommentPattern = new Regex(@"<!--.*?-->", RegexOptions.Singleline);
        private static readonly Regex TagPattern = new Regex(@"<(/?)([A-Za-z_][\w.\-]*)([^<>]*)>");
        private static readonly Regex AttributePattern = new Regex(@"([A-Za-z_][\w.\-]*)\s*=\s*(?:""([^""]*)""|'([^']*)')");
        private static readonly Regex EncodingPattern = new Regex(@"^\s*<\?xml[^>]*encoding\s*=\s*[""']([\w.\-]+)[""']");

        /// <summary>一番外側の要素を返す。要素が 1 つも無ければ null。</summary>
        public static XElement Parse(byte[] data)
        {
            string text = CommentPattern.Replace(Decode(data), string.Empty);
            XElement root = null;
            var open = new List<XElement>();
            foreach (Match tag in TagPattern.Matches(text))
            {
                string name = tag.Groups[2].Value;
                if (tag.Groups[1].Value == "/")
                {
                    // 対応する開始タグまで閉じる（間の閉じ忘れた要素も一緒に閉じる）
                    int index = open.FindLastIndex(openElement => string.Equals(openElement.Name.LocalName, name, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        open.RemoveRange(index, open.Count - index);
                    }
                    continue;
                }

                XElement element;
                try
                {
                    element = new XElement(name);
                }
                catch (XmlException)
                {
                    continue; // XML の名前として使えない文字が入っている
                }
                string body = tag.Groups[3].Value;
                foreach (Match attribute in AttributePattern.Matches(body))
                {
                    string value = attribute.Groups[2].Success ? attribute.Groups[2].Value : attribute.Groups[3].Value;
                    try
                    {
                        element.SetAttributeValue(attribute.Groups[1].Value, WebUtility.HtmlDecode(value));
                    }
                    catch (XmlException)
                    {
                        // 使えない名前の属性は無視する
                    }
                }

                if (open.Count > 0)
                {
                    open[open.Count - 1].Add(element);
                }
                else if (root == null)
                {
                    root = element;
                }
                else
                {
                    continue; // 2 つ目以降の一番外側の要素は無視する
                }
                if (!body.TrimEnd().EndsWith("/", StringComparison.Ordinal))
                {
                    open.Add(element);
                }
            }
            return root;
        }

        /// <summary>BOM か、XML 宣言の encoding で文字列にする（どちらも無ければ UTF-8）。</summary>
        private static string Decode(byte[] data)
        {
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);
            }
            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            }

            Encoding encoding = Encoding.UTF8;
            Match declaration = EncodingPattern.Match(Encoding.ASCII.GetString(data, 0, Math.Min(data.Length, 200)));
            if (declaration.Success)
            {
                try
                {
                    encoding = Encoding.GetEncoding(declaration.Groups[1].Value);
                }
                catch (ArgumentException)
                {
                    // 知らない文字コードなら UTF-8 で読む
                }
            }
            return encoding.GetString(data);
        }
    }
}
