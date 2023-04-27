using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Build.Shared;

namespace Microsoft.Build.Shared
{
    internal static class ErrorUtilities
    {
#if __
        private static readonly bool s_throwExceptions = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSBUILDDONOTTHROWINTERNAL"));

        private static readonly bool s_enableMSBuildDebugTracing = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MSBUILDENABLEDEBUGTRACING"));
#else
        private static readonly bool s_throwExceptions = true;
#endif
        public static void DebugTraceMessage(string category, string formatstring, params object[] parameters)
        {
#if __
            if (s_enableMSBuildDebugTracing)
            {
                if (parameters != null)
                {
                    Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, formatstring, parameters), category);
                }
                else
                {
                    Trace.WriteLine(formatstring, category);
                }
            }
#endif
        }

        internal static void VerifyThrowInternalError(bool condition, string message, params object[] args)
        {
            if (s_throwExceptions && !condition)
            {
                throw new Exception(message);
            }
        }

        internal static void ThrowInternalError(string message, params object[] args)
        {
            if (s_throwExceptions)
            {
                throw new Exception(message);
            }
        }

        internal static void ThrowInternalError(string message, Exception innerException, params object[] args)
        {
            if (s_throwExceptions)
            {
                throw new Exception(message, innerException);
            }
        }

        internal static void ThrowInternalErrorUnreachable()
        {
            if (s_throwExceptions)
            {
                throw new Exception("Unreachable?");
            }
        }

        internal static void VerifyThrowInternalErrorUnreachable(bool condition)
        {
            if (s_throwExceptions && !condition)
            {
                throw new Exception("Unreachable?");
            }
        }

        internal static void ThrowIfTypeDoesNotImplementToString(object param)
        {
        }

        internal static void VerifyThrowInternalNull(object parameter, string parameterName)
        {
            if (parameter == null)
            {
                ThrowInternalError("{0} unexpectedly null", parameterName);
            }
        }

        internal static void VerifyThrowInternalLockHeld(object locker)
        {
            if (!Monitor.IsEntered(locker))
            {
                ThrowInternalError("Lock should already have been taken");
            }
        }

        internal static void VerifyThrowInternalLength(string parameterValue, string parameterName)
        {
            VerifyThrowInternalNull(parameterValue, parameterName);
            if (parameterValue.Length == 0)
            {
                ThrowInternalError("{0} unexpectedly empty", parameterName);
            }
        }

        public static void VerifyThrowInternalLength<T>(T[] parameterValue, string parameterName)
        {
            VerifyThrowInternalNull(parameterValue, parameterName);
            if (parameterValue.Length == 0)
            {
                ThrowInternalError("{0} unexpectedly empty", parameterName);
            }
        }

        internal static void VerifyThrowInternalRooted(string value)
        {
            if (!Path.IsPathRooted(value))
            {
                ThrowInternalError("{0} unexpectedly not a rooted path", value);
            }
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, null, null);
            }
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage, object arg0)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, arg0);
            }
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage, object arg0, object arg1)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, arg0, arg1);
            }
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage, object arg0, object arg1, object arg2)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, arg0, arg1, arg2);
            }
        }

        internal static void VerifyThrow(bool condition, string unformattedMessage, object arg0, object arg1, object arg2, object arg3)
        {
            if (!condition)
            {
                ThrowInternalError(unformattedMessage, arg0, arg1, arg2, arg3);
            }
        }

        internal static void ThrowInvalidOperation(string resourceName, params object[] args)
        {
            if (s_throwExceptions)
            {
                throw new InvalidOperationException(resourceName);
            }
        }

        internal static void VerifyThrowInvalidOperation(bool condition, string resourceName)
        {
            if (!condition)
            {
                ThrowInvalidOperation(resourceName, null);
            }
        }

        internal static void VerifyThrowInvalidOperation(bool condition, string resourceName, object arg0)
        {
            if (!condition)
            {
                ThrowInvalidOperation(resourceName, arg0);
            }
        }

        internal static void VerifyThrowInvalidOperation(bool condition, string resourceName, object arg0, object arg1)
        {
            if (!condition)
            {
                ThrowInvalidOperation(resourceName, arg0, arg1);
            }
        }

        internal static void VerifyThrowInvalidOperation(bool condition, string resourceName, object arg0, object arg1, object arg2)
        {
            if (!condition)
            {
                ThrowInvalidOperation(resourceName, arg0, arg1, arg2);
            }
        }

        internal static void VerifyThrowInvalidOperation(bool condition, string resourceName, object arg0, object arg1, object arg2, object arg3)
        {
            if (!condition)
            {
                ThrowInvalidOperation(resourceName, arg0, arg1, arg2, arg3);
            }
        }

        internal static void ThrowArgument(string resourceName, params object[] args)
        {
            ThrowArgument(null, resourceName, args);
        }

        private static void ThrowArgument(Exception innerException, string resourceName, params object[] args)
        {
            if (s_throwExceptions)
            {
                throw new ArgumentException(resourceName, innerException);
            }
        }

        internal static void VerifyThrowArgument(bool condition, string resourceName)
        {
            VerifyThrowArgument(condition, null, resourceName);
        }

        internal static void VerifyThrowArgument(bool condition, string resourceName, object arg0)
        {
            VerifyThrowArgument(condition, null, resourceName, arg0);
        }

        internal static void VerifyThrowArgument(bool condition, string resourceName, object arg0, object arg1)
        {
            VerifyThrowArgument(condition, null, resourceName, arg0, arg1);
        }

        internal static void VerifyThrowArgument(bool condition, string resourceName, object arg0, object arg1, object arg2)
        {
            VerifyThrowArgument(condition, null, resourceName, arg0, arg1, arg2);
        }

        internal static void VerifyThrowArgument(bool condition, string resourceName, object arg0, object arg1, object arg2, object arg3)
        {
            VerifyThrowArgument(condition, null, resourceName, arg0, arg1, arg2, arg3);
        }

        internal static void VerifyThrowArgument(bool condition, Exception innerException, string resourceName)
        {
            if (!condition)
            {
                ThrowArgument(innerException, resourceName, null);
            }
        }

        internal static void VerifyThrowArgument(bool condition, Exception innerException, string resourceName, object arg0)
        {
            if (!condition)
            {
                ThrowArgument(innerException, resourceName, arg0);
            }
        }

        internal static void VerifyThrowArgument(bool condition, Exception innerException, string resourceName, object arg0, object arg1)
        {
            if (!condition)
            {
                ThrowArgument(innerException, resourceName, arg0, arg1);
            }
        }

        internal static void VerifyThrowArgument(bool condition, Exception innerException, string resourceName, object arg0, object arg1, object arg2)
        {
            if (!condition)
            {
                ThrowArgument(innerException, resourceName, arg0, arg1, arg2);
            }
        }

        internal static void VerifyThrowArgument(bool condition, Exception innerException, string resourceName, object arg0, object arg1, object arg2, object arg3)
        {
            if (!condition)
            {
                ThrowArgument(innerException, resourceName, arg0, arg1, arg2, arg3);
            }
        }

        internal static void ThrowArgumentOutOfRange(string parameterName)
        {
            if (s_throwExceptions)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        internal static void VerifyThrowArgumentOutOfRange(bool condition, string parameterName)
        {
            if (!condition)
            {
                ThrowArgumentOutOfRange(parameterName);
            }
        }

        internal static void VerifyThrowArgumentLength(string parameter, string parameterName)
        {
            VerifyThrowArgumentNull(parameter, parameterName);
            if (parameter.Length == 0 && s_throwExceptions)
            {
                throw new ArgumentException("Shared.ParameterCannotHaveZeroLength" + parameterName);
            }
        }

        internal static void VerifyThrowArgumentLength<T>(IReadOnlyCollection<T> parameter, string parameterName)
        {
            VerifyThrowArgumentNull(parameter, parameterName);
            if (parameter.Count == 0 && s_throwExceptions)
            {
                throw new ArgumentException("Shared.ParameterCannotHaveZeroLength" + parameterName);
            }
        }

        internal static void VerifyThrowArgumentLengthIfNotNull<T>(IReadOnlyCollection<T> parameter, string parameterName)
        {
            if (parameter != null && parameter.Count == 0 && s_throwExceptions)
            {
                throw new ArgumentException( "Shared.ParameterCannotHaveZeroLength" + parameterName);
            }
        }

        internal static void VerifyThrowArgumentLengthIfNotNull(string parameter, string parameterName)
        {
            if (parameter != null && parameter.Length == 0 && s_throwExceptions)
            {
                throw new ArgumentException("Shared.ParameterCannotHaveZeroLength" + parameterName);
            }
        }

        internal static void VerifyThrowArgumentNull(object parameter, string parameterName)
        {
            VerifyThrowArgumentNull(parameter, parameterName, "Shared.ParameterCannotBeNull");
        }

        internal static void VerifyThrowArgumentNull(object parameter, string parameterName, string resourceName)
        {
            if (parameter == null && s_throwExceptions)
            {
                throw new ArgumentNullException(resourceName + parameterName, (Exception?)null);
            }
        }

        internal static void VerifyThrowArgumentArraysSameLength(Array parameter1, Array parameter2, string parameter1Name, string parameter2Name)
        {
            VerifyThrowArgumentNull(parameter1, parameter1Name);
            VerifyThrowArgumentNull(parameter2, parameter2Name);
            if (parameter1.Length != parameter2.Length && s_throwExceptions)
            {
                throw new ArgumentException("Shared.ParametersMustHaveTheSameLength" + parameter1Name, parameter2Name);
            }
        }

        internal static void VerifyThrowObjectDisposed(bool condition, string objectName)
        {
            if (s_throwExceptions && !condition)
            {
                throw new ObjectDisposedException(objectName);
            }
        }
    }
}
