using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;

namespace Microsoft.Build.Shared
{
    internal static class ExceptionHandling
    {
        internal static bool IsCriticalException(Exception e)
        {
            if (e is OutOfMemoryException || e is StackOverflowException || e is ThreadAbortException || e is ThreadInterruptedException || e is AccessViolationException /*|| e is InternalErrorException*/)
            {
                return true;
            }
            if (e is AggregateException ex && ex.InnerExceptions.Any((Exception innerException) => IsCriticalException(innerException)))
            {
                return true;
            }
            return false;
        }

        internal static bool NotExpectedException(Exception e)
        {
            return !IsIoRelatedException(e);
        }

        internal static bool IsIoRelatedException(Exception e)
        {
            if (!(e is UnauthorizedAccessException) && !(e is NotSupportedException) && (!(e is ArgumentException) || e is ArgumentNullException) && !(e is SecurityException))
            {
                return e is IOException;
            }
            return true;
        }

        internal static void Rethrow(this Exception e)
        {
            ExceptionDispatchInfo.Capture(e).Throw();
        }

        internal static void RethrowIfCritical(this Exception ex)
        {
            if (IsCriticalException(ex))
            {
                ex.Rethrow();
            }
        }
    }
}
