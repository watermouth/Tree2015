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

        public void DivideTimeInterval(DateTime baseDate, DateTime leftDate, DateTime rightDate, double divideIntervalDays)
        {
            Debug.Assert(DateTime.Compare(leftDate, rightDate) < 0);
            int d = (int)Math.Round((rightDate - leftDate).TotalDays / divideIntervalDays, MidpointRounding.AwayFromZero);
            TreeTimes = new double[d + 1];
            for (int i = 0; i <= d; ++i)
            {
                TreeTimes[i] = (leftDate.AddDays(i * divideIntervalDays) - baseDate).TotalDays / 365D;
            }
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
