using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Toolkit {
	/// <summary>
	/// boost::progress_displayのパクり。
	/// </summary>
	public class ProgressDisplay {
		TextWriter m_os;  // may not be present in all imps
		readonly string m_s1;  // string is more general, safer than 
		readonly string m_s2;  //  const char *, and efficiency or size are
		readonly string m_s3;  //  not issues
		int _next_tic_count;
		int _tic;

		/// <summary>
		/// 初期化
		/// </summary>
		public ProgressDisplay(int expected_count)
			: this(expected_count, Console.Out) { }
		/// <summary>
		/// 初期化
		/// </summary>
		public ProgressDisplay(int expected_count, TextWriter os)
			: this(expected_count, os, Environment.NewLine, "", "") { }
		/// <summary>
		/// 初期化
		/// </summary>
		public ProgressDisplay(int expected_count, TextWriter os, string s1, string s2, string s3) {
			m_os = os;
			m_s1 = s1;
			m_s2 = s2;
			m_s3 = s3;
			Restart(expected_count);
		}

		/// <summary>
		/// restart
		/// </summary>
		public void Restart(int expected_count) {
			//  Effects: display appropriate scale
			//  Postconditions: count()==0, expected_count()==expected_count
			Count = _next_tic_count = _tic = 0;
			ExpectedCount = expected_count;

			m_os.Write(m_s1);
			m_os.WriteLine("0%   10   20   30   40   50   60   70   80   90   100%");
			m_os.Write(m_s2);
			m_os.WriteLine("|----|----|----|----|----|----|----|----|----|----|");
			m_os.Write(m_s3);
			if (ExpectedCount == 0) ExpectedCount = 1;  // prevent divide by zero
		}

		/// <summary>
		/// operator+=
		/// </summary>
		/// <param name="increment">加算する値</param>
		/// <remarks>加算後の値</remarks>
		public int Add(int increment) {
			//  Effects: Display appropriate progress tic if needed.
			//  Postconditions: count()== original count() + increment
			//  Returns: count().
			if ((Count += increment) >= _next_tic_count) { display_tic(); }
			return Count;
		}
		/// <summary>
		/// 内部処理。
		/// </summary>
		private void display_tic() {
			// use of floating point ensures that both large and small counts
			// work correctly.  static_cast<>() is also used several places
			// to suppress spurious compiler warnings. 
			int tics_needed =
				(int)(((double)Count / ExpectedCount) * 50.0);
			do { m_os.Write('*'); m_os.Flush(); } while (++_tic < tics_needed);
			_next_tic_count = (int)((_tic / 50.0) * ExpectedCount);
			if (Count == ExpectedCount) {
				if (_tic < 51) m_os.Write('*');
				m_os.WriteLine();
			}
		}

		/// <summary>
		/// operator++
		/// </summary>
		public void Increment() { Add(1); }

	    /// <summary>
	    /// count()
	    /// </summary>
	    public int Count { get; private set; }

	    /// <summary>
	    /// expected_count()
	    /// </summary>
	    public int ExpectedCount { get; private set; }
	}
}
