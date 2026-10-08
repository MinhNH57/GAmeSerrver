using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class C2G_ComposeDialog
    {
        [ProtoMember(1)] public int composeId;
        [ProtoMember(2)] public int sourceItemDBId;
        [ProtoMember(3)] public int rateUpItemDBId;
        [ProtoMember(4)] public int multiple;
        [ProtoMember(5)] public int insureItemDBId;
    }
}