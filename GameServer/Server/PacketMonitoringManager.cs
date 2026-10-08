using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using GameServer.Logic;
using Server.Tools;

namespace GameServer.Server
{
    /// <summary>
    /// [DEBUG] Quản lý danh sách RoleID cần log toàn bộ packet để điều tra bug.
    /// <para>Đọc RoleID từ file <c>Config/KT_Setting/PacketMonitoringUsers.xml</c> (trong thư mục tài nguyên game).</para>
    /// <para>File không tồn tại HOẶC danh sách rỗng => KHÔNG log gì.</para>
    /// <para>Dùng lệnh console <c>reloadPacketMonitor</c> để nạp lại file khi server đang chạy.</para>
    /// <para>Lệnh GM <c>PacketMonitorAdd/PacketMonitorRemove/PacketMonitorList</c> thêm/xóa tạm thời
    /// trong RAM (KHÔNG ghi vào file XML — restart hoặc reload sẽ lấy lại danh sách từ file).</para>
    /// </summary>
    public static class PacketMonitoringManager
    {
        /// <summary>
        /// Khóa tuần tự hóa các thao tác GHI (Add/Remove) — đọc vẫn lock-free qua volatile reference.
        /// </summary>
        private static readonly object _writeLock = new object();
        /// <summary>
        /// Đường dẫn tương đối (trong tài nguyên game) tới file cấu hình danh sách RoleID cần theo dõi packet.
        /// </summary>
        private const string ConfigResUri = "Config/KT_Setting/PacketMonitoringUsers.xml";

        /// <summary>
        /// Danh sách RoleID đang theo dõi.
        /// Dùng volatile reference tới HashSet bất biến để đọc lock-free trên hot path (mỗi packet).
        /// Khi reload thì thay nguyên reference bằng HashSet mới.
        /// </summary>
        private static volatile HashSet<int> _monitoredRoleIds = new HashSet<int>();

        /// <summary>
        /// True nếu không có RoleID nào cần theo dõi (dùng để short-circuit sớm, tránh lookup player).
        /// </summary>
        public static bool IsEmpty
        {
            get { return _monitoredRoleIds.Count == 0; }
        }

        /// <summary>
        /// Số lượng RoleID đang theo dõi.
        /// </summary>
        public static int Count
        {
            get { return _monitoredRoleIds.Count; }
        }

        /// <summary>
        /// Kiểm tra RoleID có nằm trong danh sách theo dõi không.
        /// </summary>
        public static bool IsMonitored(int roleID)
        {
            HashSet<int> set = _monitoredRoleIds;
            return set.Count > 0 && set.Contains(roleID);
        }

        /// <summary>
        /// Thêm RoleID vào danh sách theo dõi (chỉ trong RAM, không ghi file XML).
        /// Copy-on-write: tạo HashSet mới rồi thay reference để đọc hot path không cần lock.
        /// </summary>
        /// <returns>True nếu thêm mới, False nếu đã có sẵn hoặc roleID không hợp lệ.</returns>
        public static bool Add(int roleID)
        {
            if (roleID <= 0)
            {
                return false;
            }

            lock (_writeLock)
            {
                if (_monitoredRoleIds.Contains(roleID))
                {
                    return false;
                }

                HashSet<int> newSet = new HashSet<int>(_monitoredRoleIds) { roleID };
                _monitoredRoleIds = newSet;
                return true;
            }
        }

        /// <summary>
        /// Xóa RoleID khỏi danh sách theo dõi (chỉ trong RAM, không ghi file XML).
        /// </summary>
        /// <returns>True nếu xóa được, False nếu không có trong danh sách.</returns>
        public static bool Remove(int roleID)
        {
            lock (_writeLock)
            {
                if (!_monitoredRoleIds.Contains(roleID))
                {
                    return false;
                }

                HashSet<int> newSet = new HashSet<int>(_monitoredRoleIds);
                newSet.Remove(roleID);
                _monitoredRoleIds = newSet;
                return true;
            }
        }

        /// <summary>
        /// Trả về bản sao danh sách RoleID đang theo dõi (để hiển thị, không dùng trên hot path).
        /// </summary>
        public static List<int> Snapshot()
        {
            return new List<int>(_monitoredRoleIds);
        }

        /// <summary>
        /// Nạp (hoặc nạp lại) danh sách RoleID từ file XML.
        /// File không tồn tại / rỗng / lỗi => danh sách rỗng (không log gì).
        /// </summary>
        /// <returns>Số RoleID đã nạp.</returns>
        public static int Load()
        {
            HashSet<int> result = new HashSet<int>();

            // Danh sách chỉ nạp được sau khi AbsoluteGameResPath đã được khởi tạo (InitGameResPath).
            // Gọi sớm hơn => GameResPath() trả về đường dẫn cụt "/Config/..." => im lặng nạp 0 RoleID.
            if (string.IsNullOrEmpty(Global.AbsoluteGameResPath))
            {
                SysConOut.WriteLine("[PacketMonitor] Bỏ qua nạp: thư mục tài nguyên game chưa được khởi tạo.");
                _monitoredRoleIds = result;
                return 0;
            }

            string filePath = Global.GameResPath(ConfigResUri);
            bool fileExists = File.Exists(filePath);
            try
            {
                if (fileExists)
                {
                    XElement root = XElement.Load(filePath);
                    // Chấp nhận cả <RoleID>123</RoleID> lẫn <User RoleID="123" />
                    foreach (XElement el in root.Elements("RoleID"))
                    {
                        if (int.TryParse((el.Value ?? "").Trim(), out int rid) && rid > 0)
                        {
                            result.Add(rid);
                        }
                    }
                    foreach (XElement el in root.Elements("User"))
                    {
                        XAttribute attr = el.Attribute("RoleID");
                        if (attr != null && int.TryParse((attr.Value ?? "").Trim(), out int rid) && rid > 0)
                        {
                            result.Add(rid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.Error, string.Format("[PacketMonitor] Lỗi đọc {0}: {1}", filePath, ex));
                result = new HashSet<int>(); // lỗi => coi như rỗng, không log
            }

            _monitoredRoleIds = result;

            // Ghi ra console: log mức Info bị lọc mất khi AppConfig đặt LogType cao hơn (mặc định LogType=2).
            // Phân biệt rõ "không có file" với "file rỗng" để không im lặng nạp 0 RoleID.
            if (!fileExists)
            {
                SysConOut.WriteLine(string.Format("[PacketMonitor] Không tìm thấy {0} => không log packet.", filePath));
            }
            else
            {
                SysConOut.WriteLine(string.Format("[PacketMonitor] Đã nạp {0} RoleID cần theo dõi packet từ {1}.", result.Count, filePath));
            }

            LogManager.WriteLog(LogTypes.Info, string.Format("[PacketMonitor] Đã nạp {0} RoleID cần theo dõi packet từ {1}.", result.Count, ConfigResUri));
            return result.Count;
        }
    }
}
