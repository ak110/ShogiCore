
//#define USE_XMLSERIALIZER // ←どうもXPとかでバグい。標準入出力絡み？

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace ShogiCore.USI {
    /// <summary>
    /// シリアライズ可能なDictionary。
    /// </summary>
    public class USIOptions : Dictionary<string, string>, IXmlSerializable, ICloneable {
        const string RootTagName = "USIOptions";
        const string ItemTagName = "value";
        const string KeyAttrName = "name";

        #region IXmlSerializable メンバ

        public System.Xml.Schema.XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader reader) {
            Clear();
            reader.ReadStartElement(RootTagName);
            while (reader.Read()) {
                while (reader.NodeType == XmlNodeType.Element) {
                    // データをセット
                    string name = reader.LocalName;
                    string key = reader.GetAttribute(KeyAttrName);
                    Add(key, reader.ReadElementString(name));
                }
                // </USIOptions>にたどり着いたら終わり
                if (reader.NodeType == XmlNodeType.EndElement &&
                    reader.LocalName == RootTagName) {
                    reader.ReadEndElement();
                    break;
                }
            }
        }

        public void WriteXml(XmlWriter writer) {
            writer.WriteStartElement(RootTagName);

            foreach (KeyValuePair<string, string> p in this) {
                if (string.IsNullOrEmpty(p.Key) || string.IsNullOrEmpty(p.Value)) continue;
                //if (p.Key.StartsWith("USI_", StringComparison.Ordinal)) continue; // "USI_"で始まるのは無視。

                writer.WriteStartElement(ItemTagName);
                writer.WriteAttributeString("name", p.Key);
                writer.WriteString(p.Value);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion

        /// <summary>
        /// 保存
        /// </summary>
        public void Serialize(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return;
#if USE_XMLSERIALIZER
            using (FileStream stream = File.Create(fileName)) {
                new XmlSerializer(this.GetType()).Serialize(stream, this);
            }
#else
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.OmitXmlDeclaration = false;
            using (FileStream stream = File.Create(fileName))
            using (XmlWriter writer = XmlWriter.Create(stream, settings)) {
                writer.WriteStartDocument();
                writer.WriteStartElement(GetType().Name);
                WriteXml(writer);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
#endif
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public static USIOptions Deserialize(string fileName) {
            if (string.IsNullOrEmpty(fileName) ||
                !File.Exists(fileName) ||
                new FileInfo(fileName).Length <= 0) {
                return new USIOptions();
            }
#if USE_XMLSERIALIZER
            using (FileStream stream = File.OpenRead(fileName)) {
                return (USIOptions)new XmlSerializer(typeof(USIOptions)).Deserialize(stream);
            }
#else
            using (FileStream stream = File.OpenRead(fileName))
            using (XmlReader reader = XmlReader.Create(stream)) {
                USIOptions options = new USIOptions();

                reader.ReadStartElement(options.GetType().Name);
                options.ReadXml(reader);
                reader.ReadEndElement();

                return options;
            }
#endif
        }

        #region ICloneable メンバ

        object ICloneable.Clone() {
            return Clone();
        }

        #endregion

        /// <summary>
        /// 複製の作成
        /// </summary>
        public USIOptions Clone() {
            var copy = new USIOptions();
            foreach (var p in this) {
                copy.Add(p.Key, p.Value);
            }
            return copy;
        }
    }
}
