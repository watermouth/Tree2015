using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
namespace ShortRateTree
{
    public class Tree
    {
        private double[] _times;
        public TreeBackBone[] _TreeBackBones;
        public TreeNode[][] _TreeNodes;

        public Tree(double[] times)
        {
            Debug.Assert(times != null && times.Length > 1);
            _times = (double[])times.Clone();
            _TreeBackBones = new TreeBackBone[_times.Length];
            _TreeNodes = new TreeNode[_times.Length][];
        }

        /// <summary>
        /// 時間分割数
        /// </summary>
        /// <returns></returns>
        private int GetTimeSeparationNumber()
        {
            return _times.Length - 1;
        }
        private double ComputeV(double a, double sigma, double dt)
        {
            return Math.Sqrt(sigma * sigma * (1 - Math.Exp(-2 * a * dt)) / (2 * a));
        }

        public void InitializeBackBones(double[] a, double[] sigma)
        {
            Debug.Assert(a.Length == _times.Length && sigma.Length == _times.Length);
            /// 0, 1, ..., N 時点 分割数はN : N分割するとき、back boneはN個用いる
            _TreeBackBones[0] = new TreeBackBone();
            _TreeBackBones[0].t = _times[0];
            _TreeBackBones[0].a = a[0];
            _TreeBackBones[0].sigma = sigma[0];
            _TreeBackBones[0].dt = _times[1] - _times[0];
            _TreeBackBones[0].dx = 0;
            _TreeBackBones[0].V = ComputeV(a[0], sigma[0], _TreeBackBones[0].dt);
            /// i = 1, ..., N
            for (int i = 1; i < GetTimeSeparationNumber(); ++i)
            {
                _TreeBackBones[i] = new TreeBackBone();
                _TreeBackBones[i].t = _times[i];
                _TreeBackBones[i].a = a[i];
                _TreeBackBones[i].sigma = sigma[i];
                _TreeBackBones[i].dt = _times[i + 1] - _times[i];
                _TreeBackBones[i].dx = _TreeBackBones[i - 1].V * Math.Sqrt(3);
                _TreeBackBones[i].V = ComputeV(a[i], sigma[i], _TreeBackBones[i].dt);
            }
            int ii = GetTimeSeparationNumber();
            _TreeBackBones[ii] = new TreeBackBone();
            _TreeBackBones[ii].t = _times[ii];
            _TreeBackBones[ii].a = a[ii];
            _TreeBackBones[ii].sigma = sigma[ii];
            _TreeBackBones[ii].dt = 0;
            _TreeBackBones[ii].dx = _TreeBackBones[ii - 1].V * Math.Sqrt(3);
            _TreeBackBones[ii].V = ComputeV(a[ii], sigma[ii], _TreeBackBones[ii].dt);
        }
        /// <summary>
        /// ツリーノードの構築
        /// </summary>
        public void SetUpTreeNodes()
        {
            /// Root Node
            _TreeNodes[0] = new TreeNode[1];
            _TreeNodes[0][0] = new TreeNode();
            _TreeNodes[0][0].Initialize(0, 0, Math.Exp(-_TreeBackBones[0].a * _TreeBackBones[0].dt)
                , _TreeBackBones[1].dx, _TreeBackBones[0].V, _TreeBackBones[0].V * _TreeBackBones[0].V);
            _TreeBackBones[0].jMin = 0;
            _TreeBackBones[0].jMax = 0;
            /// i = 1, 2, ..., N (Leaf Nodeに該当) 
            int preNodeCount = 1;
            for (short i = 1; i < _times.Length; ++i)
            {
                /// i時点のTreeBackBone
                TreeBackBone ithBone = _TreeBackBones[i];
                /// jMin, jMaxの取得
                ithBone.jMax = (short)(_TreeNodes[i - 1][preNodeCount - 1].k + 1);
                ithBone.jMin = (short)(_TreeNodes[i - 1][0].k - 1);
                /// i時点のノード数
                int nodeCount = ithBone.jMax - ithBone.jMin + 1;
                /// i時点のノードのnew
                _TreeNodes[i] = new TreeNode[nodeCount];
                for (short j = 0; j < nodeCount; ++j) { _TreeNodes[i][j] = new TreeNode(); }
                /// 最終時点より前と最終時点で分ける
                if (i < GetTimeSeparationNumber())
                {
                    /// j = jMin, ..., jMax に対するnodeを順に初期化する
                    /// j = jMin, ..., jMax に対応する配列index 0,1,...,nodeCount-1
                    TreeBackBone nextBone = _TreeBackBones[i + 1];
                    double expMinusADeltaT = Math.Exp(-ithBone.a * ithBone.dt);
                    double V2 = ithBone.V * ithBone.V;
                    for (short j = 0; j < nodeCount; ++j)
                    {
                        _TreeNodes[i][j].Initialize((short)(j + ithBone.jMin), ithBone.dx, expMinusADeltaT, nextBone.dx, ithBone.V, V2);
                    }
                }
                else
                {
                    for (short j = 0; j < nodeCount; ++j)
                    {
                        _TreeNodes[i][j].InitializeLeafNode((short)(j + ithBone.jMin), ithBone.dx);
                    }
                }
                /// preNodeCount
                preNodeCount = nodeCount;
            }
        }
        /// <summary>
        /// 計算済みの時点i-1ノードのQ値を用いてツリーによる割引債価格P(0,i)を算出&設定する.
        /// 引き続いて時点iに対する全ノードのQ値を計算する.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="priceDerivativeAlpha">ツリーによる割引債価格のalphaによる微分係数</param>
        /// <returns></returns>
        public double ComputeBondPrice(int i, out double priceDerivativeAlpha)
        {
            priceDerivativeAlpha = 0;
            if (i == 0)
            {
                _TreeBackBones[0].bondPrice = 1D;
                _TreeNodes[0][0].Q = 1D;
                /// priceDerivativeAlphaは使わないのでダミー0を返す
                return 1D;
            }
            /// i-1時点データ
            TreeBackBone pBone = _TreeBackBones[i - 1];
            TreeNode[] pNodes = _TreeNodes[i - 1];
            int pNodeCount = pBone.jMax - pBone.jMin + 1;
            /// BondPrice BK tree
            double bondPrice = 0;
            for (int j = 0; j < pNodeCount; ++j)
            {
                double commonTerm = ConvertToShortRate(pBone.alpha, pNodes[j].j * pBone.dx) * pBone.dt;
                double priceTerm = pNodes[j].Q * Math.Exp(-commonTerm);
                bondPrice += priceTerm;
                priceDerivativeAlpha += priceTerm * commonTerm;
            }
            /// BondPrice BK tree
            /// 価格の微分係数そのものにしておくため, マイナスをつける
            priceDerivativeAlpha = -priceDerivativeAlpha;
            /// i時点のQ値の計算
            /// i-1時点ノードを走査して, i時点の各ノードに対するQ値を一度に計算する
            TreeBackBone bone = _TreeBackBones[i];
            TreeNode[] nodes = _TreeNodes[i];
            int nodeCount = bone.jMax - bone.jMin + 1;
            /// i時点の各ノードに対するQ値の初期化
            for (int j = 0; j < nodeCount; ++j) { nodes[j].Q = 0; }
            /// Q値の計算
            for (int j = 0; j < pNodeCount; ++j)
            {
                /// down, mid, up nodeの順に集計
                int kIndex = pNodes[j].k - bone.jMin;
                /// BK tree
                double temp = pNodes[j].Q * Math.Exp(-ConvertToShortRate(pBone.alpha, pNodes[j].j * pBone.dx) * pBone.dt);
                nodes[kIndex - 1].Q += pNodes[j].pd * temp;
                nodes[kIndex].Q += pNodes[j].pm * temp;
                nodes[kIndex + 1].Q += pNodes[j].pu * temp;
            }
            bone.bondPrice = bondPrice;
            return bondPrice;
        }
        /// <summary>
        /// BK tree によるショートレートr換算処理
        /// </summary>
        /// <param name="a"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private double ConvertToShortRate(double a, double x)
        {
            return Math.Exp(a + x);
        }
        /// <summary>
        /// 時点iのalphaを時点i+1に対するpriceに合わせる
        /// </summary>
        /// <param name="i"></param>
        /// <param name="price"></param>
        /// <returns>収束誤差</returns>
        public double FitToInputBondPrice(int i, double price)
        {
            TreeBackBone bone = _TreeBackBones[i];
            if (i == 0)
            {
                bone.alpha = Math.Log(-Math.Log(price, Math.E) / bone.dt, Math.E);
                double dummy;
                ComputeBondPrice(0, out dummy);
                ComputeBondPrice(1, out dummy);
                return 0D;
            }
            /// Newtow法
            /// 誤差
            double error = 1e-15;
            /// alphaの初期値
            bone.alpha = 1D;
            /// treeにより算出する価格
            double priceByTree;
            do
            {
                /// priceByTree - priceに対するalphaの微分係数
                double derivative;
                /// 時点i+1の価格を計算して時点iのalphaを決定する
                priceByTree = ComputeBondPrice(i + 1, out derivative);
                bone.alpha = bone.alpha - (priceByTree - price) / derivative;
            } while (Math.Abs(priceByTree - price) > error);
            _TreeBackBones[i + 1].bondPrice = priceByTree;
            return Math.Abs(priceByTree - price);
        }
        /// <summary>
        /// 入力されたbondPriceに合わせるalpha[]を求め、rを設定する.
        /// </summary>
        /// <param name="bondPrices"></param>
        public void FitToInputBondPrice(double[] bondPrices){
            Debug.Assert(bondPrices.Length == _TreeBackBones.Length);
            /// alphaの算出
            for (int i = 0; i < _TreeBackBones.Length - 1; ++i)
            {
                FitToInputBondPrice(i, bondPrices[i+1]);
            }
            /// rの設定
            for (int i = 0; i < bondPrices.Length - 1; ++i)
            {
                TreeBackBone bone = _TreeBackBones[i];
                int jSize = bone.jMax - bone.jMin + 1;
                for (int j=0; j<jSize; ++j){
                    _TreeNodes[i][j].r = ConvertToShortRate(bone.alpha, _TreeNodes[i][j].j * bone.dx);
                } 
            }
        }
        public void OutputCsvTreeBackBones(string filepath)
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine(TreeBackBone.ToStringValuesHeader());
                for (int i = 0; i < _TreeBackBones.Length; ++i)
                {
                    sw.WriteLine(_TreeBackBones[i].ToStringValues());
                }
            }
        }
        public void OutputCsvTreeNodes(string filepath)
        {
            using (var sw = new System.IO.StreamWriter(filepath, false))
            {
                sw.WriteLine("i,{0}", TreeNode.ToStringValuesHeader());
                for (int i = 0; i < _TreeBackBones.Length; ++i)
                {
                    TreeBackBone bone = _TreeBackBones[i];
                    for (short j = 0; j < bone.jMax - bone.jMin + 1; ++j)
                    {
                        sw.Write("{0},", i);
                        sw.WriteLine(_TreeNodes[i][j].ToStringValues());
                    }
                }
            }
        }
    }
}
