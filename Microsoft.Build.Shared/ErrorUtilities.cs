using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.Shared
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

        internal static void VerifyThrow(bool condition, string unformattedMessage)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, null, null);
            }
        }

        internal static void ThrowInternalError(string message, Exception innerException, params object[] args)
        {
            throw new Exception(message);
        }
    }
}
