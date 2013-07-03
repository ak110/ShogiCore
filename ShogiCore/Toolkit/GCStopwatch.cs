using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit {
	/// <summary>
	/// 特定区間のGCカウントを計る為のクラス
	/// </summary>
	public class GCStopwatch {
		int[] startCounts;
		int[] elapsedCounts;

		/// <summary>
		/// 計測中ならtrue
		/// </summary>
		public bool IsRunning { get; private set; }

		/// <summary>
		/// 個数差
		/// </summary>
		public int[] ElapsedCounts {
			get {
				if (IsRunning) {
					int[] elapsed = (int[])elapsedCounts.Clone();
					for (int i = 0; i < startCounts.Length; i++) {
						elapsed[i] += GC.CollectionCount(i) - startCounts[i];
					}
					return elapsed;
				} else {
					return elapsedCounts;
				}
			}
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public GCStopwatch() {
			startCounts = new int[GC.MaxGeneration + 1];
			elapsedCounts = new int[startCounts.Length];
		}

		/// <summary>
		/// 計測開始
		/// </summary>
		public void Start() {
			for (int i = 0; i < startCounts.Length; i++) {
				startCounts[i] = GC.CollectionCount(i);
			}
			IsRunning = true;
		}

		/// <summary>
		/// 計測停止
		/// </summary>
		public void Stop() {
			IsRunning = false;
			for (int i = 0; i < startCounts.Length; i++) {
				elapsedCounts[i] += GC.CollectionCount(i) - startCounts[i];
			}
		}

		/// <summary>
		/// クリアする
		/// </summary>
		public void Reset() {
			Array.Clear(elapsedCounts, 0, elapsedCounts.Length);
		}

		/// <summary>
		/// 「157m/37k/10k」みたいな文字列を返す。
		/// </summary>
		public string ToShortString() {
			StringBuilder str = new StringBuilder();
			int[] elp = ElapsedCounts;
			foreach (int t in elp) {
			    if (0 < str.Length) str.Append('/');
			    if (5 * 1024 * 1024 <= t) { // 閾値は適当
			        str.Append(t / 1024 / 1024);
			        str.Append('m');
			    } else if (5 * 1024 <= t) { // 閾値は適当
			        str.Append(t / 1024);
			        str.Append('k');
			    } else {
			        str.Append(t);
			    }
			}
			return str.ToString();
		}

		/// <summary>
		/// 「157,123,468 / 37,104 / 10,168」みたいな文字列を返す。
		/// </summary>
		public string ToLongString() {
			StringBuilder str = new StringBuilder();
			int[] elp = ElapsedCounts;
			for (int i = 0; i < elp.Length; i++) {
				if (0 < str.Length) str.Append(" / ");
				str.Append(elp[i].ToString("#,##0"));
			}
			return str.ToString();
		}
	}
}
