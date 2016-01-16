using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using ShogiCore.Notation;

namespace ShogiCore.USI {
    /// <summary>
    /// USIエンジンを呼びだすプログラム。
    /// </summary>
    /// <remarks>
    /// いい加減に書くと、
    /// GUIなど → USIDriver → BlunderUSI.exe → USIClient → 思考エンジン
    /// という感じ。
    /// http://www.geocities.jp/shogidokoro/usi.html
    /// </remarks>
    public class USIDriver : System.Runtime.ConstrainedExecution.CriticalFinalizerObject, IDisposable {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static readonly log4net.ILog usiLogger = log4net.LogManager.GetLogger("USILogger");

        string engineFileName, engineArguments;

        object syncObject = new object();
        Process process = null;
        int processExitCode;
        Queue<USICommand> commandQueue = new Queue<USICommand>();

        string logID;
        string sendUSILogPrefix, recvUSILogPrefix, errUSILogPrefix;
        /// <summary>
        /// ログ記録用のエンジンのID
        /// </summary>
        public int LogID {
            set {
                logID = value.ToString();
                sendUSILogPrefix = logID + ">:";
                recvUSILogPrefix = logID + "<:";
                errUSILogPrefix = logID + "E:";
            }
        }

        /// <summary>
        /// optionコマンド1つに対応するデータ。継承とかは面倒なので適当に。
        /// </summary>
        public class OptionEntry {
            /// <summary>
            /// 名前
            /// </summary>
            public string Name;
            /// <summary>
            /// 種類。
            /// </summary>
            /// <remarks>
            /// check    チェックボックスを表示します。
            /// spin    数値のみを入力できるコントロールを表示します。
            /// combo    ポップアップメニューを表示します。
            /// button    ボタンを表示します。このボタンを押すと、オプション名で指定されたコマンドがエンジンに送られます。（オプション名がoptionnameなら、このボタンを押すと、setoption name optionnameというコマンドが送られます。）
            /// string    文字列を入力できるコントロールを表示します。
            /// filename    ファイル名を入力できるコントロールを表示します。ファイル名を指定しやすいよう、ファイル選択ボタンを表示し、それを押すと、ファイル選択のダイアログを表示するようになっています。ここで選択したファイルの絶対パスが設定値となります。
            /// </remarks>
            public string Type;
            /// <summary>
            /// 既定値
            /// </summary>
            public string Default;
            /// <summary>
            /// 最小値、最大値 (spin用)
            /// </summary>
            public string Min, Max;
            /// <summary>
            /// 選択できる文字列 (combo用)
            /// </summary>
            public List<string> Var = new List<string>();

            readonly static Regex regex1 = new Regex(@"\s*name\s+(.+)\s+type\s+([a-z]+)\s*(.*)", RegexOptions.Compiled);
            readonly static Regex regex2 = new Regex(@"\s*(min|max|var|default)\s+", RegexOptions.Compiled);

            /// <summary>
            /// パラメータの解析
            /// </summary>
            public static OptionEntry Parse(string str) {
                OptionEntry option = new OptionEntry();
                Match m = regex1.Match(str);
                if (m.Success) {
                    option.Name = m.Groups[1].Value;
                    option.Type = m.Groups[2].Value;
                    string[] ar = regex2.Split(m.Groups[3].Value).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    for (int i = 0; i < ar.Length / 2; i++) {
                        switch (ar[i * 2 + 0]) {
                            case "min": option.Min = ar[i * 2 + 1]; break;
                            case "max": option.Max = ar[i * 2 + 1]; break;
                            case "var": option.Var.Add(ar[i * 2 + 1]); break;
                            case "default": option.Default = ar[i * 2 + 1]; break;
                        }
                    }
                }
                return option;
            }
        }

        /// <summary>
        /// ゲーム中ならtrueなフラグ。
        /// </summary>
        /// <remarks>
        /// usinewgame送信後にtrueになり、gameover送信後にfalseになる。
        /// </remarks>
        public bool GameStarted { get; private set; }
        /// <summary>
        /// 受信したid nameコマンドの値
        /// </summary>
        public string IdName { get; private set; }
        /// <summary>
        /// 受信したid authorコマンドの値
        /// </summary>
        public string IdAuthor { get; private set; }
        /// <summary>
        /// 受信したoptionコマンド
        /// </summary>
        public List<OptionEntry> Options { get; private set; }

