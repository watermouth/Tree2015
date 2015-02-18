using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortRateTree
{
    public class CalibrationHelper
    {
        /// <summary>
        /// 標準的なヨーロピアンスワップションの簡易化された条件を取得する.
        ///
        /// リセット日の日付調整は実施していない。
        /// 利払い日が次のCFのリセット日になっている。
        /// 権利行使日はリセット日に一致させる。 
        /// 必要ならいくつか条件を細かく改修すること。
        /// </summary>
        /// <param name="startResetDate"></param>
        /// <param name="resetIntervalMonths"></param>
        /// <param name="cashflowNumber"></param>
        /// <param name="swapRate"></param>
        /// <param name="exerciseDate"></param>
        /// <param name="cashflows"></param>
        static public void GetSwaptionCondition(DateTime startResetDate, int resetIntervalMonths, int cashflowNumber, double swapRate
            , out DateTime[] exerciseDate, out Cashflow[] cashflows)
        {
            exerciseDate = new DateTime[1] { startResetDate };
            cashflows = new Cashflow[cashflowNumber];
            for (int i = 0; i < cashflows.Length; ++i)
            {
                cashflows[i] = new Cashflow(startResetDate.AddMonths(resetIntervalMonths * i)
                    , startResetDate.AddMonths(resetIntervalMonths * (i + 1))
                    , swapRate);
            }
        }
        /// <summary>
        /// 1次元版Levenberg-Marquardt法によるsigmaの探索
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
            double c = 1e6;
            sigma = initialSigma;
            /// 初回計算
            /// sigmaの設定とツリー計算準備
            for (int i = 0; i < europeanSwaptions.Length; ++i)
            {
                europeanSwaptions[i].InitializeTree(a, sigma);
                europeanSwaptions[i].FitToBondPrices();
            }
            /// 目的関数値などJの計算
            ComputeSwaptionCalibrationObjectives(true, inputVs, europeanSwaptions, sigma, deltaSigma
                , out prevJ, ref J1, ref J2);
            /// sigma の変化分の算出とその値によるJの算出
            double ds = -J1 / (c * J2);
            do
            {
                double sigmaCandidate = sigma + ds > 0 ? sigma + ds : initialSigma;
                /// sigmaの設定とツリー計算準備
                for (int i = 0; i < europeanSwaptions.Length; ++i)
                {
                    europeanSwaptions[i].InitializeTree(a, sigmaCandidate);
                    europeanSwaptions[i].FitToBondPrices();
                }
                /// 目的関数値などJの計算
                ComputeSwaptionCalibrationObjectives(false, inputVs, europeanSwaptions, sigmaCandidate, deltaSigma
                    , out J, ref J1, ref J2);
                /// 比較と更新
                if (J >= prevJ)
                {
                    c *= 10;
                    /// sigma の変化分の算出とその値によるJの算出
                    ds = -J1 / (c * J2);
                    continue;
                }
                c /= 10;
                prevJ = J;
                sigma += ds;
                for (int i = 0; i < europeanSwaptions.Length; ++i)
                {
                    europeanSwaptions[i].InitializeTree(a, sigma);
                    europeanSwaptions[i].FitToBondPrices();
                }
                ComputeSwaptionCalibrationObjectives(true, inputVs, europeanSwaptions, sigma, deltaSigma
                    , out prevJ, ref J1, ref J2);
                ds = -J1 / (c * J2);
            } while (J > error);
            return J;
        }
        static void ComputeSwaptionCalibrationObjectives(bool withDerivative
            , double[] inputVs, SimpleBermudanSwaption[] europeanSwaptions
            , double sigma, double deltaSigma, out double J, ref double J1, ref double J2)
        {
            /// 桁落ちが出来るだけ起きないように差を最後にまとめておこなう
            double positive0 = 0;
            double negative0 = 0;
            ///// 1階微分の項
            double positive1 = 0;
            double negative1 = 0;
            ///// 2階微分の項
            double positive2 = 0;
            double negative2 = 0;
            for (int i = 0; i < europeanSwaptions.Length; ++i)
            {
                double v, vShift, iv2;
                v = europeanSwaptions[i].ComputePV();
                iv2 = (inputVs[i] * inputVs[i]);
                positive0 += 1 + v * v / iv2;
                negative0 += 2 * v / inputVs[i];
                if (withDerivative)
                {
                    vShift = europeanSwaptions[i].ComputeSigmaShiftedPV(deltaSigma);
                    //J1 += (1 - v / inputVs[i]) * (vShift - v) / (deltaSigma * inputVs[i]);
                    //J2 += Math.Pow((vShift - v) / (deltaSigma * inputVs[i]), 2);
                    positive1 += v * (inputVs[i] + vShift) / iv2;
                    negative1 += (inputVs[i] * vShift + v * v) / iv2;
                    positive2 += (vShift * vShift + v * v) / iv2;
                    negative2 += 2 * vShift * v / iv2;
                }
            }
            J = (positive0 - negative0) / 2;
            if (withDerivative)
            {
                J1 = (positive1 - negative1) / (deltaSigma);
                J2 = (positive2 - negative2) / (deltaSigma * deltaSigma);
            }
            if (Double.IsNaN(J))
            {
                J = Double.PositiveInfinity;
            }
        }
    }
}
