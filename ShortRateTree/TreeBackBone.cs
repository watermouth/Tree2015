using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortRateTree
{
    /// <summary>
    /// 52Byte
    /// </summary>
    public class TreeBackBone
    {
        public double t;
        public double a;
        public double sigma;
        public double V;
        public double dx;
        public double dt;
        public double alpha;
        public short jMin;
        public short jMax;
        public double bondPrice;
        public static string ToStringValuesHeader()
        {
            return string.Format("t,a,sigma,dt,dx,V,alpha,jMin,jMax,bondPrice");
        }
        public string ToStringValues()
        {
            return string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}",
                t, a, sigma, dt, dx, V, alpha, jMin, jMax, bondPrice);
        }
    }
}
