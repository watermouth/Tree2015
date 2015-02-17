using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShortRateTree
{
    /// <summary>
    /// Bermudan Swaption 評価用のTime Interval
    /// </summary>
    public class TimeInterval
    {
        /// <summary>
        /// この区間内のツリー分割時点
        /// </summary>
        public double[] TreeTimes;
        public DateTime[] TreeDates;///Debug用
        /// <summary>
        /// この区間の最大時点に対するツリー時点のindex
        /// </summary>
        public int MaxTreeTimeIndex;
        /// <summary>
        /// この区間の最小時点に対するツリー時点のindex
        /// </summary>
        public int MinTreeTimeIndex;
        /// <summary>
        /// この区間の右端の時点が、権利行使時点か 
        /// </summary>
        public bool IsExerciseDate;
        /// <summary>
        /// この区間の左端の時点が、割引債の満期時点であるか 
        /// </summary>
        public bool IsDiscountBondPriceMaturity;
        /// <summary>
        /// この区間に属すCashflow
        /// </summary>
        public Cashflow cashflow;
        /// <summary>
        /// 2つの日付間を分割する。分割できればtrueを返す。
        /// 分割されないとき(leftDate == rightDate)はfalseを返す.
        /// </summary>
        /// <param name="baseDate"></param>
        /// <param name="leftDate"></param>
        /// <param name="rightDate"></param>
        /// <param name="divideIntervalDays"></param>
        /// <returns></returns>
        public bool DivideTimeInterval(DateTime baseDate, DateTime leftDate, DateTime rightDate, double divideIntervalDays)
        {
            Debug.Assert(DateTime.Compare(baseDate, leftDate) <= 0, "基準日はleftDate以前でなければならない");
            Debug.Assert(DateTime.Compare(leftDate, rightDate) <= 0, "leftDate <= rightDateでなければならない");
            if (DateTime.Compare(leftDate, rightDate) == 0)
            {
                /// 分割なし
                TreeTimes = new double[1];
                TreeDates = new DateTime[1];
                return false;
            }
            /// 分割
            int d = (int)Math.Round((rightDate - leftDate).TotalDays / divideIntervalDays, MidpointRounding.AwayFromZero);
            d = d == 0 ? 1 : d;
            TreeTimes = new double[d+1];
            TreeDates = new DateTime[d+1];
            for (int i = 0; i < d; ++i)
            {
                TreeDates[i] = leftDate.AddDays(i * divideIntervalDays);
                TreeTimes[i] = (TreeDates[i] - baseDate).TotalDays / 365D;
            }
            TreeDates[d] = rightDate;
            TreeTimes[d] = (rightDate - baseDate).TotalDays / 365D;
            return d > 0;
        }
        /// <summary>
        /// 分割後の区間を取得
        /// </summary>
        /// <param name="baseDate"></param>
        /// <param name="leftDate"></param>
        /// <param name="rightDate"></param>
        /// <param name="divideIntervalDays"></param>
        /// <param name="isLeftDiscountBondPriceMaturity"></param>
        /// <param name="isRightExerciseDate"></param>
        /// <param name="leftTreeTimeIndex"></param>
        public bool SetTimeInterval(DateTime baseDate, DateTime leftDate, DateTime rightDate, double divideIntervalDays
            , bool isLeftDiscountBondPriceMaturity, bool isRightExerciseDate
            , Cashflow cashflow, ref int leftTreeTimeIndex)
        {
            /// 分割できないときはfalse
            if (!DivideTimeInterval(baseDate, leftDate, rightDate, divideIntervalDays))
            {
                return false;
            };
            IsDiscountBondPriceMaturity = isLeftDiscountBondPriceMaturity;
            IsExerciseDate = isRightExerciseDate;
            this.cashflow = cashflow;
            MinTreeTimeIndex = leftTreeTimeIndex;
            MaxTreeTimeIndex = leftTreeTimeIndex + TreeTimes.Length - 1;
            leftTreeTimeIndex = MaxTreeTimeIndex;
            return true;
        }

        public static string ToStringValuesHeader()
        {
            return string.Format("MaxTreeTimeIndex,IsDiscountBondPriceMaturity,IsExerciseDate,separation");
        } 
        public string ToStringValues()
        {
            string[] s = TreeTimes.Select(x => string.Format("{0}", x)).ToArray();
            string[] sd = TreeDates.Select(x => string.Format("{0}", x)).ToArray();
            return string.Format("{0},{1},{2},{3}",
                MaxTreeTimeIndex, IsDiscountBondPriceMaturity, IsExerciseDate
                , string.Join("|", string.Join("_", s), string.Join("_", sd))
                );
        }
    }
}
