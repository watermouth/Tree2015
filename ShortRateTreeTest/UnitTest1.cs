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
            int sepNum = 10;
            double[] times = Enumerable.Range(0, sepNum+1).Select(x => (double)x / sepNum).ToArray();
            double[] a = Enumerable.Range(0, sepNum).Select(x => 0.005).ToArray();
            double[] sigma  = Enumerable.Range(0, sepNum).Select(x => 0.5).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
            tree.SetUpBackBones(a, sigma);
            tree.OutputCsvTreeBackBones("TestMethod1.csv");
        }
    }
}
