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
            int resetCount = 6;
            double swapRate = 0.01;
            double divideInterval = 30;
            Debug.Assert(resetCount >= exerciseCount);
            DateTime[] exerciseDates = Enumerable.Range(1, exerciseCount).Select(x => baseDate.AddMonths(x * 6)).ToArray();
            DateTime[] resetDates = Enumerable.Range(1, resetCount + 1).Select(x => baseDate.AddMonths(x * 6).AddDays(2)).ToArray();
            double[] divideIntervals = resetDates.Select(x => divideInterval).ToArray();
            //divideIntervals[0] *= 0.1 * divideIntervals[0];
            //divideIntervals[1] *= 0.1 * divideIntervals[1];
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
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            sbs.InitializeTree(a, sigma);
            sbs.SetBondPrices(bondPrices);
            sbs.FitToBondPrices();
            sbs.SetPayerOrReceiver(true);
            Console.WriteLine("PV \t\t ={0}", sbs.ComputePV());
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0003));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            stopWatch.Stop();
            Console.WriteLine("{0}ms", stopWatch.ElapsedMilliseconds);

            Console.WriteLine("InitializeTree(double, double)");
            stopWatch.Reset();
            stopWatch.Start();
            sbs.InitializeTree(a[0], sigma[0]);
            sbs.SetBondPrices(bondPrices);
            sbs.FitToBondPrices();
            sbs.SetPayerOrReceiver(true);
            Console.WriteLine("PV \t\t ={0}", sbs.ComputePV());
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0003));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            stopWatch.Stop();
            Console.WriteLine("{0}ms", stopWatch.ElapsedMilliseconds);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");

            SimpleBermudanSwaption sbs2 = new SimpleBermudanSwaption();
            sbs2.DivideTimeIntervals(baseDate, exerciseDates, cashflows.ToArray()
            , divideIntervals.Select(x => 0.5*x).ToArray());
            sbs2.SetTreeTimes();
            sbs2.SetBondPrices(sbs2.GetTreeTimes().Select(x => Math.Exp(-r * x)).ToArray());
            sbs2.SetPayerOrReceiver(true);
            using (var sw = new System.IO.StreamWriter("PVSigma.csv", false))
            {
                double unit = 0.01;
                sw.WriteLine("sigma,pv,pv2");
                for (int i = 1; i <= 1D / unit; ++i)
                {
                    sbs.InitializeTree(a[0], i * unit);
                    sbs.FitToBondPrices();
                    sbs2.InitializeTree(a[0], i * unit);
                    sbs2.FitToBondPrices();
                    sw.WriteLine("{0},{1},{2}", i * unit, sbs.ComputePV(), sbs2.ComputePV());
                }
            }
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
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            sbs.InitializeTree(a, sigma);
            sbs.SetBondPrices(bondPrices);
            sbs.FitToBondPrices();
            sbs.SetPayerOrReceiver(true);
            Console.WriteLine("PV \t\t ={0}", sbs.ComputePV());
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0003));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            stopWatch.Stop();
            Console.WriteLine("{0}ms", stopWatch.ElapsedMilliseconds);
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
            int exerciseCount = 1;
            int resetCount = 20;
            double swapRate = 0.01;
            double divideInterval = 30 * 3;
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
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            sbs.InitializeTree(a, sigma);
            sbs.SetBondPrices(bondPrices);
            sbs.FitToBondPrices();
            sbs.SetPayerOrReceiver(true);
            Console.WriteLine("PV \t\t ={0}", sbs.ComputePV());
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0003));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            stopWatch.Stop();
            Console.WriteLine("{0}ms", stopWatch.ElapsedMilliseconds);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");
        }
        /// <summary>
        /// 権利行使日が評価基準日に一致するとき, かつ権利行使日が1つのとき
        /// スワップの価値が正なら正になる　かつ　sigmaに依存しない値になる
        /// </summary>
        [TestMethod]
        public void TestMethod4()
        {
            DateTime baseDate = new DateTime(2015, 2, 1);
            int exerciseCount = 1;
            int resetCount = 6;
            double swapRate = 0.01;
            double divideInterval = 60;
            Debug.Assert(resetCount >= exerciseCount);
            DateTime[] exerciseDates = Enumerable.Range(0, exerciseCount).Select(x => baseDate.AddMonths(x * 6)).ToArray();
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
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            sbs.InitializeTree(a, sigma);
            sbs.SetBondPrices(bondPrices);
            sbs.FitToBondPrices();
            sbs.SetPayerOrReceiver(false);
            Console.WriteLine("PV \t\t ={0}", sbs.ComputePV());
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0003));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0002));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0001));
            Console.WriteLine("PVShifted \t ={0}", sbs.ComputeSigmaShiftedPV(0.0000));
            stopWatch.Stop();
            Console.WriteLine("{0}ms", stopWatch.ElapsedMilliseconds);
            sbs._Tree.OutputCsvTreeBackBones("BermudanSwaptionTreeBackBones.csv");
            sbs._Tree.OutputCsvTreeNodes("BermudanSwaptionTreeNodes.csv");
        }
    }
}
