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
        /// <summary>
        /// この区間の最大時点に対するツリー時点のindex
        /// </summary>
        public int MaxTreeTimeIndex;
        /// <summary>
        /// この区間の右端の時点が、権利行使時点か 
        /// </summary>
        public bool IsExerciseDate;
        /// <summary>
        /// この区間の左端の時点が、割引債の満期時点であるか 
        /// </summary>
        public bool IsDiscountBondPriceMaturity;

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
            if (DateTime.Compare(leftDate, rightDate) == 0) return false;
            int d = (int)Math.Round((rightDate - leftDate).TotalDays / divideIntervalDays, MidpointRounding.AwayFromZero);
            TreeTimes = new double[d + 1];
            for (int i = 0; i <= d; ++i)
            {
                TreeTimes[i] = (leftDate.AddDays(i * divideIntervalDays) - baseDate).TotalDays / 365D;
            }
            return d > 0;
        }

        //public static 
        public string ToStringValues()
        {
            string[] s = TreeTimes.Select(x => string.Format("{0}", x)).ToArray();
            return string.Format("{0},{1},{2},{3}",
                MaxTreeTimeIndex, IsExerciseDate, IsDiscountBondPriceMaturity, string.Join("_", s) 
                );
        }
    }
}
