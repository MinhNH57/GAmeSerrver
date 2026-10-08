using System.Collections.Generic;
using ProtoBuf;

namespace Server.Data
{
    [ProtoContract]
    public class G2C_ComposeDialog
    {
        [ProtoMember(1)] public string template;
        [ProtoMember(2)] public List<int> compose_ids;
    }
}