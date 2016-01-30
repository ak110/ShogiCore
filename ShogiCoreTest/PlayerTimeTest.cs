﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ShogiCore {
    [TestClass]
    public class PlayerTimeTest {
        /// <summary>
        /// PlayerTime.Consumeのテスト
        /// </summary>
        [TestMethod]
        public void ConsumeTest1() {
            // 秒読み1秒
            ConsumeTestImpl(0, 1000, 0);
            // 秒読み2秒
            ConsumeTestImpl(0, 2000, 0);
            // 持ち時間1秒秒読み2秒
            ConsumeTestImpl(1000, 2000, 0);
            // 持ち時間2秒秒読み0秒
            ConsumeTestImpl(2000, 0, 1000);
            // 持ち時間2秒秒読み2秒
            ConsumeTestImpl(2000, 2000, 0);
            // 秒読み0.1秒(厳守出来るとは限らないため0.999秒までセーフにしてしまう)
            ConsumeTestImpl2(0, 100, 0, 1000);
        }

        private void ConsumeTestImpl(int time, int byoyomi, int nextTime) {
            int ngTime = time + byoyomi;
            ConsumeTestImpl2(time, byoyomi, nextTime, ngTime);
        }

        private void ConsumeTestImpl2(int time, int byoyomi, int nextTime, int ngTime) {
            var playerTime = new PlayerTime();
            playerTime.Total = time;
            playerTime.Byoyomi = byoyomi;
            playerTime.Delay = ngTime < 1000 ? 1000 - ngTime : 0;
            playerTime.Reset();

            Assert.IsFalse(playerTime.Consume(ngTime));
            Assert.IsTrue(playerTime.Consume(ngTime - 1));
            Assert.AreEqual(nextTime, playerTime.Remain);
        }

        /// <summary>
        /// PlayerTime.Consumeのテスト
        /// </summary>
        [TestMethod]
        public void ConsumeTest2() {
            var playerTime = new PlayerTime();
            playerTime.Unit = 500;
            playerTime.Remain = 1000;
            playerTime.Byoyomi = 500;
            playerTime.Increment = 1234;
            // 持ち時間+秒読みを超えるとNG
            Assert.IsFalse(playerTime.Consume(1500));
            // ぎりぎりOK
            Assert.IsTrue(playerTime.Consume(1499));
            // 持ち時間が尽きたけどIncrementの分増える
            Assert.AreEqual(playerTime.Remain, 1234);
            // TimeUnit未満は消費無し
            Assert.IsTrue(playerTime.Consume(499));
            Assert.AreEqual(playerTime.Remain, 1234 * 2);
        }

        [TestMethod]
        public void ToStringTest() {
            var playerTime = new PlayerTime();
            playerTime.Unit = 500;
            playerTime.Total= 10000;
            playerTime.Remain = 1000;
            playerTime.Byoyomi = 500;
            playerTime.Increment = 1234;
            playerTime.Delay = 100;
            Assert.AreEqual("単位=0.5,持ち=10,残り=1,秒読み=0.5,加算=1.234,遅延=0.1", playerTime.ToString());
        }

        [TestMethod]
        public void GetFixedTimeTest() {
            var playerTime = new PlayerTime();
            playerTime.Unit = 333;
            playerTime.LeastPerMove = 0;
            playerTime.Roundup = false;
            Assert.AreEqual(0, playerTime.GetFixedTime(332));
            Assert.AreEqual(333, playerTime.GetFixedTime(333));
            Assert.AreEqual(333, playerTime.GetFixedTime(334));
            playerTime.Roundup = true;
            Assert.AreEqual(333, playerTime.GetFixedTime(332));
            Assert.AreEqual(333, playerTime.GetFixedTime(333));
            Assert.AreEqual(666, playerTime.GetFixedTime(334));
            playerTime.Roundup = false;
            playerTime.LeastPerMove = 333;
            Assert.AreEqual(333, playerTime.GetFixedTime(332));
            Assert.AreEqual(333, playerTime.GetFixedTime(333));
            Assert.AreEqual(333, playerTime.GetFixedTime(334));
        }
    }
}
