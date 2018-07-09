using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Helpers
{
    public static partial class Converters
    {
        public static DateTime TimexToDateConverter(string timex)
        {
            //convert Timex to Date
            var justDate = timex.Substring(0, timex.IndexOf("T"));
            var date = Convert.ToDateTime(justDate);

            return date;
        }
    }
}
