using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Toolkit {
	/// <summary>
	/// XmlSerializerでも使えるDictionary。
	/// </summary>
	/// <typeparam name="TKey">キーの型</typeparam>
	/// <typeparam name="TValue">値の型</typeparam>
	[XmlRoot(RootTagName)]
	public class DictionaryForXml<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable {
		const string RootTagName = "Dictionary";

		/// <summary>
		/// KeyValuePairはreadonlyなせいで使えないので…。(´ω`)
		/// </summary>
		public class Pair {
			public TKey Key;
			public TValue Value;
		}
		/// <summary>
		/// serializer
		/// </summary>
		static XmlSerializer serializer = new XmlSerializer(typeof(Pair));

		#region コンストラクタ

		public DictionaryForXml() { }
		public DictionaryForXml(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
		public DictionaryForXml(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public DictionaryForXml(int capacity) : base(capacity) { }
		public DictionaryForXml(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
			: base(dictionary, comparer) { }
		public DictionaryForXml(int capacity, IEqualityComparer<TKey> comparer)
			: base(capacity, comparer) { }
		protected DictionaryForXml(SerializationInfo info, StreamingContext context)
			: base(info, context) { }

		#endregion

		#region IXmlSerializable メンバ

		public System.Xml.Schema.XmlSchema GetSchema() {
			return null;
		}

		public void ReadXml(System.Xml.XmlReader reader) {
			bool wasEmpty = reader.IsEmptyElement;
			if (!reader.Read()) return;
			if (wasEmpty) return;
			while (reader.NodeType != System.Xml.XmlNodeType.None &&
				reader.NodeType != System.Xml.XmlNodeType.EndElement) {
				var p = (Pair)serializer.Deserialize(reader);
				this[p.Key] = p.Value;
				reader.MoveToContent();
			}
			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer) {
			foreach (var p in this) {
				serializer.Serialize(writer, new Pair() { Key = p.Key, Value = p.Value });
			}
		}

		#endregion
	}
}
