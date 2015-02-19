using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using ShortRateTree;
using System.Diagnostics;

namespace ShortRateTreeTest
{
    [TestClass]
    public class CalibrationHelperTest
    {
        /// <summary>
        /// キャリブレーション用スワップション条件生成 
        /// </summary>
        [TestMethod]
        public void TestMethod1()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            double swapRate = 0.01;
            double divideInterval = 60;

            int cashflowNumber = 6;
            int resetIntervalMonths = 6;
            for (int i = 0; i < cashflowNumber - 1; ++i)
            {
                DateTime[] exerciseDates;
                Cashflow[] cashflows;
                CalibrationHelper.GetSwaptionCondition(baseDate.AddMonths(resetIntervalMonths * (i + 1))
                    , resetIntervalMonths, cashflowNumber - i, swapRate, out exerciseDates, out cashflows);
                double[] divideIntervals = cashflows.Select(x => divideInterval).ToArray();
                SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
                sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows, divideIntervals);
                sbs.OutputCsvExerciseDates(string.Format("BermudanSwaptionExerciseDates{0}.csv", i));
                sbs.OutputCsvCashflows(string.Format("BermudanSwaptionCashflows{0}.csv", i));
                sbs.OutputCsvTimeIntervals(string.Format("BermudanSwaptionTimeIntervals{0}.csv", i));
            }
        }
        /// <summary>
        /// </summary>
        [TestMethod]
        public void TestMethod2()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            double swapRate = 0.01;
            double divideInterval = 6;
            double r = 0.01;
            double a = 0.0005;
            double sigma = 0.5;

            /// キャリブレーション用ツリーによるヨーロピアンスワップション評価オブジェクトの用意
            int cashflowNumber = 2;
            int resetIntervalMonths = 6;
            SimpleBermudanSwaption[] sbss = new SimpleBermudanSwaption[cashflowNumber - 1];
            for (int i = 0; i < cashflowNumber - 1; ++i)
            {
                DateTime[] exerciseDates;
                Cashflow[] cashflows;
                CalibrationHelper.GetSwaptionCondition(baseDate.AddMonths(resetIntervalMonths * (i + 1))
                    , resetIntervalMonths, cashflowNumber - i, swapRate, out exerciseDates, out cashflows);
                double[] divideIntervals = cashflows.Select(x => divideInterval).ToArray();
                SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
                sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows, divideIntervals);
                double[] times = sbs.GetTreeTimes();
                double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
                sbs.SetBondPrices(bondPrices);
                sbs.InitializeTree(a, sigma);
                sbss[i] = sbs;
                Console.WriteLine("PV{0}={1}", i, sbs.ComputePV());
            }
            /// ダミーの市場価格
            /// 本当はボラから計算する必要がある。
            double[] PVs = sbss.Select(x => 0.01D).ToArray();
            double error = CalibrationHelper.CalibrateTreeSigmaToSwaptionValues(PVs, sbss, a, out sigma, 0.1, 0.9);
            Console.WriteLine("sigma = {0}, error = {1}", sigma, error);
            for (int i = 0; i < sbss.Length; ++i)
            {
                Console.WriteLine("PV{0}={1}", i, sbss[i]._Tree._TreeNodes[0][0].ContingentClaimValue);
            }
        }
    }
}
