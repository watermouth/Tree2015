﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SolverFoundation.Solvers;
using Microsoft.SolverFoundation.Common;

namespace ShortRateTree
{
    public class CalibrationHelper
    {
        /// <summary>
        /// 標準的なヨーロピアンスワップションの簡易化された条件を取得する.
        /// スワップレートは後で設定すること。
        /// リセット日の日付調整は実施していない。
        /// 利払い日が次のCFのリセット日になっている。
        /// 権利行使日はリセット日に一致させる。 
        /// 必要ならいくつか条件を細かく改修すること。
        /// </summary>
        /// <param name="startResetDate"></param>
        /// <param name="resetIntervalMonths"></param>
        /// <param name="cashflowNumber"></param>
        /// <param name="exerciseDate"></param>
        /// <param name="cashflows"></param>
        static public void GetSwaptionCondition(DateTime startResetDate, int resetIntervalMonths, int cashflowNumber 
            , out DateTime[] exerciseDate, out Cashflow[] cashflows)
        {
            exerciseDate = new DateTime[1] { startResetDate };
            cashflows = new Cashflow[cashflowNumber];
            for (int i = 0; i < cashflows.Length; ++i)
            {
                cashflows[i] = new Cashflow(startResetDate.AddMonths(resetIntervalMonths * i)
                    , startResetDate.AddMonths(resetIntervalMonths * (i + 1))
                    , 0D);
            }
        }
        /// <summary>
        /// フォワードスワップレート算出 簡易用
        /// Cashflowに設定するフォワードスワップレートを求めるために使う。
        /// テスト実行用に用意した関数であり、既存機能で設定可能ならそれでよい。
        /// </summary>
        /// <param name="bondPrices">各CashflowのResetDate, 最後のCFのSettlementDateに対する割引債価格</param>
        /// <returns></returns>
        static public double GetForwardSwapRate(double[] bondPrices, double[] yearFractions)
        {
            double denom = 0;
            for (int i = 0; i < yearFractions.Length - 1; ++i)
            {
                denom += bondPrices[i+1] * yearFractions[i]; 
            }
            return (bondPrices[0] - bondPrices[bondPrices.Length - 1]) / denom;
        }
        static public void CalibrateToSwaptionValues(double[] inputVs
            , SimpleBermudanSwaption[] europeanSwaptions
            , double init_a, double init_sigma, out double a, out double sigma)
        {
            double[] xInitial = new double[] { init_a, init_sigma };
            double[] xLower = new double[] { 0, 1e-4 };
            double[] xUpper = new double[] { 10, 10 };
            var solution = NelderMeadSolver.Solve(x =>
            {
                double J, J1=0D, J2=0D;
                ComputeSwaptionCalibrationObjectives(false, inputVs, europeanSwaptions, x[0], x[1], out J, ref J1, ref J2);
                Console.WriteLine("J={0},a={1},sigma={2}", J, x[0], x[1]);
                return J;
            }, xInitial, xLower, xUpper);
            a = solution.GetValue(1);
            sigma = solution.GetValue(2);
            Console.WriteLine(solution.ToString());
        }
        static public void CalibrateToSwaptionValues(double[] inputVs
            , SimpleBermudanSwaption[] europeanSwaptions
            , double a, double init_sigma, out double sigma)
        {
            double[] xInitial = new double[] { init_sigma };
            double[] xLower = new double[] { 1e-4 };
            double[] xUpper = new double[] { 10 };
            var solution = NelderMeadSolver.Solve(x =>
            {
                double J, J1=0D, J2=0D;
                ComputeSwaptionCalibrationObjectives(false, inputVs, europeanSwaptions, a, x[0], out J, ref J1, ref J2);
                Console.WriteLine("J={0},sigma={1}", J, x[0]);
                return J;
            }, xInitial, xLower, xUpper);
            sigma = solution.GetValue(1);
            Console.WriteLine(solution.ToString());
        }
        /// <summary>
        /// 1次元版Levenberg-Marquardt法によるsigmaの探索 : 未完
        /// </summary>
        /// <param name="inputVs">各テナー構造・満期のヨーロピアンスワップションの価値</param>
        /// <param name="europeanSwaptions">キャリブレーション対象となるsigmaで生成されるツリーによるスワップション評価オブジェクト
        /// DivideTimeIntervalsまで実行済みであること。</param>
        /// <param name="deltaSigma">sigmaに関するデルタ計算用のsigmaの増分</param>
        /// <returns></returns>
        static public double CalibrateTreeSigmaToSwaptionValues(
            double[] inputVs, SimpleBermudanSwaption[] europeanSwaptions
            , double a, out double sigma, double deltaSigma = 0.01, double initialSigma = 0.2D, double error = 1e-5)
        {
            /// 目的関数
            /// sum_i (inputVs[i] - ツリーによるSwaption価値[i])^2
            /// を最小化する sigma を求める
            /// 1階微分を差分で離散近似して用いる。

            /// 目的関数
            double J, prevJ;
            /// 1階,2階微分
            double J1 = 0D;
            double J2 = 0D;
            /// Levenberg-Marquard法のペナルティ係数
            double c = 1e-6;
            sigma = initialSigma;
            /// 初回計算
            /// 目的関数値などJの計算
            ComputeSwaptionCalibrationObjectives(true, inputVs, europeanSwaptions, a, sigma
                , out prevJ, ref J1, ref J2);
            /// sigma の変化分の算出とその値によるJの算出
            double ds = -J1 / ((1 + c) * J2);
            do
            {
                double sigmaCandidate = sigma + ds > 0 ? sigma + ds : initialSigma;
                /// 目的関数値などJの計算
                ComputeSwaptionCalibrationObjectives(false, inputVs, europeanSwaptions, a, sigmaCandidate
                    , out J, ref J1, ref J2);
                /// 比較と更新 : 行き過ぎなので増分を変更してやりなおし
                if (J > prevJ)
                {
                    //c *= 10;
                    c *= 2;
                    /// sigma の変化分の算出とその値によるJの算出
                    ds = -J1 / ((1 + c) * J2);
                    continue;
                }
                    //c *= 10;
                c /= 2;
                prevJ = J;
                sigma += ds;
                ComputeSwaptionCalibrationObjectives(true, inputVs, europeanSwaptions, a, sigma 
                    , out prevJ, ref J1, ref J2);
                ds = -J1 / ((1 + c) * J2);
            } while (J > error);
            return J;
        }
        /// <summary>
        /// キャリブレーション用目的関数値の計算
        /// </summary>
        /// <param name="withDerivative"></param>
        /// <param name="inputVs"></param>
        /// <param name="europeanSwaptions"></param>
        /// <param name="a"></param>
        /// <param name="sigma"></param>
        /// <param name="J"></param>
        /// <param name="J1"></param>
        /// <param name="J2"></param>
        /// <param name="deltaSigma"></param>
        static public void ComputeSwaptionCalibrationObjectives(bool withDerivative
            , double[] inputVs, SimpleBermudanSwaption[] europeanSwaptions
            , double a, double sigma, out double J, ref double J1, ref double J2, double deltaSigma=0D)
        {
            /// sigmaの設定とツリー計算準備
            for (int i = 0; i < europeanSwaptions.Length; ++i)
            {
                europeanSwaptions[i].InitializeTree(a, sigma);
                europeanSwaptions[i].FitToBondPrices();
            }
            /// 桁落ちが出来るだけ起きないように差を最後にまとめておこなう
            double[] values = new double[europeanSwaptions.Length];
            double positive0 = 0;
            double negative0 = 0;
            for (int i = 0; i < europeanSwaptions.Length; ++i)
            {
                values[i] = europeanSwaptions[i].ComputePV();
                double v = values[i];
                positive0 += 1 + v * v / (inputVs[i] * inputVs[i]);
                negative0 += 2 * v / inputVs[i];
            }
            J = (positive0 - negative0) / 2;
            if (withDerivative)
            {
                double[] shiftedJs = new double[2];
                double[] deltaSigmas = new double[] { -deltaSigma * 0.5, deltaSigma * 0.5 };
                for (int j = 0; j < deltaSigmas.Length; ++j)
                {
                    positive0 = 0;
                    negative0 = 0;
                    for (int i = 0; i < europeanSwaptions.Length; ++i)
                    {
                        double v = europeanSwaptions[i].ComputeSigmaShiftedPV(deltaSigmas[j]);
                        positive0 += 1 + v * v / (inputVs[i] * inputVs[i]);
                        negative0 += 2 * v / inputVs[i];
                    }
                    shiftedJs[j] = (positive0 - negative0) / 2;
                }
                ///// 1階微分の項
                J1 = (shiftedJs[1] - shiftedJs[0]) / (deltaSigma);
                ///// 2階微分の項
                J2 = 4 * (shiftedJs[1] + shiftedJs[0] - 2 * J) / (deltaSigma * deltaSigma);
            }
        }
        static public void OutputCsvCalibrationObjectiveValues(string filepath
            , double[] inputVs, SimpleBermudanSwaption[] europeanSwaptions
            , double[] a, double[] sigma) 
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine("a,sigma,J");
                foreach (double aa in a)
                {
                    foreach (double ss in sigma)
                    {
                        double J, J1=0, J2=0;
                        ComputeSwaptionCalibrationObjectives(false, inputVs, europeanSwaptions, aa, ss, out J, ref J1, ref J2);
                        sw.WriteLine("{0},{1},{2}", aa, ss, J);
                    }
                }
            }
        }
    }
}
