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
