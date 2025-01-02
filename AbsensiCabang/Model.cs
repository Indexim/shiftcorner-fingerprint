using System;

namespace AbsensiCabang
{
    public class ModelAbsen
    {
        public DateTime? Tanggal { get; set; }
        public string Nik { get; set; }
        public string NamaLengkap { get; set; }
        public string Jabatan { get; set; }
        public string Departemen { get; set; }
        public string Shift { get; set; }
        public string Hauler { get; set; }
        public string Loader { get; set; }
        public string Transportasi { get; set; }
        public string StatusKerja { get; set; }
        public string KategoriTidur { get; set; }
        public int? FingerCount { get; set; }
        public DateTime? FingerDate { get; set; }
        public string FingerId { get; set; }
        public string FingerIp { get; set; }
        public int? PrintCount { get; set; }
        public DateTime? PrintDate { get; set; }
        public string PrintId { get; set; }
        public string PrintIp { get; set; }
    }

    public class ModelFinger
    {
        public string IpMesinFinger { get; set; }
        public string IpMesinPrint { get; set; }
        public string NamaPrinter { get; set; }
    }
}
