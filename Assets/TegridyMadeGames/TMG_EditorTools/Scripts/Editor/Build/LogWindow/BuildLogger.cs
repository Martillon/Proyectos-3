namespace TMG_EditorTools
{

    public static class BuildLogger
    {
        private static string buildLogData = "";

        public static void AppendLog(string newLog)
        {
            buildLogData += newLog + "\n";
        }

        public static string GetBuildLog()
        {
            return buildLogData;
        }

        public static void SetLog(string newLog)
        {
            buildLogData = newLog;
        }
    }

}