using System;
using System.IO;
namespace AbsensiCabang
{
    class Log
    {
        public string path;

        public Log(string path)
        {
            if (File.Exists(path)) this.path = path;
        }

        public void WriteLog(string subject, string desc)
        {
            if (!Directory.Exists(@"log"))
            {
                Directory.CreateDirectory(@"log");
            }

            string dateLog = DateTime.Now.ToString("yyyy-MM-dd");
            string timeLog = DateTime.Now.ToString("HH:mm:ss");
            string textLog = (desc != null) ? subject.ToString() + " | " + desc.ToString() : subject.ToString();

            StreamWriter log;
            if (!File.Exists(@"log\\" + dateLog.ToString() + "-finger.log"))
            {
                log = new StreamWriter(@"log\\" + dateLog.ToString() + "-finger.log");
            }
            else
            {
                log = File.AppendText(@"log\\" + dateLog.ToString() + "-finger.log");
            }

            Console.WriteLine($"{timeLog} | {textLog}");

            log.WriteLine($"{timeLog} | {textLog}");
            log.Close();
        }
    }
}
