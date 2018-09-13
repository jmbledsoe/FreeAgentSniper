using System;
using System.IO;
using LiteDB;
using static System.Environment;

namespace FreeAgentSniper
{
    public static class LiteDbFactory
    {
        private static readonly string DB_DIRECTORY = Environment.GetFolderPath(SpecialFolder.ApplicationData);    

        public static LiteDatabase New()
        {
            return new LiteDatabase(Path.Combine(DB_DIRECTORY, $"{typeof(LiteDbFactory).Namespace}.db"));
        }
    }
}