using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortRateTree
{
    public class Cashflow
    {
        public DateTime ResetDate;
        public DateTime SettlementDate;
        public double SwapRate;

        public Cashflow(){}
        public Cashflow(DateTime resetDate, DateTime settlementDate, double swapRate)
        {
            SetCashflow(resetDate, settlementDate, swapRate);
        }
        public void SetCashflow(DateTime resetDate, DateTime settlementDate, double swapRate)
        {
            ResetDate = resetDate;
            SettlementDate = settlementDate;
            SwapRate = swapRate;
        }
        public static string ToStringValuesHeader()
        {
            return string.Format("ResetDate,SettlementDate,SwapRate");
        }
        public string ToStringValues()
        {
            return string.Format("{0},{1},{2}", ResetDate, SettlementDate, SwapRate);
        }
    }
}
