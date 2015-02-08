using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using ShortRateTree;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            int sepNum = 11;
            double dt = 0.1;
            double[] times = Enumerable.Range(0, sepNum+1).Select(x => x*dt).ToArray();
            double[] a = Enumerable.Range(0, times.Length-1).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, times.Length-1).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.OutputCsvTreeBackBones("TestMethod1A.csv");
            tree.SetUpTreeNodes();
            tree.OutputCsvTreeNodes("TestMethod1B.csv");
        }
        /// <summary>
        /// variable dt
        /// </summary>
        [TestMethod]
        public void TestMethod2()
        {
            double[] times = { 0, 0.01, 0.1, 0.11, 0.2, 1.0, 10.0, 10.1, 11.0, 20.0, 100, 101};
            double[] a = Enumerable.Range(0, times.Length-1).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, times.Length-1).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.OutputCsvTreeBackBones("TestMethod2A.csv");
            tree.SetUpTreeNodes();
            tree.OutputCsvTreeBackBones("TestMethod2B.csv");
            for (int i = 0; i < times.Length-1; ++i)
            {
                tree.ComputeBondPrice(i);
            }
            tree.OutputCsvTreeNodes("TestMethod2C.csv");
        }
        /// <summary>
        /// many time steps
        /// </summary>
        [TestMethod]
        public void TestMethod3()
        {
            int sepNum = 1001;
            double dt = 0.1;
            double[] times = Enumerable.Range(0, sepNum+1).Select(x => x*dt).ToArray();
            double[] a = Enumerable.Range(0, times.Length-1).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, times.Length-1).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.InitializeBackBones(a, sigma);
            tree.SetUpTreeNodes();
            for (int i = 0; i < times.Length-1; ++i)
            {
                tree.ComputeBondPrice(i);
            }
        }
    }
}
