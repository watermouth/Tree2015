using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortRateTree
{
    /// <summary>
    /// 44Byte
    /// </summary>
    public class TreeNode
    {
        public short j;
        public short k;
        public double pu;
        public double pm;
        public double pd;
        public double Q;
        public double r;

        /// <summary>
        /// 初期化 : 遷移に関する量を設定する
        /// </summary>
        /// <param name="j"></param>
        /// <param name="dx"></param>
        /// <param name="expMinusADeltaT"></param>
        /// <param name="dxForNextTime"></param>
        /// <param name="V"></param>
        /// <param name="V2"></param>
        public void Initialize(short j, double dx, double expMinusADeltaT, double dxForNextTime, double V, double V2 )
        {
            this.j = j;
            double x = j * dx;
            double m = x * expMinusADeltaT;
            /// dxとdxForNextTimeのdouble精度外の誤差の影響を小さくするためdx/dxForNextTimeを明示的に計算する
            this.k = (short)Math.Round(j * expMinusADeltaT * dx / dxForNextTime, MidpointRounding.AwayFromZero);
            double eta = m - x;
            double etaSquaredOverSixTimesVSquared = eta * eta / (6 * V2);
            double etaOverTwoTimesVTimesSqrt3 = eta / (2 * Math.Sqrt(3) * V);
            this.pu = (1D / 6D) + etaSquaredOverSixTimesVSquared + etaOverTwoTimesVTimesSqrt3; 
            this.pm = (2D / 3D) - 2 * etaSquaredOverSixTimesVSquared;
            this.pd = (1D / 6D) + etaSquaredOverSixTimesVSquared - etaOverTwoTimesVTimesSqrt3;
        }
        /// <summary>
        /// 最終時点ノード用初期化
        /// </summary>
        /// <param name="j"></param>
        /// <param name="dx"></param>
        public void InitializeLeafNode(short j, double dx)
        {
            this.j = j;
            double x = j * dx;
        }
        public static string ToStringValuesHeader()
        {
            return string.Format("j,k,pu,pm,pd,Q,r");
        }
        public string ToStringValues()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", j, k, pu, pm, pd, Q, r); 
        }
    }
}
