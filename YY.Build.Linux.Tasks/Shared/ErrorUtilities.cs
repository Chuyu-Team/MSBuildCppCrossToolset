using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YY.Build.Linux.Tasks.Shared
{
    public class ErrorUtilities
    {
        internal static void VerifyThrow(bool condition, string unformattedMessage, object arg0)
        {
            if (!condition)
            {
                throw new Exception(unformattedMessage);
            }
        }
    }
}
