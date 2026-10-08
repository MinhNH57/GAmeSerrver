using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.KiemThe.Network.Data.Item
{
    /// <summary>
    /// Gói tin tặng quà
    /// </summary>
    [ProtoContract]
    public class SendGiftPet
    {
        /// <summary>
        /// DbID trang bị trong túi đồ
        /// </summary>
        [ProtoMember(1)]
        public int EquipDbID { get; set; }

        /// <summary>
        /// DbID các vật phẩm quà trong túi đồ
        /// </summary>
        [ProtoMember(2)]
        public List<int> GiftsDbID { get; set; }

    }
}
