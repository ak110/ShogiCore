using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShogiCore {
    /// <summary>
    /// プレイヤーの持ち時間
    /// </summary>
    public class PlayerTime {
        /// <summary>
        /// 単位時間[ms]
        /// </summary>
        public int Unit { get; set; }
        /// <summary>
        /// 1手の着手に必ず記録される消費時間（最少消費時間）[ms]
        /// </summary>
        public int LeastPerMove { get; set; }
        /// <summary>
        /// 単位時間未満の時間を切り上げるならtrue
        /// </summary>
        public bool Roundup { get; set; }
        /// <summary>
        /// 持ち時間の初期値[ms]
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 秒読み[ms]
        /// </summary>
        public int Byoyomi { get; set; }
        /// <summary>
        /// 遅延時間[ms]
        /// </summary>
        public int Delay { get; set; }
        /// <summary>
        /// 加算時間[ms]
        /// </summary>
        public int Increment { get; set; }

        /// <summary>
        /// 残り持ち時間[ms]
        /// </summary>
        public int RemainTime { get; set; }

        /// <summary>
        /// 適当に無難な初期値を設定する
        /// </summary>
        public PlayerTime() {
            Unit = 1000;
            //LeastTimePerMove = 0;
            //TimeRoundup = false;
        }

        /// <summary>
        /// CSAプロトコルから設定
        /// </summary>
        public PlayerTime(CSA.CSAGameSummary.Time time) {
            if (string.IsNullOrWhiteSpace(time.Time_Unit))
                Unit = 1000;
            else if (time.Time_Unit.EndsWith("msec"))
                Unit = int.Parse(time.Time_Unit.Substring(0, time.Time_Unit.Length - 4));
            else if (time.Time_Unit.EndsWith("sec"))
                Unit = int.Parse(time.Time_Unit.Substring(0, time.Time_Unit.Length - 3));
            else if (time.Time_Unit.EndsWith("min"))
                Unit = int.Parse(time.Time_Unit.Substring(0, time.Time_Unit.Length - 3));
            LeastPerMove = time.Least_Time_Per_Move * Unit;
            Roundup = time.Time_Roundup;
            Total = time.Total_Time * Unit;
            Byoyomi = time.Byoyomi * Unit;
            Delay = time.Delay * Unit;
            Increment = time.Increment * Unit;
            Reset();
        }

        /// <summary>
        /// 持ち時間の一括設定
        /// </summary>
        public void Set(int total, int byoyomi, int inc) {
            Total = total;
            Byoyomi = byoyomi;
            Increment = inc;
            Reset();
        }

        /// <summary>
        /// リセット
        /// </summary>
        public void Reset() {
            RemainTime = Total;
        }

        /// <summary>
        /// 時間を使用する。足りない場合はfalseを返す。
        /// </summary>
        /// <param name="time">消費時間</param>
        public bool Consume(int time) {
            return Consume(ref time);
        }

        /// <summary>
        /// 時間を使用する。足りない場合はfalseを返す。
        /// </summary>
        /// <param name="time">消費時間</param>
        public bool Consume(ref int time) {
            time -= Delay;
            if (time <= LeastPerMove)
                time = LeastPerMove;
            else if (Roundup)
                time += time % Unit == 0 ? 0 : Unit - time % Unit;
            else
                time -= time % Unit;

            if (RemainTime + Byoyomi <= time)
                return false;

            RemainTime = Math.Max(0, RemainTime - time);
            RemainTime += Increment;
            return true;
        }
    }
}
