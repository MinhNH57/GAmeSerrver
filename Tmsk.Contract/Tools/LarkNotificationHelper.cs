using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameServer.Core.Executor;
using RestEase;
using Server.Tools;
using Newtonsoft.Json;

namespace GameServer.Tools
{

    [Header("Content-Type", "application/json")]
    public interface ILarkApi
    {
        [Header("Authorization")]
        string AccessToken { get; set; }
        
        [Post("messages?receive_id_type=chat_id")]
        Task PostMessageAsync([Body]string message);
    }
    
    public static class LarkNotificationHelper
    {
        private const string EXE_GROUP_ID = "oc_da5e9d7ebba68f38c88b126153b580ff";
        private const string DEV_GROUP_ID = "oc_f820b821589c561f8123206366d2182f";
        private static string LarkToken = "";
        private const string LARK_API_URL = "https://open.larksuite.com/open-apis/im/v1/";

        private static long LastDevTick = 0;

        public static void UpdateLarkToken(string token)
        {
            LarkToken = token;
        }

        public static void PushNormalNotification(string message)
        {
            PushNotificationToGroup(EXE_GROUP_ID, message);
        }

        public static void PushDevNotification(string message)
        {
            var currentTick = TimeUtil.NOW();
            var timeSpan = new TimeSpan(currentTick - LastDevTick).TotalSeconds;
            
            // 10s mới cho gửi 1 lần
            if (timeSpan < 10)
                return;
            
            PushNotificationToGroup(DEV_GROUP_ID, message);
            LastDevTick = currentTick;
        }

        private static void PushNotificationToGroup(string groupId, string message)
        {
            try
            {
                var content = new Dictionary<string, string>()
                {
                    { "text", message }
                };
                var body = new Dictionary<string, string>
                {
                    { "receive_id",  groupId},
                    { "msg_type", "text" },
                    { "content", JsonConvert.SerializeObject(content) }
                };
                var bodyJson = JsonConvert.SerializeObject(body);
                var api = RestClient.For<ILarkApi>(LARK_API_URL);
                api.AccessToken = $"Bearer {LarkToken}";
                api.PostMessageAsync(bodyJson);
            }
            catch (Exception ex)
            {
                SysConOut.WriteLine(ex.ToString());
            }
        }
    }
}