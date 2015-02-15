using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShortRateTree;

namespace ShortRateTreeTest
{
    [TestClass]
    public class TimeIntervalTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            DateTime resetDate = baseDate.AddMonths(6);
            DateTime settleDate = baseDate.AddMonths(12);
            TimeInterval t = new TimeInterval();
            t.DivideTimeInterval(baseDate, resetDate, settleDate, 1 * 28);
            Console.WriteLine(t.ToStringValues());
            Assert.IsTrue(t.DivideTimeInterval(baseDate, resetDate, settleDate, 1));
            Assert.IsTrue(!t.DivideTimeInterval(baseDate, resetDate, resetDate, 1));
        }
    }
}
