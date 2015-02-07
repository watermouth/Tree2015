using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortRateTree
{
    /// <summary>
    /// 32Byte
    /// </summary>
    public class TreeBackBone
    {
        public double V;
        public double dx;
        public double dt;
        public double alpha;

        public static string ToStringValuesHeader()
        {
            return string.Format("dt,dx,V,alpha");
        }
        public string ToStringValues()
        {
            return string.Format("{0}, {1}, {2}, {3}", dt, dx, V, alpha);
        }
    }
}