        object goingLock = new object();
        /// <summary>
        /// goしてbestmoveが帰ってくる前ならtrue
        /// </summary>
        public bool Going { get; private set; }

        #region 各種イベント

        /// <summary>
        /// メッセージ受信
        /// </summary>
        public event EventHandler<USIEventArgs> MessageReceived;
        /// <summary>
        /// メッセージ送信
        /// </summary>
        public event EventHandler<USIEventArgs> MessageSent;

        /// <summary>
        /// コマンド受信
        /// </summary>
        public event EventHandler<USICommandEventArgs> CommandReceived;
        /// <summary>
        /// infoコマンド受信
        /// </summary>
        public event EventHandler<USIInfoEventArgs> InfoReceived;

        #endregion

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="engineFileName">USIエンジンのファイル名(環境変数利用可能)</param>
        /// <param name="engineArguments">USIエンジンの引数。nullで無し。将棋所が未対応なのでnullが無難？</param>
        /// <param name="logID">ログ記録用のエンジンのID</param>
        public USIDriver(string engineFileName, string engineArguments = null, int logID = 1) {
            Options = new List<OptionEntry>();
            this.engineFileName = engineFileName;
            this.engineArguments = engineArguments;
            LogID = logID;
        }

        /// <summary>
        /// エンジンの起動
        /// </summary>
        /// <returns>true:起動した、false:起動済み</returns>
        public bool Start(ProcessPriorityClass pricessPriorityClass = ProcessPriorityClass.Normal) {
            try {
                if (process != null)
                    return false;
                process = new Process();

                engineFileName = NormalizeEnginePath(engineFileName);

                process.StartInfo.FileName = engineFileName;
                if (!string.IsNullOrEmpty(engineArguments)) {
                    process.StartInfo.Arguments = engineArguments;
                }
                try {
                    string workDir = Path.GetDirectoryName(engineFileName);
                    if (Directory.Exists(workDir)) {
                        process.StartInfo.WorkingDirectory = workDir;
                    }
                } catch (ArgumentException) {
                }
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += process_OutputDataReceived;
                process.ErrorDataReceived += process_ErrorDataReceived;
                process.Exited += process_Exited;
                if (!process.Start()) {
                    throw new ApplicationException("USIエンジンの起動に失敗: " + engineFileName + " " + engineArguments);
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                try {
                    process.ProcessorAffinity = Process.GetCurrentProcess().ProcessorAffinity; // プロセッサAffinityを引き継ぐようにしてみる
                } catch (Exception e) {
                    logger.Warn("ProcessorAffinityの設定に失敗", e);
                }
                try {
                    process.PriorityClass = pricessPriorityClass;
                } catch (Exception e) {
                    logger.Warn("PriorityClassの設定に失敗", e);
                }

                Send("usi");
                // usiokまで待つ。(とりあえず適当に最大30秒)
                if (!WaitFor(30000, command => {
                    switch (command.Name) {
                        case "usiok": return true;
                        case "id":
                        case "option":
                        case "info":
                            return false; // 無視
                        default:
                            logger.DebugFormat("usiok前に不正なコマンドを受信: {0}", command);
                            return false;
                    }
                })) {
                    throw new ApplicationException("USIエンジンの起動に失敗(usiok受信失敗): " + engineFileName + " " + engineArguments);
                }
                return true;
            } catch (Exception e) {
                logger.Error("USIエンジンの起動に失敗: " + engineFileName + " " + engineArguments, e);
                Dispose();
                throw new ApplicationException("USIエンジンの起動に失敗: " + engineFileName + " " + engineArguments, e);
            }
        }

        /// <summary>
        /// エンジンのパスを整える
        /// </summary>
        public static string NormalizeEnginePath(string engineFileName) {
            if (string.IsNullOrEmpty(engineFileName)) return engineFileName;
            if (engineFileName[0] == '"' && engineFileName[engineFileName.Length - 1] == '"') {
                engineFileName = engineFileName.Substring(1, engineFileName.Length - 2);
            }
            if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar) {
                engineFileName = engineFileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            engineFileName = Environment.ExpandEnvironmentVariables(engineFileName);
            engineFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, engineFileName);
            engineFileName = Path.GetFullPath(engineFileName);
            return engineFileName;
        }

        #region Dispose

        /// <summary>
        /// 後始末
        /// </summary>
        ~USIDriver() {
            Dispose(false);
        }

