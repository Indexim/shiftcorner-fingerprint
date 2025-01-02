using System;
using System.IO;
using Newtonsoft.Json;

namespace AbsensiCabang
{
    public class Config
    {
        public DbInfo DbInfo { get; set; }
        public FingerInfo FingerInfo { get; set; }
    }

    public class DbInfo
    {
        public string dbConnection { get; set; }
    }
    public class FingerInfo
    {
        public FingerInfo() { port = 4370; }
        public string id { get; set; }
        public string ip { get; set; }
        public Int32 port { get; set; }
        public string printerId { get; set; }
        public Int32 maxScan { get; set; }
        public Int32 timeout { get; set; }
    }

    public class ReadConfig
    {
        public string Load { get; set; }

        public static Config LoadJson(string file = "conf\\config.json")
        {
            string path = Path.GetFullPath(file);
            StreamReader sr = File.OpenText(path);
            var json = sr.ReadToEnd();
            var result = JsonConvert.DeserializeObject<Config>(json);
            return result;
        }
    }
}
