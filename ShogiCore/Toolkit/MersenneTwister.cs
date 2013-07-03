using System;
using System.Collections.Generic;
using System.Text;

namespace Toolkit {
	/// <summary>
	/// MersenneTwister。
	/// </summary>
	public sealed class MersenneTwister : Random {
		const int N = 624;
		readonly uint[] mt = new uint[N];
		int mti;

		/// <summary>
		/// ランダムに初期化済みのインスタンスを作成して返す
		/// </summary>
		/// <returns>初期化済みインスタンス</returns>
		public static MersenneTwister CreateRandom() {
			byte[] data = new byte[0x100]; // ←適当
			new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(data);
			return new MersenneTwister(data);
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public MersenneTwister() : this(Environment.TickCount) {}

		/// <summary>
		/// 初期化
		/// </summary>
		public MersenneTwister(int seed) {
			unchecked {
				uint s = (uint)seed;
				mt[0] = s;
				for (int i = 1; i < N; i++) {
					mt[i] = (uint)(1812433253u * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i);
				}
				mti = 0;
			}
		}
		
		/// <summary>
		/// 初期化
		/// </summary>
		public MersenneTwister(byte[] init_key) : this(BytesToUInts(init_key)) { }

		/// <summary>
		/// byte[]をuint[]に適当変換。
		/// </summary>
		private static uint[] BytesToUInts(byte[] init_key) {
			uint[] data = new uint[(init_key.Length + 3) / 4];
			// memcpy的にコピる
			unsafe {
				fixed (uint* p = data) {
					System.Runtime.InteropServices.Marshal.Copy(
						init_key, 0, (IntPtr)p, init_key.Length);
				}
			}
			return data;
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public MersenneTwister(uint[] init_key) : this(19650218) {
			unchecked {
				int i = 1;
				int j = 0;
				int k = init_key.Length < N ? N : init_key.Length;
				for (; k != 0; k--) {
					mt[i] = (uint)((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1664525u)) + init_key[j] + j); // non linear
					i++;
					j++;
					if (N <= i) { mt[0] = mt[N - 1]; i = 1; }
					if (init_key.Length <= j) j = 0;
				}
				for (k = N - 1; k != 0; k--) {
					mt[i] = (uint)((mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1566083941u)) - i); // non linear
					i++;
					if (N <= i) { mt[0] = mt[N - 1]; i = 1; }
				}
			}
		}

		/// <summary>
		/// 0 以上の乱数を返します。
		/// </summary>
		public override int Next() {
			return Next(int.MaxValue);
		}
		/// <summary>
		/// 指定した最大値より小さい 0 以上の乱数を返します。
		/// </summary>
		public override int Next(int maxValue) {
			if (maxValue <= 1) {
				if (maxValue < 0) {
					throw new ArgumentOutOfRangeException();
				} else {
					return 0;
				}
			}
			unchecked {
				return (int)((ulong)NextUInt() * (uint)maxValue / ((ulong)uint.MaxValue + 1));
			}
		}
		/// <summary>
		/// 指定した範囲内の乱数
		/// </summary>
		/// <param name="minValue">返される乱数の包括的下限値</param>
		/// <param name="maxValue">返される乱数の排他的上限値。maxValue は minValue 以上にする必要があります</param>
		/// <returns></returns>
		public override int Next(int minValue, int maxValue) {
			if (maxValue < minValue) {
				throw new ArgumentOutOfRangeException();
			} else if (maxValue == minValue) {
				return minValue;
			} else {
				return Next(maxValue - minValue) + minValue;
			}
		}
		/// <summary>
		/// 指定したバイト配列の要素に乱数を格納
		/// </summary>
		public override void NextBytes(byte[] buffer) {
			if (buffer == null) {
				throw new ArgumentNullException();
			}

			for (int i = 0; i < buffer.Length; i += 4) {
				uint n = NextUInt();
				buffer[i] = (byte)(n & 0xff);
				buffer[i + 1] = (byte)((n >> 8) & 0xff);
				buffer[i + 2] = (byte)((n >> 16) & 0xff);
				buffer[i + 3] = (byte)((n >> 24) & 0xff);
			}
			for (int i = buffer.Length - buffer.Length % 4; i < buffer.Length; i++) {
				buffer[i] = (byte)(NextUInt() & 0xff);
			}
		}
		/// <summary>
		/// 0.0 と 1.0 の間の乱数
		/// </summary>
		public override double NextDouble() {
			return (double)NextUInt() / ((ulong)uint.MaxValue + 1);
		}

		/// <summary>
		/// 32bit乱数の生成
		/// </summary>
		public uint NextUInt() {
			unchecked {
				lock (mt) {
					if (mti <= 0) {
						mti = N - 1;
						GenerateWords();
					} else {
						mti--;
					}
					uint y = mt[mti];
					// Tempering
					y ^= (y >> 11);
					y ^= (y << 7) & 0x9d2c5680u;
					y ^= (y << 15) & 0xefc60000u;
					y ^= (y >> 18);
					return y;
				}
			}
		}

		/// <summary>
		/// 再生成
		/// </summary>
		private unsafe void GenerateWords() {
			uint* mag01 = stackalloc uint[2];
			mag01[0] = 0x0;
			mag01[1] = 0x9908b0dfu; //mag01[x] = x * MATRIX_A  for x=0, 1

			uint kk = 0;
			for (; kk < (N - 397); kk++) {
				uint y = (mt[kk] & 0x80000000) | (mt[kk + 1] & 0x7fffffff);
				mt[kk] = mt[kk + 397] ^ (y >> 1) ^ mag01[y & 0x1];
			}
			for (; kk < (N - 1); kk++) {
				uint y = (mt[kk] & 0x80000000) | (mt[kk + 1] & 0x7fffffff);
				mt[kk] = mt[kk + (397 - N)] ^ (y >> 1) ^ mag01[y & 0x1];
			}
			{
				uint y = (mt[N - 1] & 0x80000000) | (mt[0] & 0x7fffffff);
				mt[N - 1] = mt[397 - 1] ^ (y >> 1) ^ mag01[y & 0x1];
			}
		}
	}
}
