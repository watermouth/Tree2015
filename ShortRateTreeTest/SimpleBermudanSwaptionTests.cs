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
        /// <summary>
        /// 通常のバミューダンスワップションの設定
        /// </summary>
        [TestMethod]
        public void TestMethod1()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            int exerciseCount = 2;
            int resetCount = 4;
            double swapRate = 0.01;
            double divideInterval = 90;
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
            sbs.OutputCsvExerciseDates("BermudanSwaptionExerciseDates.csv");
            sbs.OutputCsvCashflows("BermudanSwaptionCashflows.csv");
            sbs.OutputCsvTimeIntervals("BermudanSwaptionTimeIntervals.csv");
            sbs.SetTreeTimes();
            double r = 0.01;
            double[] times = sbs.GetTreeTimes();
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
            double[] a = times.Select(x => 0.005).ToArray();
            double[] sigma = times.Select(x => 0.5).ToArray();
            sbs.InitializeTree(a, sigma);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");
        }
        /// <summary>
        /// 複数の権利行使日が1つのリセット日に対してあるもの 
        /// </summary>
        [TestMethod]
        public void TestMethod2()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            int exerciseCount = 4;
            int resetCount = 4;
            double swapRate = 0.01;
            double divideInterval = 60;
            Debug.Assert(resetCount >= exerciseCount);
            DateTime[] exerciseDates = Enumerable.Range(1, exerciseCount).Select(x => baseDate.AddMonths(x * 3)).ToArray();
            DateTime[] resetDates = Enumerable.Range(1, resetCount + 1).Select(x => baseDate.AddMonths(x * 6).AddDays(2)).ToArray();
            double[] divideIntervals = resetDates.Select(x => divideInterval).ToArray();
            List<Cashflow> cashflows = new List<Cashflow>();
            for (int i = 0; i < resetDates.Length - 1; ++i)
            {
                cashflows.Add(new Cashflow(resetDates[i], resetDates[i + 1], swapRate));
            }
            SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
            sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows.ToArray(), divideIntervals);
            sbs.OutputCsvExerciseDates("BermudanSwaptionExerciseDates.csv");
            sbs.OutputCsvCashflows("BermudanSwaptionCashflows.csv");
            sbs.OutputCsvTimeIntervals("BermudanSwaptionTimeIntervals.csv");
            sbs.SetTreeTimes();
            double r = 0.01;
            double[] times = sbs.GetTreeTimes();
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
            double[] a = times.Select(x => 0.005).ToArray();
            double[] sigma = times.Select(x => 0.5).ToArray();
            sbs.InitializeTree(a, sigma);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");
        }
        /// <summary>
        /// 権利行使日とリセット日が一致するもの 
        /// </summary>
        [TestMethod]
        public void TestMethod3()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            int exerciseCount = 2;
            int resetCount = 4;
            double swapRate = 0.01;
            double divideInterval = 90;
            Debug.Assert(resetCount >= exerciseCount);
            DateTime[] exerciseDates = Enumerable.Range(1, exerciseCount).Select(x => baseDate.AddMonths(x * 6)).ToArray();
            DateTime[] resetDates = Enumerable.Range(1, resetCount + 1).Select(x => baseDate.AddMonths(x * 6)).ToArray();
            double[] divideIntervals = resetDates.Select(x => divideInterval).ToArray();
            List<Cashflow> cashflows = new List<Cashflow>();
            for (int i = 0; i < resetDates.Length - 1; ++i)
            {
                cashflows.Add(new Cashflow(resetDates[i], resetDates[i + 1], swapRate));
            }
            SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
            sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows.ToArray(), divideIntervals);
            sbs.OutputCsvExerciseDates("BermudanSwaptionExerciseDates.csv");
            sbs.OutputCsvCashflows("BermudanSwaptionCashflows.csv");
            sbs.OutputCsvTimeIntervals("BermudanSwaptionTimeIntervals.csv");
            sbs.SetTreeTimes();
            double r = 0.01;
            double[] times = sbs.GetTreeTimes();
            double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
            double[] a = times.Select(x => 0.005).ToArray();
            double[] sigma = times.Select(x => 0.5).ToArray();
            sbs.InitializeTree(a, sigma);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");
        }
    }
}
