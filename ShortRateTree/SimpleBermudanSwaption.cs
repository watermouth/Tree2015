﻿using System;
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
        Cashflow[] _cashflows;
        public List<TimeInterval> timeIntervals;
        private DateTime _baseDate;
        public DateTime[] _exerciseDates;
        public Tree _Tree;
        private double[] _BKParameter_a;
        private double[] _BKParameter_sigma;
        private double[] _bondPrices;
        private bool _IsPayersSwaption;
        public SimpleBermudanSwaption _SigmaShiftedSwaption;
        public void SetPayerOrReceiver(bool IsPayersSwaption)
        {
            _IsPayersSwaption = IsPayersSwaption;
        }
        /// <summary>
        /// 評価対象バミューダンスワップション情報を用いてツリー分割区間を構成する.
        /// 権利行使日と基準日が一致するとき、権利行使未実施かつ当日行使可能とみなして評価する。
        /// 基準日当日の権利を失っているものとして評価するときは、その権利行使日を入力しないこと。
        /// </summary>
        /// <param name="baseDate"></param>
        /// <param name="exerciseDates"></param>
        /// <param name="cashflows"></param>
        /// <param name="divideIntervalDays"></param>
        public void DivideTimeIntervals(DateTime baseDate, DateTime[] exerciseDates, Cashflow[] cashflows
            , double[] divideIntervalDays)
        {
            Debug.Assert(cashflows.Length <= divideIntervalDays.Length);
            Debug.Assert(exerciseDates.Length > 0);
            Debug.Assert(DateTime.Compare(baseDate, exerciseDates[0]) <= 0);
            _baseDate = baseDate;
            _cashflows = cashflows;
            _exerciseDates = exerciseDates;
            timeIntervals = new List<TimeInterval>();
            int treeTimeIndex = 0;
            /// cashflow index
            int cashflowIndex = 0;
            Cashflow cf = cashflows[cashflowIndex];
            TimeInterval tval = new TimeInterval();
            /// 初回の扱い : 権利行使日と基準日が一致するときも追加する
            if (tval.SetTimeInterval(baseDate, baseDate, exerciseDates[0], (double)divideIntervalDays[0],
                false, true, cf, ref treeTimeIndex, true)) timeIntervals.Add(tval);
            /// 2つめの権利行使時点から最後の権利行使時点まで。 
            for (int i = 1; i < exerciseDates.Length; ++i)
            {
                cf = cashflows[cashflowIndex];
                /// 実装チェック
                Debug.Assert(DateTime.Compare(exerciseDates[i - 1], cf.ResetDate) <= 0);
                /// 次の権利行使時点が直近のcf.ResetDateよりも手前かどうかで分ける;
                /// 複数の権利行使時点があるとき
                /// [i-1番目の権利行使時点, i番目の権利行使時点]
                /// 権利行使日 = リセット日となるケースを含めることに注意
                if (DateTime.Compare(exerciseDates[i], cf.ResetDate) <= 0)
                {
                    tval = new TimeInterval();
                    ///このTimeIntervalの右端が権利行使時点
                    if (tval.SetTimeInterval(baseDate, exerciseDates[i - 1], exerciseDates[i], divideIntervalDays[i]
                        , false, true, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
                    continue;
                }
                Debug.Assert(DateTime.Compare(exerciseDates[i], cf.ResetDate) > 0);
                /// [i-1番目の権利行使時点, cashflowIndexのリセット時点]
                /// ただディスカウントする期間
                /// cashflowIndexを更新しないことに注意
                tval = new TimeInterval();
                ///このTimeIntervalの右端が権利行使時点でない
                if (tval.SetTimeInterval(baseDate, exerciseDates[i - 1], cf.ResetDate, divideIntervalDays[i]
                    , false, false, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
                /// 次のcashflowのリセット時点がi番目の権利行使日よりも手前かどうかで場合わけ
                /// 次のcashflowがないときはパス
                while (cashflowIndex + 1 < cashflows.Length &&
                    DateTime.Compare(cashflows[cashflowIndex + 1].ResetDate, exerciseDates[i]) < 0)
                {
                    Cashflow nextCf = cashflows[cashflowIndex + 1];
                    /// [cashflowIndexのリセット時点, 次のcashflowIndexのリセット時点] 
                    tval = new TimeInterval();
                    if (tval.SetTimeInterval(baseDate, cf.ResetDate, nextCf.ResetDate, divideIntervalDays[i]
                        , true, false, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
                    cashflowIndex++;
                    cf = cashflows[cashflowIndex];
                }
                /// [cashflowIndexのリセット時点, i番目の権利行使時点]
                /// このTimeIntervalの左端を満期とする債券価格を考える
                tval = new TimeInterval();
                if (tval.SetTimeInterval(baseDate, cf.ResetDate, exerciseDates[i], divideIntervalDays[i]
                    , true, true, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
                /// cashflow Indexの更新
                cashflowIndex++;
            }
            /// 最終権利行使時点以降
            /// [最後の権利行使時点, cashflowIndexのリセット時点]
            /// ただディスカウントする期間
            /// cashflowIndexを更新しないことに注意
            tval = new TimeInterval();
            if (tval.SetTimeInterval(baseDate, exerciseDates[exerciseDates.Length - 1]
                , cashflows[cashflowIndex].ResetDate, divideIntervalDays[exerciseDates.Length - 1]
                , false, false, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
            /// [cashflowIndexのリセット時点, 次のcashflowIndexのリセット時点 = このcashflowのSettlement]
            while (cashflowIndex < cashflows.Length)
            {
                tval = new TimeInterval();
                if (tval.SetTimeInterval(baseDate, cashflows[cashflowIndex].ResetDate, cashflows[cashflowIndex].SettlementDate
                    , divideIntervalDays[cashflowIndex], true, false, cf, ref treeTimeIndex)) timeIntervals.Add(tval);
                cashflowIndex++;
            }
        }
        /// <summary>
        /// 分割したTimeIntervalを用いて、ツリーの分割時点を設定・取得する。
        /// 内部的に新たなTreeを作成する。
        /// </summary>
        public void SetTreeTimes()
        {
            List<double> treeTimeList = new List<double>();
            foreach (TimeInterval l in timeIntervals)
            {
                /// 右端を除いて時点を取得
                for (int i = 0; i < l.TreeTimes.Length - 1; ++i)
                {
                    treeTimeList.Add(l.TreeTimes[i]);
                }
            }
            /// 最後のTimeIntervalの右端の時点を取得
            treeTimeList.Add(timeIntervals.Last().TreeTimes.Last());
            _Tree = new Tree(treeTimeList.ToArray());
        }
        public double[] GetTreeTimes()
        {
            if (null == _Tree) SetTreeTimes();
            return (double[])_Tree._times.Clone();
        }
        /// <summary>
        /// ツリーの時点にあわせたa, sigmaを入力し、ツリーを初期化する
        /// </summary>
        /// <param name="a"></param>
        /// <param name="sigma"></param>
        public void InitializeTree(double[] a, double[] sigma)
        {
            _BKParameter_a = (double[])a.Clone();
            _BKParameter_sigma = (double[])sigma.Clone();
            _Tree.InitializeBackBones(a, sigma);
            _Tree.SetUpTreeNodes();
        }
        /// <summary>
        /// 全期間一定のa, sigmaによりツリーを初期化する 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="sigma"></param>
        public void InitializeTree(double a, double sigma)
        {
            if (_Tree == null) SetTreeTimes();
            if (_BKParameter_a == null)
            {
                _BKParameter_a = Enumerable.Repeat<double>(a, _Tree._TreeBackBones.Length).ToArray();
            }
            else
            {
                for (int i = 0; i < _BKParameter_a.Length; ++i)
                {
                    _BKParameter_a[i] = a;
                }
            }
            if (_BKParameter_sigma == null)
            {
                _BKParameter_sigma = Enumerable.Repeat<double>(sigma, _Tree._TreeBackBones.Length).ToArray();
            }
            else
            {
                for (int i = 0; i < _BKParameter_sigma.Length; ++i)
                {
                    _BKParameter_sigma[i] = sigma;
                }
            }
            InitializeTree(_BKParameter_a, _BKParameter_sigma);
        }
        /// <summary>
        /// 割引債価格にツリーを合わせる. 事前条件：InitializeTree実行済みであること。SetBondPrice実行済みであること。
        /// </summary>
        public void FitToBondPrices()
        {
            _Tree.FitToInputBondPrice(_bondPrices);
        }
        /// <summary>
        /// ツリーによる評価で用いる割引債価格配列
        /// </summary>
        /// <param name="bondPrices"></param>
        public void SetBondPrices(double[] bondPrices)
        {
            _bondPrices = bondPrices;
        }
        /// <summary>
        /// 設定したバミューダンスワップションの現在価値計算
        /// </summary>
        /// <returns>現在価値（=ツリーノード(0,0)のContingentClaimValue)</returns>
        public double ComputePV()
        {
            TreeNode[] columnNodes;
            TimeInterval[] tvals = timeIntervals.ToArray();
            TimeInterval tval;
            /// ツリーの最終時点を満期とする割引債価格などの初期化
            tval = tvals.Last();
            columnNodes = _Tree._TreeNodes[tval.MaxTreeTimeIndex];
            Debug.Assert(columnNodes != null, "InitializeTreeを先に実行してください");
            for (int j = 0; j < columnNodes.Length; ++j)
            {
                columnNodes[j].DiscountBondPrice = 1D;
                columnNodes[j].FixedLegValue = 0D;
                columnNodes[j].FloatLegValue = 0D;
                columnNodes[j].ContingentClaimValue = 0D;
            }
            /// Backward Induction
            /// TimeIntervalの属性に応じて処理する
            for (int l = tvals.Length - 1; l >= 0; --l)
            {
                tval = tvals[l];
                /// 右端
                /// Exercise Check
                /// 権利行使時点では権利行使価値を評価しCCを更新する
                if (tval.IsExerciseDate)
                {
                    columnNodes = _Tree._TreeNodes[tval.MaxTreeTimeIndex];
                    for (int j = 0; j < columnNodes.Length; ++j)
                    {
                        /// IRS Value , Receivers Swaption
                        double IRS = columnNodes[j].FixedLegValue - columnNodes[j].FloatLegValue;
                        if (_IsPayersSwaption) IRS = -IRS;
                        /// CC Value
                        columnNodes[j].ContingentClaimValue = Math.Max(columnNodes[j].ContingentClaimValue, IRS);
                    }
                }
                /// 右端から左端へ
                /// discounting
                for (int i = tval.MaxTreeTimeIndex - 1; i >= tval.MinTreeTimeIndex; --i)
                {
                    for (int j = 0; j < _Tree._TreeNodes[i].Length; ++j)
                    {
                        OneStepBackwardInduction(_Tree._TreeNodes, i, j);
                    }
                }
                /// 左端
                /// New Bond Maturity Check
                /// この区間の左端をリセット日とするキャッシュフローの寄与を加算する 
                /// この区間の左端を満期とする割引債価格として初期化する
                if (tval.IsDiscountBondPriceMaturity)
                {
                    columnNodes = _Tree._TreeNodes[tval.MinTreeTimeIndex];
                    for (int j = 0; j < columnNodes.Length; ++j)
                    {
                        /// Year Fraction計算は適宜変更すること
                        int days = (tval.cashflow.SettlementDate - tval.cashflow.ResetDate).Days;
                        /// Fixed Leg, Act/365
                        columnNodes[j].FixedLegValue += columnNodes[j].DiscountBondPrice * tval.cashflow.SwapRate * days / 365D;
                        /// Float Leg, Act/360
                        /// マルチカーブを意識したこの書き方にすると、長時間後のショートレート発散時にNaNになってしまう。
                        //double IBOR = (1 - columnNodes[j].DiscountBondPrice) / columnNodes[j].DiscountBondPrice;
                        //columnNodes[j].FloatLegValue += columnNodes[j].DiscountBondPrice * IBOR * days / 360D;
                        columnNodes[j].FloatLegValue += (1 - columnNodes[j].DiscountBondPrice) * days / 360D;
                        /// 新たな満期の割引債価格として初期化
                        columnNodes[j].DiscountBondPrice = 1D;
                    }
                }
            }
            return _Tree._TreeNodes[0][0].ContingentClaimValue;
        }
        public void OneStepBackwardInduction(TreeNode[][] nodes, int i, int j)
        {
            TreeNode node = nodes[i][j];
            /// down, mid, up nodeの順に集計
            int kIndex = node.k - _Tree._TreeBackBones[i + 1].jMin;
            double expMinusRDeltaT = Math.Exp(-node.r * _Tree._TreeBackBones[i].dt);
            node.DiscountBondPrice = expMinusRDeltaT * (
                node.pu * nodes[i + 1][kIndex + 1].DiscountBondPrice +
                node.pm * nodes[i + 1][kIndex].DiscountBondPrice +
                node.pd * nodes[i + 1][kIndex - 1].DiscountBondPrice
                );
            node.FixedLegValue = expMinusRDeltaT * (
                node.pu * nodes[i + 1][kIndex + 1].FixedLegValue +
                node.pm * nodes[i + 1][kIndex].FixedLegValue +
                node.pd * nodes[i + 1][kIndex - 1].FixedLegValue
                );
            node.FloatLegValue = expMinusRDeltaT * (
                node.pu * nodes[i + 1][kIndex + 1].FloatLegValue +
                node.pm * nodes[i + 1][kIndex].FloatLegValue +
                node.pd * nodes[i + 1][kIndex - 1].FloatLegValue
                );
            node.ContingentClaimValue = expMinusRDeltaT * (
                node.pu * nodes[i + 1][kIndex + 1].ContingentClaimValue +
                node.pm * nodes[i + 1][kIndex].ContingentClaimValue +
                node.pd * nodes[i + 1][kIndex - 1].ContingentClaimValue
                );
        }
        /// <summary>
        /// sigmaがシフトしたときのPVを計算する 
        /// </summary>
        /// <param name="deltaSigma">sigmaの増分</param>
        /// <returns></returns>
        public double ComputeSigmaShiftedPV(double deltaSigma)
        {
            /// シフト計算用のバミューダンスワップションオブジェクト
            /// ツリーは固有だが、ほかは共有。
            if (_SigmaShiftedSwaption == null)
            {
                _SigmaShiftedSwaption = (SimpleBermudanSwaption)this.MemberwiseClone();
                _SigmaShiftedSwaption.SetTreeTimes();/// treeは専用のものを作成。
            }
            /// shifted sigma
            double[] shiftedSigma = _BKParameter_sigma.Select(x => (x + deltaSigma)).ToArray();
            _SigmaShiftedSwaption.InitializeTree(_BKParameter_a, shiftedSigma);
            _SigmaShiftedSwaption.FitToBondPrices();
            return _SigmaShiftedSwaption.ComputePV();
        }

        public void OutputCsvExerciseDates(string filepath)
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine("ExerciseDate");
                for (int i = 0; i < _exerciseDates.Length; ++i)
                {
                    sw.WriteLine(_exerciseDates[i]);
                }
            }
        }
        public void OutputCsvCashflows(string filepath)
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine(Cashflow.ToStringValuesHeader());
                for (int i = 0; i < _cashflows.Length; ++i)
                {
                    sw.WriteLine(_cashflows[i].ToStringValues());
                }
            }
        }
        public void OutputCsvTimeIntervals(string filepath)
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine(TimeInterval.ToStringValuesHeader());
                for (int i = 0; i < timeIntervals.Count; ++i)
                {
                    sw.WriteLine(timeIntervals[i].ToStringValues());
                }
            }
        }
    }
}