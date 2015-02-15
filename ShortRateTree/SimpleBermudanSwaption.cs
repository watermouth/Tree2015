using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShortRateTree
{
    public class SimpleBermudanSwaption
    {
        //Tree tree;
        List<TimeInterval> timeIntervals;
        public void DivideTimeIntervals(DateTime baseDate, DateTime[] exerciseDates, Cashflow[] cashflows, int[] divideIntervalDays)
        {
            Debug.Assert(cashflows.Length <= divideIntervalDays.Length);
            Debug.Assert(exerciseDates.Length > 0);
            Debug.Assert(DateTime.Compare(baseDate, exerciseDates[0]) <= 0);
            timeIntervals = new List<TimeInterval>();
            int treeTimeIndex = 0;
            /// 初回の扱い
            TimeInterval tval = new TimeInterval();
            tval.SetTimeInterval(baseDate, baseDate, exerciseDates[0], (double)divideIntervalDays[0],
                false, true, ref treeTimeIndex);
            /// cashflow index
            int cashflowIndex = 0;
            /// 2つめの権利行使時点から最後の権利行使時点まで。 
            for (int i = 1; i < exerciseDates.Length; ++i)
            {
                Cashflow cf = cashflows[cashflowIndex];
                /// 実装チェック
                Debug.Assert(DateTime.Compare(exerciseDates[i - 1], cf.ResetDate) <= 0);
                /// 次の権利行使時点が直近のcf.ResetDateよりも手前かどうかで分ける;
                /// 複数の権利行使時点があるとき
                /// [i-1番目の権利行使時点, i番目の権利行使時点]
                if (DateTime.Compare(exerciseDates[i], cf.ResetDate) >= 0)
                {
                    tval = new TimeInterval();
                    ///このTimeIntervalの右端が権利行使時点
                    tval.SetTimeInterval(baseDate, exerciseDates[i - 1], exerciseDates[i], divideIntervalDays[i]
                        , false, true, ref treeTimeIndex);
                    timeIntervals.Add(tval);
                    continue;
                }
                Debug.Assert(DateTime.Compare(exerciseDates[i], cf.ResetDate) > 0);
                /// [i-1番目の権利行使時点, cashflowIndexのリセット時点]
                /// ただディスカウントする期間
                /// cashflowIndexを更新しないことに注意
                tval = new TimeInterval();
                ///このTimeIntervalの右端が権利行使時点でない
                tval.SetTimeInterval(baseDate, exerciseDates[i - 1], cf.ResetDate, divideIntervalDays[i]
                    , false, false, ref treeTimeIndex);
                timeIntervals.Add(tval);
                /// 次のcashflowのリセット時点がi番目の権利行使日よりも手前かどうかで場合わけ
                /// 次のcashflowがないときはパス
                while (cashflowIndex + 1 < cashflows.Length &&
                    DateTime.Compare(cashflows[cashflowIndex + 1].ResetDate, exerciseDates[i]) < 0)
                {
                    Cashflow nextCf = cashflows[cashflowIndex + 1];
                    /// [cashflowIndexのリセット時点, 次のcashflowIndexのリセット時点] 
                    tval = new TimeInterval();
                    tval.SetTimeInterval(baseDate, cf.ResetDate, nextCf.ResetDate, divideIntervalDays[i]
                        , true, false, ref treeTimeIndex);
                    timeIntervals.Add(tval);
                    cashflowIndex++;
                    cf = cashflows[cashflowIndex];
                }
                /// [cashflowIndexのリセット時点, i番目の権利行使時点]
                /// このTimeIntervalの左端を満期とする債券価格を考える
                tval.SetTimeInterval(baseDate, cf.ResetDate, exerciseDates[i], divideIntervalDays[i]
                    , true, true, ref treeTimeIndex);
                timeIntervals.Add(tval);
                /// cashflow Indexの更新
                cashflowIndex++;
            }
            /// 最終権利行使時点以降
            /// [最後の権利行使時点, cashflowIndexのリセット時点]
            /// ただディスカウントする期間
            /// cashflowIndexを更新しないことに注意
            tval = new TimeInterval();
            tval.SetTimeInterval(baseDate, exerciseDates[exerciseDates.Length - 1]
                , cashflows[cashflowIndex].ResetDate, divideIntervalDays[exerciseDates.Length - 1]
                , false, false, ref treeTimeIndex);
            timeIntervals.Add(tval);
            /// [cashflowIndexのリセット時点, 次のcashflowIndexのリセット時点 = このcashflowのSettlement]
            while (cashflowIndex < cashflows.Length)
            {
                tval = new TimeInterval();
                tval.SetTimeInterval(baseDate, cashflows[cashflowIndex].ResetDate, cashflows[cashflowIndex].SettlementDate
                    , divideIntervalDays[cashflowIndex], true, false, ref treeTimeIndex);
                cashflowIndex++;
            }
        }
    }
}