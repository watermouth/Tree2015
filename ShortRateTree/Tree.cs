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
            _TreeBackBones = new TreeBackBone[GetTimeSeparationNumber()];
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
            Debug.Assert(a.Length == GetTimeSeparationNumber()
                && sigma.Length == GetTimeSeparationNumber());
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
        }
        /// <summary>
        /// ツリーノードの構築
        /// 遷移確率が0となるノードができる条件のとき、作成自体は行う。
        /// 作成しないようにすることももちろんできる。
        /// が、そのような条件自体が実用上はほぼないと思われる。
        /// また、後のフォワードインダクションやバックワードインダクションのときにも、
        /// 求めようとする値の初期化により支障なく計算可能である（はず）。
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
            /// i = 1, 2, ..., N - 1(Leaf Nodeに該当) 
            int preNodeCount = 1;
            for (short i = 1; i < GetTimeSeparationNumber(); ++i)
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
                if (i < GetTimeSeparationNumber() - 1)
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
