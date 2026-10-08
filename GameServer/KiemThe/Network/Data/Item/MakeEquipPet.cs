using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace Server.Data
{
    /// <summary>
    /// Gói tin Cường hóa trang bị
    /// </summary>
    [ProtoContract]
    public class MakeEquipPet
    {
        /// <summary>
        /// DbID các vật phẩm Huyền Tinh trong túi đồ
        /// </summary>
        [ProtoMember(2)]
        public List<int> CrystalStonesDbID { get; set; }
    }
}
