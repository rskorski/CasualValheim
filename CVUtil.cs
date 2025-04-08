namespace CasualValheim
{
    static class CVUtil
    {
        public static void Log(string output, bool decorate = true, bool debug = true, bool stacktrace = false)
        {
            if( null != Mod.DbgLogEnabled &&
                !Mod.DbgLogEnabled.Value && 
                debug )
            {
                return;
            }

            if (decorate)
            {
                var header = debug ? "^^^^^DBG| ": " " ;
                output = $"@@@@@@{header}{output} @@@@@@";
            }

            if (stacktrace)
            {
                output += "\n";
                output += UnityEngine.StackTraceUtility.ExtractStackTrace();
            }


            Mod.logger.LogInfo(output);
        }
    }
}
