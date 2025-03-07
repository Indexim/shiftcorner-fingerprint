using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace AbsensiCabang
{
    class Program
    {
        [DllImport("User32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow([In] IntPtr hWnd, [In] int nCmdShow);

        static void Main(string[] args)
        {
            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
            ShowWindow(handle, 6);

            string path = string.Empty;
            foreach (string arg in args)
            {
                if (arg.Contains("config")) path = arg.Replace("--config=", "");
            }

            Config config = (File.Exists(path)) ? ReadConfig.LoadJson(path) : ReadConfig.LoadJson();
            zkemkeeper.CZKEMClass axCZKEM1 = new zkemkeeper.CZKEMClass();

            ConnectMachine(axCZKEM1, config, new Log(path));
        }

        static void ConnectMachine(zkemkeeper.CZKEMClass axCZKEM1, dynamic config, Log log)
        {
            string dbConnect = config.DbInfo.dbConnection.ToString();
            string fingerId = config.FingerInfo.id.ToString();
            string fingerIp = config.FingerInfo.ip.ToString();
            string fingerPort = config.FingerInfo.port.ToString();
            int maxScan = config.FingerInfo.maxScan;
            int timeout = config.FingerInfo.timeout;

            log.WriteLog(fingerIp, "Starting...");

        kembali:
            int iMachineNumber = 1;

            #region proses absen
            try
            {
                #region baca mesin absen
                Boolean isConnected = axCZKEM1.Connect_Net(fingerIp, Convert.ToInt32(fingerPort));
                string sdwEnrollNumber = "";
                int idwVerifyMode = 0;
                int idwInOutMode = 0;
                int idwYear = 0;
                int idwMonth = 0;
                int idwDay = 0;
                int idwHour = 0;
                int idwMinute = 0;
                int idwSecond = 0;
                int idwWorkcode = 0;
                int idwErrorCode = 0;

                if (isConnected)
                {
                    // log.WriteLog(fingerIp, "Connection successful");
                    // in fact,when you are using the tcp/ip communication,this parameter will be ignored,that is any integer will all right.Here we use 1.
                    iMachineNumber = 1;
                    // here you can register the realtime events that you want to be triggered(the parameters 65535 means registering all)
                    axCZKEM1.RegEvent(iMachineNumber, 65535);
                    // disable the device
                    axCZKEM1.EnableDevice(iMachineNumber, false);

                    // read all the attendance records to the memory
                    if (axCZKEM1.ReadGeneralLogData(iMachineNumber))
                    {
                        #region looping data absen                       
                        DateTime startDate = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
                        DateTime absenDate = DateTime.Now;

                        string nik = "";
                        string inout = "";

                        // get records from memory
                        // log.WriteLog(fingerIp, "Read data finger...");
                        while (axCZKEM1.SSR_GetGeneralLogData(iMachineNumber, out sdwEnrollNumber, out idwVerifyMode, out idwInOutMode, out idwYear, out idwMonth, out idwDay, out idwHour, out idwMinute, out idwSecond, ref idwWorkcode))
                        {
                            var endDate = new DateTime(int.Parse(idwYear.ToString()), int.Parse(idwMonth.ToString()), int.Parse(idwDay.ToString()));
                            var dateDiff = endDate.Subtract(startDate);

                            if (dateDiff.TotalDays == 0)
                            {
                                nik = sdwEnrollNumber;
                                inout = idwInOutMode.ToString() == "0" ? "IN" : "OUT";
                                absenDate = DateTime.Parse(idwYear.ToString() + "-" + idwMonth.ToString() + "-" + idwDay.ToString() + " " + idwHour.ToString() + ":" + idwMinute.ToString() + ":" + idwSecond.ToString());
                            }
                        }

                        if (nik != "")
                        {
                            Database database = new Database(dbConnect);
                            ModelFinger finger = database.GetFinger(fingerIp).Result;
                            ModelAbsen absen = database.GetAbsen(nik).Result;
                            Boolean isPrint = false;
                            if (finger.IpMesinFinger == "" || finger.IpMesinPrint == "" || finger.IpMesinFinger == null || finger.IpMesinPrint == null)
                            {
                                log.WriteLog(fingerIp, $"Invalid ip fingerprint: {fingerIp}");
                            }
                            else if (absen.Nik != null)
                            {
                                if (maxScan > absen.FingerCount)
                                {
                                    TimeSpan selisih = (TimeSpan)(absenDate - absen.FingerDate);
                                    if (absen.FingerCount == 0 || selisih.TotalSeconds > timeout)
                                    {
                                        absen.StatusKerja = inout;
                                        absen.FingerIp = finger.IpMesinFinger;
                                        absen.PrintIp = finger.IpMesinPrint;
                                        absen.FingerCount++;

                                        _ = database.UpdateFinger(absen);
                                        isPrint = true;
                                        log.WriteLog(fingerIp, $"User has scanned: {nik}");
                                    }
                                    else
                                    {
                                        log.WriteLog(fingerIp, $"Scanning limited: {nik}, last: {absenDate}, scan: {absen.FingerCount}");
                                    }
                                }
                                else
                                {
                                    log.WriteLog(fingerIp, $"Scanning limited: {nik}, last: {absenDate}, scan: {absen.FingerCount} (max)");
                                }
                            }
                            else
                            {
                                absen.Nik = nik;
                                absen.NamaLengkap = "- NOT IN LINE UP -";
                                absen.StatusKerja = inout;
                                absen.FingerIp = finger.IpMesinFinger;
                                absen.PrintIp = finger.IpMesinPrint;
                                absen.PrintCount = 0;
                                absen.FingerCount = 1;

                                _ = database.InsertFinger(absen);
                                isPrint = true;
                                log.WriteLog(fingerIp, $"User has scanned: {nik} (NOT IN LINE UP)");
                            }

                            if (isPrint && absen.PrintCount < absen.FingerCount)
                            {
                                PrintOut1(database, absen, log);
                            }
                        }
                        else
                        {
                            log.WriteLog(fingerIp, "NIK not found");
                        }
                        #endregion
                    }
                    else
                    {
                        axCZKEM1.GetLastError(ref idwErrorCode);
                        if (idwErrorCode != 0)
                        {
                            log.WriteLog(fingerIp, $"Read data failed, ErrorCode: {idwErrorCode}");
                        }
                        else
                        {
                            log.WriteLog(fingerIp, "No data returned!");
                        }
                    }

                    axCZKEM1.EnableDevice(iMachineNumber, true);
                    axCZKEM1.Disconnect();
                }
                else
                {
                    log.WriteLog(fingerIp, "Connection failed");
                }
                #endregion

                isConnected = false;
                axCZKEM1.Disconnect();
            }
            catch (Exception e)
            {
                axCZKEM1.EnableDevice(iMachineNumber, true);
                axCZKEM1.Disconnect();

                log.WriteLog(fingerIp, e.Message.ToString());
            }
            #endregion

            goto kembali;
        }

        static void PrintOut1(Database database, ModelAbsen absen, Log log)
        {
            try
            {
                // konek printer menggunakan TCP
                using (TcpClient tcpClient = new TcpClient(absen.PrintIp, 9100))
                using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    var cmd = new StringBuilder();
                    cmd.Append("\x1b\x21\x30"); // quad area text
                    cmd.Append("\x1b\x61\x01"); // align center
                    cmd.Append("PT UNGGUL DINAMIKA UTAMA\n");
                    cmd.Append("\x1b\x21\x20"); // double width text
                    cmd.Append("site project Indexim\n");
                    cmd.Append("\x1b\x21\x10"); // double height text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x21\x00"); // normal text
                    cmd.Append($"{DateTime.Now:dd MMMM yyyy, HH:mm:ss}\n");
                    cmd.Append("\x1b\x21\x10"); // double height text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x61\x00"); // align left
                    cmd.Append("NIK       : " + (absen.Nik ?? "") + "\n");
                    cmd.Append("Nama      : " + (absen.NamaLengkap ?? "") + "\n");
                    cmd.Append("Shift     : " + (absen.Shift ?? "") + "\n");
                    cmd.Append("Unit      : " + (absen.Hauler ?? "") + "\n");
                    cmd.Append("Lokasi    : " + (absen.Loader ?? "") + "\n");
                    cmd.Append("Transport : " + (absen.Transportasi ?? "") + "\n");
                    cmd.Append("Tidur     : " + (absen.KategoriTidur ?? "") + "\n");
                    cmd.Append("\x1b\x61\x01"); // align center
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x21\x00"); // normal text
                    cmd.Append("Utamakan Keselamatan Kerja\n");
                    cmd.Append("Safety, Yes... Insiden, No...\n");
                    cmd.Append("Unggul... Bisa... Unggul... Luar Biasa\n");
                    cmd.Append("Unggul... Tetap Semangat\n");
                    cmd.Append("\x1b\x21\x10"); // double height text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1D\x6B\x04"); // barcode CODE39
                    cmd.Append($"{absen.Nik}\x00"); // nik as barcode data
                    cmd.Append("\x1B\x69"); // cut paper

                    byte[] rawData = Encoding.ASCII.GetBytes(cmd.ToString());
                    networkStream.Write(rawData, 0, rawData.Length);

                    _ = database.UpdatePrinter(absen);
                    log.WriteLog(absen.FingerIp, $"Absen has printed: {absen.Nik}, ip: {absen.PrintIp}");
                }
            }
            catch (Exception e)
            {
                log.WriteLog(absen.FingerIp, e.Message.ToString());
            }
        }

        static void PrintOut2(Database database, ModelAbsen absen, Log log)
        {
            try
            {
                // konek printer menggunakan TCP
                using (TcpClient tcpClient = new TcpClient(absen.PrintIp, 9100))
                using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    var cmd = new StringBuilder();
                    cmd.Append("\x1b\x21\x30"); // quad area text
                    cmd.Append("\x1b\x61\x01"); // align center
                    cmd.Append("PT UNGGUL DINAMIKA UTAMA\n");
                    cmd.Append("\x1b\x21\x20"); // double width text
                    cmd.Append("site project Indexim\n");
                    cmd.Append("\x1b\x21\x30"); // quad area text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x21\x10"); // double height text
                    cmd.Append($"{DateTime.Now:dd MMMM yyyy, HH:mm:ss}\n");
                    cmd.Append("\x1b\x21\x30"); // quad area text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x61\x00"); // align left
                    cmd.Append("NIK      : " + (absen.Nik ?? "") + "\n");
                    cmd.Append("Nama     : " + (absen.NamaLengkap ?? "") + "\n");
                    cmd.Append("Shift    : " + (absen.Shift ?? "") + "\n");
                    cmd.Append("Hauler   : " + (absen.Hauler ?? "") + "\n");
                    cmd.Append("Loader   : " + (absen.Loader ?? "") + "\n");
                    cmd.Append("Transport: " + (absen.Transportasi ?? "") + "\n");
                    cmd.Append("Tidur    : " + (absen.KategoriTidur ?? "") + "\n");
                    cmd.Append("\x1b\x61\x01"); // align center
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1b\x21\x10"); // double height text
                    cmd.Append("Utamakan Keselamatan Kerja\n");
                    cmd.Append("Safety, Yes... Insiden, No...\n");
                    cmd.Append("Unggul... Bisa... Unggul... Luar Biasa\n");
                    cmd.Append("Unggul... Tetap Semangat\n");
                    cmd.Append("\x1b\x21\x30"); // quad area text
                    cmd.Append("---------------------------------\n");
                    cmd.Append("\x1D\x6B\x04"); // barcode CODE39
                    cmd.Append($"{absen.Nik}\x00"); // nik as barcode data
                    cmd.Append("\x1B\x69"); // cut paper

                    byte[] rawData = Encoding.ASCII.GetBytes(cmd.ToString());
                    networkStream.Write(rawData, 0, rawData.Length);

                    _ = database.UpdatePrinter(absen);
                    log.WriteLog(absen.FingerIp, $"Absen has printed: {absen.Nik}, ip: {absen.PrintIp}");
                }
            }
            catch (Exception e)
            {
                log.WriteLog(absen.FingerIp, e.Message.ToString());
            }
        }
    }

}