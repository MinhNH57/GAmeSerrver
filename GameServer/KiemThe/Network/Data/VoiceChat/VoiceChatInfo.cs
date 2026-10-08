using ProtoBuf;
using UnityEditor;
using UnityEngine;

namespace GameServer.KiemThe.Network.Data.VoiceChat
{
    [ProtoContract]
    public class VoiceChatInfo
    {
        [ProtoMember(1)]
        public int OpenID { get; set; }

        [ProtoMember(2)]
        public string AppID { get; set; }

        [ProtoMember(3)]
        public string AuthKey { get; set; }

        [ProtoMember(4)]
        public string RoomID { get; set; }
    }
}