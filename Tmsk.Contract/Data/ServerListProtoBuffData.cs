using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Tmsk.Contract
{
    /// <summary>
    /// 服务器信息
    /// </summary>
    [ProtoContract]
    public class BuffServerInfo
    {
        /// <summary>
        /// 服务器编号
        /// </summary>
        [ProtoMember(1)]
        public int nServerOrder { get; set; }

        /// <summary>
        /// 服务器ID
        /// </summary>
        [ProtoMember(2)]
        public int nServerID { get; set; }

        /// <summary>
        /// 服务器名称
        /// </summary>
        [ProtoMember(3)]
        public string strServerName { get; set; }

        /// <summary>
        /// 服务器状态
        /// </summary>
        [ProtoMember(4)]
        public int nStatus { get; set; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        [ProtoMember(5)]
        public string strURL { get; set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        [ProtoMember(6)]
        public int nServerPort { get; set; }

        [ProtoMember(7)]
        public string Msg { get; set; }

        //[ProtoMember(13)]
        //public string dbSvIp { get; set; }


        //[ProtoMember(14)]
        //public int nDbSvPort { get; set; }

        //[ProtoMember(15)]
        //public string logSvIp { get; set; }


        //[ProtoMember(16)]
        //public int nLogSvPort { get; set; }
    }

    /// <summary>
    /// GM工具请求服务器列表时，传过的参数数据
    /// </summary>
    [ProtoContract]
    public class ClientServerListData
    {
        /// <summary>
        /// 发送消息时的时间戳
        /// </summary>
        [ProtoMember(1)]
        public long lTime;

        /// <summary>
        /// 时间戳格式化成字符串后，与私钥生成的MD5码，以便作简单的验证。
        /// </summary>        
        [ProtoMember(2)]
        public string strMD5 = "";
    }

    /// <summary>
    /// 服务器列表数据
    /// </summary>
    [ProtoContract]
    public class ServerListData
    {
        /// <summary>
        /// 服务器列表数据
        /// </summary>
        [ProtoMember(1)]
        public string content = "";
    }

    /// <summary>
    /// 服务器列表数据
    /// </summary>
    [ProtoContract]
    public class BuffServerListData
    {
        /// <summary>
        /// 服务器列表数据
        /// </summary>
        [ProtoMember(1)]
        public BuffServerInfo[] ListServerData { get; set; }
    }
}
