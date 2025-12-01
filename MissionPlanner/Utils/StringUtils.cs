using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MissionPlanner.Utils
{
    public static class StringExtensions
    {
        public static string TrimAll(this string s)
        {
            var s1 = s.Trim();
            if (s1.EndsWith("^N"))
                s1 = s1.Substring(0, s1.Length - 2);
            return s1;
        }

    }
}
