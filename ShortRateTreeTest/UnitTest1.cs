using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using ShortRateTree;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        /// <summary>
        /// small 
        /// </summary>
        [TestMethod]
        public void TestMethod1()
        {
            int sepNum = 10;
            double dt = 0.1;
            double r = 0.01;
            double[] times = Enumerable.Range(0, sepNum).Select(x => x*dt).ToArray();
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
            double[] a = times.Select(x => 0.005).ToArray();
            double[] sigma = times.Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.OutputCsvTreeBackBones("TestMethod1A.csv");
            tree.SetUpTreeNodes();
            tree.OutputCsvTreeNodes("TestMethod1B.csv");
            for (int i = 0; i < times.Length-1; ++i)
            {
                tree.FitToInputBondPrice(i, bondPrices[i]);
            }
            tree.OutputCsvTreeBackBones("TestMethod1C.csv");
            tree.OutputCsvTreeNodes("TestMethod1D.csv");
        }
        /// <summary>
        /// variable dt
        /// </summary>
        [TestMethod]
        public void TestMethod2()
        {
            double[] times = { 0, 0.01, 0.1, 0.11, 0.2, 1.0, 10.0, 10.1, 11.0, 20.0, 100, 101};
            double r = 0.01;
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray(); 
            double[] a = Enumerable.Range(0, times.Length-1).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, times.Length-1).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.OutputCsvTreeBackBones("TestMethod2A.csv");
            tree.SetUpTreeNodes();
            tree.OutputCsvTreeBackBones("TestMethod2B.csv");
            for (int i = 0; i < times.Length-1; ++i)
            {
                tree.FitToInputBondPrice(i, bondPrices[i+1]);
            }
            tree.OutputCsvTreeBackBones("TestMethod2C.csv");
            tree.OutputCsvTreeNodes("TestMethod2D.csv");
        }
        /// <summary>
        /// many time steps
        /// </summary>
        [TestMethod]
        public void TestMethod3()
        {
            int sepNum = 1001;
            double dt = 0.1;
            double r = 0.01;
            double[] times = Enumerable.Range(0, sepNum+1).Select(x => x*dt).ToArray();
            double[] bondPrices = Enumerable.Range(0, sepNum+1).Select(x => Math.Exp(-r * x * dt)).ToArray();
            double[] a = Enumerable.Range(0, times.Length-1).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, times.Length-1).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.SetUpTreeNodes();
            for (int i = 0; i < times.Length-1; ++i)
            {
                double priceDerivativeAlpha;
                tree.ComputeBondPrice(i, out priceDerivativeAlpha);
            }
        }
        
        [TestMethod]
        public void TestComputeBondPrice1()
        {
            double[] times = { 0, 0.1, 0.2, 0.3, 0.4, 0.5 };
            double r = 0.01;
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
            double[] a = times.Select(x => 0.005).ToArray();
            double[] sigma = times.Select(x => 0.5).ToArray();
            Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.SetUpTreeNodes();
            double deriva;
            /// 時点i=0の価格計算
            tree.ComputeBondPrice(0, out deriva);
            Assert.AreEqual(1D, tree._TreeBackBones[0].bondPrice);
            Assert.AreEqual(bondPrices[0], tree._TreeBackBones[0].bondPrice);
            /// 時点i=1の価格計算
            /// i=0のalphaを設定しておく（解析的にもとまる)
            tree._TreeBackBones[0].alpha = Math.Log(-Math.Log(bondPrices[1]) / times[1]);
            /// i=1の債権価格
            tree.ComputeBondPrice(1, out deriva);
            Assert.AreEqual(bondPrices[1], tree._TreeBackBones[1].bondPrice);
            /// i=1の債権価格 : Fit関数を使う
            tree.FitToInputBondPrice(0, bondPrices[1]);
            Assert.AreEqual(bondPrices[1], tree._TreeBackBones[1].bondPrice);
            /// i=2の債権価格
            tree.FitToInputBondPrice(1, bondPrices[2]);
            Assert.AreEqual(bondPrices[2], tree._TreeBackBones[2].bondPrice);
        }
    }
}
