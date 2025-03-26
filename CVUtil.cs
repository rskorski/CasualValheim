namespace CasualValheim
{
    static class CVUtil
    {
        public static void Log(string output, bool decorate = true, bool debug = true)
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

            Mod.logger.LogInfo(output);
        }
    }
}
