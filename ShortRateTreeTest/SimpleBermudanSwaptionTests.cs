using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using ShortRateTree;
using System.Diagnostics;

namespace ShortRateTreeTest
{
    [TestClass]
    public class SimpleBermudanSwaptionTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            int exerciseCount = 2;
            int resetCount = 4;
            double swapRate = 0.01;
            double divideInterval = 60;
            Debug.Assert(resetCount >= exerciseCount);
            DateTime[] exerciseDates = Enumerable.Range(1, exerciseCount).Select(x => baseDate.AddMonths(x * 6)).ToArray();
            DateTime[] resetDates = Enumerable.Range(1, resetCount + 1).Select(x => baseDate.AddMonths(x * 6).AddDays(2)).ToArray();
            double[] divideIntervals = resetDates.Select(x => divideInterval).ToArray(); 
            List<Cashflow> cashflows = new List<Cashflow>();
            for (int i = 0; i < resetDates.Length - 1; ++i)
            {
                cashflows.Add(new Cashflow(resetDates[i], resetDates[i + 1], swapRate));
            }
            SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
            sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows.ToArray(), divideIntervals);
            sbs.OutputCsvCashflows("BermudanSwaptionCashflows.csv");
            sbs.OutputCsvTimeIntervals("BermudanSwaptionTimeIntervals.csv");
        }
    }
}