        /// <summary>
        /// 後始末
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 後始末
        /// </summary>
        private void Dispose(bool disposing) {
            try {
                lock (syncObject) {
                    if (process != null) {
                        Kill();
                        processExitCode = process.ExitCode;
                        process.Dispose();
                        process = null;
                    }
                }
            } catch {
            }
        }

        #endregion

        /// <summary>
        /// 受信
        /// </summary>
        void process_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            string line = (e.Data ?? "").TrimEnd('\r', '\n'); // 念のため末尾に改行っぽいのがあれば削除（適当）

            if (usiLogger.IsDebugEnabled) {
                usiLogger.Debug(recvUSILogPrefix + line);
            }

            if (string.IsNullOrEmpty(line)) return; // 念のため、空なら無視

            var MessageReceived = this.MessageReceived;
            if (MessageReceived != null) {
                MessageReceived(this, new USIEventArgs(line));
            }

            USICommand command = USICommand.Parse(line);

            switch (command.Name) {
                case "id": {
                        if (command.Parameters.StartsWith("name ", StringComparison.Ordinal)) {
                            IdName = command.Parameters.Substring(5);
                        } else if (command.Parameters.StartsWith("author ", StringComparison.Ordinal)) {
                            IdAuthor = command.Parameters.Substring(7);
                        }
                    } break;
                case "option": {
                        Options.Add(OptionEntry.Parse(command.Parameters));
                    } break;
                case "info": try {
                        USIInfoEventArgs infoEventArgs = new USIInfoEventArgs(
                            USIInfo.Parse(command.Parameters));
                        var InfoReceived = this.InfoReceived;
                        if (InfoReceived != null) {
                            InfoReceived(this, infoEventArgs);
                        }
                    } catch (Exception ex) {
                        logger.Warn("infoコマンド処理中に例外発生: " + command.Parameters, ex);
                    } break;
                case "bestmove":
                    lock (goingLock) {
                        Going = false;
                    }
                    break;
            }

            USICommandEventArgs commandEventArgs = new USICommandEventArgs(command);
            var CommandReceived = this.CommandReceived;
            if (CommandReceived != null) {
                CommandReceived(this, commandEventArgs);
            }

            if (!commandEventArgs.Handled) { // イベント側で処理されてたらキューへは入れない
                // キューへ追加
                lock (commandQueue) {
                    commandQueue.Enqueue(command);
                    Monitor.Pulse(commandQueue);
                }
            }
        }

