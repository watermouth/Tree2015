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
        public TreeNode[,] _TreeNodes;

        public Tree(double[] times)
        {
            Debug.Assert(times != null && times.Length > 1);
            _times = (double[])times.Clone();
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

        public void SetUpBackBones(double[] a, double[] sigma)
        {
            Debug.Assert(a.Length == GetTimeSeparationNumber()
                && sigma.Length == GetTimeSeparationNumber());
            /// 0, 1, ..., N 時点 分割数はN : N分割するとき、back boneはN個用いる
            _TreeBackBones = new TreeBackBone[GetTimeSeparationNumber()];
            /// i = 0
            _TreeBackBones[0].dt = _times[1] - _times[0];
            _TreeBackBones[0].dx = 0;
            _TreeBackBones[0].V = ComputeV(a[0], sigma[0], _TreeBackBones[0].dt);
            /// i = 1, ..., N
            for (int i = 1; i < GetTimeSeparationNumber(); ++i)
            {
                _TreeBackBones[i].dt = _times[i + 1] - _times[i];
                _TreeBackBones[i].dx = _TreeBackBones[i - 1].V * Math.Sqrt(3);
                _TreeBackBones[i].V = ComputeV(a[i], sigma[i], _TreeBackBones[i].dt);
            }
        }
    }
}
