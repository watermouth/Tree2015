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
            double[] times = Enumerable.Range(0, sepNum).Select(x => (double)x / sepNum).ToArray();
            ShortRateTree.Tree tree = new Tree(times);
        }
    }
}