        /// <summary>
        /// 標準エラー
        /// </summary>
        void process_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            if (usiLogger.IsInfoEnabled) {
                string line = (e.Data ?? "").TrimEnd('\r', '\n'); // 念のため末尾に改行っぽいのがあれば削除（適当）
                usiLogger.Info(errUSILogPrefix + line);
            }
        }

        /// <summary>
        /// エンジンを止める
        /// </summary>
        public void Kill() {
            if (process == null)
                return;
            try {
                if (!process.HasExited) {
                    SendQuit();
                    process.WaitForExit(5000);
                }
            } catch (IOException) { // 黙殺。
            } catch (InvalidOperationException) { // 黙殺。
            } catch (NullReferenceException) { // 黙殺。
            }
            try {
                if (!process.HasExited) {
                    process.Kill();
                }
            } catch (InvalidOperationException) { // 黙殺。
            } catch (NullReferenceException) { // 黙殺。
            }
        }

        /// <summary>
        /// プロセス終了
        /// </summary>
        void process_Exited(object sender, EventArgs e) {
            if (usiLogger.IsDebugEnabled) {
                int exitCode;
                try {
                    var process = this.process;
                    exitCode = process == null ? processExitCode : process.ExitCode;
                } catch {
                    exitCode = processExitCode;
                }
                usiLogger.DebugFormat("{0}X: プロセス終了(ExitCode={1})", logID, exitCode.ToString());
            }
            lock (commandQueue) {
                Monitor.Pulse(commandQueue);
            }
        }

        /// <summary>
        /// コマンド受信処理
        /// </summary>
        /// <param name="command">コマンド</param>
        /// <returns>続行時false、中断時true</returns>
        public delegate bool WaitForDelegate(USICommand command);
        /// <summary>
        /// コマンドの受信
        /// </summary>
        /// <param name="timeout">最大待ち時間[ms]</param>
        /// <returns>タイムアウト時false、指定コマンド受信時true</returns>
        public bool WaitFor(int timeout, WaitForDelegate waitFor) {
            int startTick = Environment.TickCount;
            int waited = 0;
            while (true) {
                USICommand command;
                if (TryReceiveCommand(timeout - waited, out command)) {
                    // 指定コマンド受信
                    if (waitFor(command)) {
                        return true;
                    }
                }
                waited = unchecked(Environment.TickCount - startTick);
                // 時間切れ
                if (timeout <= waited) break;
            }
            return false;
        }

        /// <summary>
        /// isreadyを送ってreadyokを待つ。Start()したあとSetOptionするならしたあとコレをやれば準備完了。
        /// </summary>
        /// <param name="timeout">最大待ち時間[ms]</param>
        /// <returns>成功時true、失敗時false</returns>
        public bool WaitForReadyOK(int timeout) {
            SendIsReady();
            while (true) {
                USICommand command;
                if (TryReceiveCommand(timeout, out command)) {
                    if (command.Name == "readyok") {
                        return true; // 成功
                    } else if (command.Name == "info") {
                        // 無視
                    } else {
                        logger.DebugFormat("readyok前に不正なコマンドを受信: {0}", command);
                    }
                } else {
                    return false; // 失敗
                }
            }
        }

        /// <summary>
        /// コマンドの受信
        /// </summary>
        /// <param name="timeout">最大待ち時間[ms]</param>
        /// <returns>タイムアウト時false、受信時true</returns>
        public bool TryReceiveCommand(int timeout, out USICommand command) {
            lock (commandQueue) {
                while (commandQueue.Count <= 0) {
                    if (process.HasExited || !Monitor.Wait(commandQueue, timeout)) {
                        command = USICommand.Empty;
                        return false;
                    }
                }
                command = commandQueue.Dequeue();
                return true;
            }
        }

        /// <summary>
        /// 対局開始前に送ります。エンジンは対局準備ができたらreadyokを返すことになります。
        /// </summary>
        public void SendIsReady() {
            Send("isready");
        }

        /// <summary>
        /// エンジンに対して値を設定する時に送ります。
        /// </summary>
        public void SendSetOption(string name) {
            Send("setoption name " + name);
        }
        /// <summary>
        /// エンジンに対して値を設定する時に送ります。
        /// </summary>
        public void SendSetOption(string name, string value) {
            Send("setoption name " + name + " value " + value);
        }

        /// <summary>
        /// 対局開始時に送ります。これで対局開始になります。
        /// </summary>
        public void SendUSINewGame() {
            Send("usinewgame");
            GameStarted = true;
        }

        /// <summary>
        /// position
        /// </summary>
        public void SendPosition(Notation.Notation notation) {
            Send("position", new SFENNotationWriter().WriteToString(notation).TrimEnd());
        }

        /// <summary>
        /// 思考開始の合図です。エンジンはこれを受信すると思考を開始します。
        /// </summary>
        /// <param name="btime">先手の持ち時間情報</param>
        /// <param name="wtime">後手の持ち時間情報</param>
        /// <param name="colorIsBlack">先手番ならtrue、後手番ならfalse</param>
        /// <param name="byoyomiHack">秒読みを1秒減らしてエンジンへ送信するならtrue</param>
        public void SendGo(PlayerTime btime, PlayerTime wtime, bool colorIsBlack, bool byoyomiHack) {
            var byoyomi = (colorIsBlack ? btime : wtime).Byoyomi;
            var s = new StringBuilder();
            s.Append(" btime ").Append(btime.RemainTime);
            s.Append(" wtime ").Append(wtime.RemainTime);
            s.Append(" byoyomi ").Append(byoyomiHack ? Math.Max(0, byoyomi - 1000) : byoyomi);
            if (btime.Increment != 0 || wtime.Increment != 0) {
                s.Append(" binc ").Append(btime.Increment);
                s.Append(" winc ").Append(wtime.Increment);
            }
            lock (goingLock) {
                Going = true;
                Send("go" + s.ToString());
            }
        }
        /// <summary>
        /// 時間無制限で思考させる場合に使います。
        /// </summary>
        /// <remarks>
        /// エンジンはgo infiniteで思考を開始した場合、次にstopが送られてくる前に
        /// bestmoveを返してはいけません。stopが送られてきた時にbestmoveを返すことになります。
        /// </remarks>
        public void SendGoInfinite() {
            lock (goingLock) {
                Going = true;
                Send("go infinite");
            }
        }
        /// <summary>
        /// 深さ指定で思考させる
        /// </summary>
        /// <param name="depth">深さ</param>
        public void SendGoDepth(int depth) {
            lock (goingLock) {
                Going = true;
                Send("go depth " + depth.ToString());
            }
        }
        /// <summary>
        /// ノード数指定で思考させる
        /// </summary>
        /// <param name="nodes">ノード数</param>
        public void SendGoNodes(long nodes) {
            lock (goingLock) {
                Going = true;
                Send("go nodes " + nodes.ToString());
            }
        }
        /// <summary>
        /// go ponderを送る
        /// </summary>
        public void SendGoPonder() {
            lock (goingLock) {
                Going = true;
                Send("go ponder");
            }
        }

        /// <summary>
        /// ponderhitを送る
        /// </summary>
        public void SendPonderHit() {
            Debug.Assert(Going);
            Send("ponderhit");
        }

        /// <summary>
        /// 詰将棋解答を開始する時に使います。
        /// </summary>
        /// <param name="time">制限時間[ms]</param>
        public void SendGoMate(int time) {
            lock (goingLock) {
                Going = true;
                Send("go mate " + time.ToString());
            }
        }
        /// <summary>
        /// 詰将棋解答を開始する時に使います。
        /// </summary>
        public void SendGoMateInfinite() {
            lock (goingLock) {
                Going = true;
                Send("go mate infinite");
            }
        }

        /// <summary>
        /// エンジンに対し思考停止を命令するコマンドです。
        /// </summary>
        /// <remarks>
        /// エンジンはこれを受信したら、できるだけすぐ思考を中断し、
        /// bestmoveで指し手を返す必要があります。
        /// </remarks>
        public void SendStop() {
            lock (goingLock) {
                if (Going) {
                    Send("stop");
                }
            }
        }

        /// <summary>
        /// アプリケーション終了を命令するコマンドです。
        /// </summary>
        /// <remarks>
        /// エンジンはこれを受信したらすぐに終了する必要があります。
        /// </remarks>
        public void SendQuit() {
            try {
                Send("quit");
            } catch (ObjectDisposedException) {
                // 無視。
            }
        }

        /// <summary>
        /// 対局終了をエンジンに知らせるためのコマンド。
        /// </summary>
        /// <remarks>
        /// エンジンはgameoverを受信したら対局状態を終了して、
        /// 対局待ち状態になります。その後、isready及びusinewgameを
        /// 受信すると次の対局開始ということになります。
        /// </remarks>
        public void SendGameOverWin() {
            Send("gameover win");
            GameStarted = false;
        }
        /// <summary>
        /// 対局終了をエンジンに知らせるためのコマンド。
        /// </summary>
        /// <remarks>
        /// エンジンはgameoverを受信したら対局状態を終了して、
        /// 対局待ち状態になります。その後、isready及びusinewgameを
        /// 受信すると次の対局開始ということになります。
        /// </remarks>
        public void SendGameOverLose() {
            Send("gameover lose");
            GameStarted = false;
        }
        /// <summary>
        /// 対局終了をエンジンに知らせるためのコマンド。
        /// </summary>
        /// <remarks>
        /// エンジンはgameoverを受信したら対局状態を終了して、
        /// 対局待ち状態になります。その後、isready及びusinewgameを
        /// 受信すると次の対局開始ということになります。
        /// </remarks>
        public void SendGameOverDraw() {
            Send("gameover draw");
            GameStarted = false;
        }

        /// <summary>
        /// その他のコマンドの送信
        /// </summary>
        /// <param name="raw">コマンド</param>
        public void Send(params string[] raw) {
            Send(string.Join(" ", raw));
        }

        /// <summary>
        /// その他のコマンドの送信
        /// </summary>
        /// <param name="line">コマンド</param>
        public void Send(string line) {
            WriteLine(line);
            Flush();
        }

        /// <summary>
        /// 行の送信
        /// </summary>
        /// <param name="line">行</param>
        private void WriteLine(string line) {
            var MessageSent = this.MessageSent;
            if (MessageSent != null) {
                MessageSent(this, new USIEventArgs(line));
            }
            if (usiLogger.IsDebugEnabled) {
                usiLogger.Debug(sendUSILogPrefix + line);
            }
            process.StandardInput.WriteLine(line);
        }

        /// <summary>
        /// flush
        /// </summary>
        private void Flush() {
            process.StandardInput.Flush();
        }
    }
}
