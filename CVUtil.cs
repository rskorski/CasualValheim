using System;
using BepInEx.Logging;

namespace CasualValheim
{
    static class CVUtil
    {
        public static void Log(string output, bool decorate = true, bool stacktrace = false, LogLevel level = LogLevel.Message)
        {
            if (decorate)
            {
                var header = (0 != (level & LogLevel.Debug) ) ? "^^^^^DBG| ": " " ;
                output = $"@@@@@@{header}{output} @@@@@@";
            }

            if (stacktrace)
            {
                output += System.Environment.NewLine;
                output += UnityEngine.StackTraceUtility.ExtractStackTrace();
            }


            Mod.logger.Log(level, output);
        }
        public static void LogError(string output, bool decorate = true, bool stacktrace = true)
        {
            Log(output, decorate, stacktrace, LogLevel.Error);
        }
        public static void LogWarning(string output, bool decorate = true, bool stacktrace = false)
        {
            Log(output, decorate, stacktrace, LogLevel.Warning);
        }
        public static void LogInfo(string output, bool decorate = true, bool stacktrace = false)
        {
            Log(output, decorate, stacktrace, LogLevel.Info);
        }
        public static void LogDebug(string output, bool decorate = true, bool stacktrace = false)
        {
            Log(output, decorate, stacktrace, LogLevel.Debug);
        }

        // our own logging category ontop of BepInEx's.  This is intended for very noisy logs (eg: in update functions)
        // or for logs that have expensive processing, where you can use the Func flavor to skip that processing when verbose is not enabled.
        public static void LogVerbose(string output, bool decorate = true, bool stacktrace = false)
        {
            if( isVerboseEnabled )
            {
                LogDebug(output, decorate, stacktrace);
            }
        }                                                         
        public static void LogVerbose(Func<string> func, bool decorate = true, bool stacktrace = false)
        {
            if( isVerboseEnabled )
            {
                LogDebug(func(), decorate, stacktrace);
            }
        }

        // LogDebug can be used to dump information that is sometimes expensive to collect, so calling code may want to know
        // if that processing will even make it out to the log before doing so.  Likely not necessary with the LogVerbose(Func<>) function,
        // but there's no good reason to keep read access proviate.
        // NOTE: The state is cached in CVUtils, so a reload of the config would not apply.  CV doesn't play into config reloading yet as a whole.
        public static bool isVerboseEnabled { get; private set; } = false;

        // isVerboseEnabled is intended to be set once by the boilerplate setup, putting the value set behind an Init function helps communicate that.
        public static void LoggingInit(bool enableVerbose)
        {
            isVerboseEnabled = enableVerbose;
           
        }
    }
}
