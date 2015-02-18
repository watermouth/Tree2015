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
        /// <param name="bondPrices">europeanSwaptionsそれぞれのツリー時点に対する債券価格の配列の配列
        /// bondPrices[i]がi番目のeuropeanSwaptionのツリー時点に対する配列</param>
        /// <returns></returns>
        static public double CalibrateTreeSigmaToSwaptionValues(
            double[] inputVs, SimpleBermudanSwaption[] europeanSwaptions, double[][] bondPrices
            , double a, double incrementRatio = 0.01, double initialSigma = 0.2D, double error = 1e-5)
        {
            /// 目的関数
            /// sum_i (inputVs[i] - ツリーによるSwaption価値[i])^2
            /// を最小化する sigma を求める
            /// 1階微分を差分で離散近似して用いる。

            /// 目的関数
            double J;
            double sigma = initialSigma;
            do
            {
                /// sigmaの設定とツリー計算準備
                for (int i = 0; i < europeanSwaptions.Length; ++i)
                {
                    europeanSwaptions[i].InitializeTree(a, sigma);
                    europeanSwaptions[i].FitToBondPrices();
                }
                /// 桁落ちが出来るだけ起きないように差を最後にまとめておこなう
                double positive0 = 0;
                double negative0 = 0;
                /// 1階微分の項
                double positive1 = 0;
                double negative1 = 0;
                /// 2階微分の項
                double positive2 = 0;
                double negative2 = 0;
                for (int i = 0; i < europeanSwaptions.Length; ++i)
                {
                    double v, vShift, iv2;
                    v = europeanSwaptions[i].ComputePV();
                    vShift = europeanSwaptions[i].ComputeSigmaShiftedPV(incrementRatio);
                    iv2 = (inputVs[i] * inputVs[i]);
                    positive0 += 1 + v * v / iv2;
                    negative0 += 2 * v / inputVs[i];
                    positive1 += v * (inputVs[i] + vShift) / iv2;
                    negative1 += v * (vShift + v) / iv2;
                    positive2 += (vShift * vShift + v * v) / iv2;
                    negative2 += 2 * vShift * v / iv2;
                }
                J = (positive0 - negative0) / 2;
                double J1 = (positive1 - negative1) / incrementRatio;
                double J2 = (positive2 - negative2) / (incrementRatio * incrementRatio);
                sigma += -J1 / J2;
            } while (J > error);
            return J;
        }
    }
}
