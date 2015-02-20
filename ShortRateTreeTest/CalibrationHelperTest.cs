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
            double divideInterval = 60;

            int cashflowNumber = 6;
            int resetIntervalMonths = 6;
            for (int i = 0; i < cashflowNumber - 1; ++i)
            {
                DateTime[] exerciseDates;
                Cashflow[] cashflows;
                CalibrationHelper.GetSwaptionCondition(baseDate.AddMonths(resetIntervalMonths * (i + 1))
                    , resetIntervalMonths, cashflowNumber - i, out exerciseDates, out cashflows);
                double[] divideIntervals = cashflows.Select(x => divideInterval).ToArray();
                SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
                sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows, divideIntervals);
                sbs.OutputCsvExerciseDates(string.Format("BermudanSwaptionExerciseDates{0}.csv", i));
                sbs.OutputCsvCashflows(string.Format("BermudanSwaptionCashflows{0}.csv", i));
                sbs.OutputCsvTimeIntervals(string.Format("BermudanSwaptionTimeIntervals{0}.csv", i));
            }
        }
        /// <summary>
        /// キャリブレーション実行のテスト
        /// - ツリーによるスワップション評価オブジェクトの値域の中に入力価値が入らないと当てはめようがない。
        /// - ツリーの分割数が十分小さくないと、少なくとも短期のスワップションについて、当てはめようがない。
        /// - 複数のヨーロピアンスワップションに合わせるようにすることである程度安定的にキャリブレーションできる。
        /// </summary>
        [TestMethod]
        public void TestMethod2()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            double divideInterval = 10;
            double r = 0.01;

            /// キャリブレーション用ツリーによるヨーロピアンスワップション評価オブジェクトの用意
            int cashflowNumber = 20;
            int resetIntervalMonths = 12;
            SimpleBermudanSwaption[] sbss = new SimpleBermudanSwaption[cashflowNumber-1];
            for (int i = 1; i < cashflowNumber; ++i)
            {
                DateTime[] exerciseDates;
                Cashflow[] cashflows;
                CalibrationHelper.GetSwaptionCondition(baseDate.AddMonths(resetIntervalMonths * i)
                    , resetIntervalMonths, cashflowNumber-i, out exerciseDates, out cashflows);
                /// スワップレートの設定
                List<double> bondPricesAtCashflowDates = cashflows.Select(x => Math.Exp(-r * (x.ResetDate - baseDate).Days / 365D)).ToList();
                bondPricesAtCashflowDates.Add(Math.Exp(-r * (cashflows.Last().SettlementDate - baseDate).Days / 365D));
                double[] bondPricesForSwapRate = bondPricesAtCashflowDates.ToArray();
                double[] yearFractionsForSwapRate = bondPricesForSwapRate.Select(x => resetIntervalMonths / 12D).ToArray();
                double swapRate = CalibrationHelper.GetForwardSwapRate(bondPricesForSwapRate, yearFractionsForSwapRate);
                Console.WriteLine("SwapRate={0}", swapRate);
                foreach (Cashflow cf in cashflows) cf.SwapRate = swapRate;
                /// スワップションオブジェクトの生成
                double[] divideIntervals = cashflows.Select(x => divideInterval).ToArray();
                SimpleBermudanSwaption sbs = new SimpleBermudanSwaption();
                sbs.DivideTimeIntervals(baseDate, exerciseDates, cashflows, divideIntervals);
                double[] times = sbs.GetTreeTimes();
                double[] bondPrices = times.Select(x => Math.Exp(-r * x)).ToArray();
                sbs.SetBondPrices(bondPrices);
                sbs.SetPayerOrReceiver(true);
                sbss[i-1] = sbs;
            }
            /// ダミーの市場価格
            /// 本当はボラから計算する必要がある。
            double[] PVs = sbss.Select(x => 0.00005D).ToArray();
            double a, sigma;
            CalibrationHelper.CalibrateToSwaptionValues(PVs, sbss, 0, 0.2D, out a, out sigma);
            Console.WriteLine("a={0}, sigma={1}", a, sigma);
            for (int i = 0; i < sbss.Length; ++i)
            {
                sbss[i].OutputCsvCashflows(string.Format("ESwaptionCashflow{0}.csv", i));
                Console.WriteLine("{0}, Input : {1}, Fitted : {2}", i, PVs[i], sbss[i]._Tree._TreeNodes[0][0].ContingentClaimValue);
            }
        }
        /// <summary>
        /// スワップレートの確認
        /// </summary>
        [TestMethod]
        public void TestMethod3()
        {
            double r = 0.01D;
            double[] bondPrices = Enumerable.Range(1, 6).Select(x=> Math.Exp(-r*x)).ToArray();
            double[] yearFractions = bondPrices.Select(x=> 0.5).ToArray();
            double swapRate = CalibrationHelper.GetForwardSwapRate(bondPrices, yearFractions);
            using (var sw = new System.IO.StreamWriter("SwapRate.csv", false))
            {
                sw.WriteLine("bondPrice,yearFraction");
                for (int i = 0; i < bondPrices.Length; ++i)
                {
                    sw.WriteLine("{0},{1}", bondPrices[i], yearFractions[i]);
                }
            }
            Console.WriteLine("");
            Console.WriteLine("SwapRate={0}", swapRate);
        }
    }
}
