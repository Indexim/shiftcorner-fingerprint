using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace AbsensiCabang
{
    public class Database
    {
        private readonly string dbConnect;

        public Database(string dbConnect)
        {
            this.dbConnect = dbConnect;
        }

        public async Task<ModelAbsen> GetAbsen(string nik)
        {
            try
            {
                using (var conn = new NpgsqlConnection(dbConnect))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand("SELECT * FROM tbl_absen WHERE tanggal=NOW()::date AND nik='" + nik + "' LIMIT 1", conn))
                    using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read())
                        {
                            var model = new ModelAbsen();
                            model.Tanggal = reader.GetDateTime(0);
                            model.Nik = reader.GetString(1);
                            model.NamaLengkap = reader.GetString(2);
                            model.Jabatan = reader.GetValue(3).ToString();
                            model.Departemen = reader.GetValue(4).ToString();
                            model.Shift = reader.GetValue(5).ToString();
                            model.Hauler = reader.GetValue(6).ToString();
                            model.Loader = reader.GetValue(7).ToString();
                            model.Transportasi = reader.GetValue(8).ToString();
                            model.StatusKerja = reader.GetValue(9).ToString();
                            model.KategoriTidur = reader.GetValue(10).ToString();
                            model.FingerCount = (reader.IsDBNull(11)) ? 0 : reader.GetInt16(11);
                            model.FingerDate = (reader.IsDBNull(12)) ? DateTime.Now : reader.GetDateTime(12);
                            model.FingerId = (reader.IsDBNull(13)) ? "" : reader.GetValue(13).ToString();
                            model.FingerIp = (reader.IsDBNull(14)) ? "" : reader.GetValue(14).ToString();
                            model.PrintCount = (reader.IsDBNull(15)) ? 0 : reader.GetInt16(15);
                            model.PrintDate = (reader.IsDBNull(16)) ? DateTime.Now : reader.GetDateTime(16);
                            model.PrintId = (reader.IsDBNull(17)) ? "" : reader.GetValue(17).ToString();
                            model.PrintIp = (reader.IsDBNull(18)) ? "" : reader.GetValue(18).ToString();
                            return model;
                        }
                    }

                    conn.Close();
                }

                return new ModelAbsen();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message GetAbsen: {ex.Message}");
                throw;
            }
        }

        public async Task<ModelFinger> GetFinger(string ip)
        {
            try
            {
                using (var conn = new NpgsqlConnection(dbConnect))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand("SELECT ip_mesin_finger, ip_mesin_print, nama_printer FROM tbl_m_absen_to_finger WHERE ip_mesin_finger='" + ip + "' LIMIT 1", conn))
                    using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read())
                        {
                            var model = new ModelFinger();
                            model.IpMesinFinger = reader.GetString(0);
                            model.IpMesinPrint = reader.GetString(1);
                            model.NamaPrinter = reader.GetString(2);
                            return model;
                        }
                    }

                    conn.Close();
                }

                return new ModelFinger();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message GetFinger: {ex.Message}");
                throw;
            }
        }

        public async Task UpdatePrinter(ModelAbsen absen)
        {
            try
            {
                var conn = new NpgsqlConnection(dbConnect);
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand("UPDATE tbl_absen SET " +
                    "print_date=NOW()," +
                    "print_count=finger_count " +
                    "WHERE tanggal=NOW()::date AND nik='" + absen.Nik + "'", conn);
                await cmd.ExecuteNonQueryAsync();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message UpdatePrinter: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateFinger(ModelAbsen absen)
        {
            try
            {
                var conn = new NpgsqlConnection(dbConnect);
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand("UPDATE tbl_absen SET " +
                    "finger_date=NOW()," +
                    "finger_count='" + absen.FingerCount + "'," +
                    "finger_id='" + absen.FingerId + "'," +
                    "finger_ip='" + absen.FingerIp + "'," +
                    "print_id='" + absen.PrintId + "'," +
                    "print_ip='" + absen.PrintIp + "'," +
                    "status_kerja='" + absen.StatusKerja + "'" +
                    "WHERE tanggal=NOW()::date AND nik='" + absen.Nik + "'", conn);
                await cmd.ExecuteNonQueryAsync();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message UpdateFinger: {ex.Message}");
                throw;
            }
        }

        public async Task InsertFinger(ModelAbsen absen)
        {
            try
            {
                var conn = new NpgsqlConnection(dbConnect);
                await conn.OpenAsync();
                var cmd = new NpgsqlCommand("INSERT INTO tbl_absen (tanggal, nik, nama_lengkap, finger_count, finger_date, finger_id, finger_ip, print_id, print_ip, status_kerja) " +
                    "VALUES (NOW()::date, '" + absen.Nik + "', '" + absen.NamaLengkap + "', 1, NOW(), '" + absen.FingerId + "', '" + absen.FingerIp + "', '" + absen.PrintId + "', '" + absen.PrintIp + "', '" + absen.StatusKerja + "')", conn);
                await cmd.ExecuteNonQueryAsync();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error message InsertFinger: {ex.Message}");
                throw;
            }
        }
    }
}
