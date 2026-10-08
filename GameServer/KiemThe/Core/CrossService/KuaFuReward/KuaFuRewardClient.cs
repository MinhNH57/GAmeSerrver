using GameServer.KiemThe;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;
using GameServer.Server;
using KF.Contract;
using KF.Contract.Data;
using KF.Contract.Interface;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml.Linq;
using Tmsk.Contract;

namespace KF.Client
{
    public class KuaFuRewardClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        private const int MaxPickCount = 20;
        private const string TemplateConfigPath = "Config/KT_GameEvents/KuaFuRewardMailTemplates.xml";

        private static readonly KuaFuRewardClient Instance = new KuaFuRewardClient();
        private static readonly object TemplateMutex = new object();
        private static Dictionary<string, RewardMailText> MailTemplates;

        private readonly object _mutex = new object();
        private readonly object _remotingMutex = new object();

        private ICoreInterface _coreInterface;
        private KuaFuClientContext _clientInfo = new KuaFuClientContext();
        private IKuaFuRewardService _rewardService;
        private string _remoteServiceUri;

        private class RewardMailText
        {
            public string Title { get; set; }

            public string Content { get; set; }
        }

        public static KuaFuRewardClient getInstance()
        {
            return Instance;
        }

        public bool initialize(ICoreInterface coreInterface)
        {
            _coreInterface = coreInterface;
            _clientInfo.ServerId = _coreInterface.GetLocalServerId();
            _clientInfo.GameType = (int)GameTypes.KuaFuReward;
            _clientInfo.Token = _coreInterface.GetLocalAddressIPs();
            return true;
        }

        public bool startup()
        {
            return true;
        }

        public bool showdown()
        {
            return true;
        }

        public bool destroy()
        {
            return true;
        }

        public void EventCallBackHandler(int eventType, params object[] args)
        {
        }

        public object GetDataFromClientServer(int dataType, params object[] args)
        {
            return null;
        }

        public int GetNewFuBenSeqId()
        {
            return 0;
        }

        public int UpdateRoleData(KuaFuRoleData kuaFuRoleData, int roleId = 0)
        {
            return 0;
        }

        public int OnRoleChangeState(int roleId, int state, int age)
        {
            return 0;
        }

        public void TimerProc(object sender, EventArgs e)
        {
            try
            {
                RefreshRemoteUri();
                IKuaFuRewardService service = GetRewardService();
                if (service == null)
                {
                    return;
                }

                ProcessPendingMailRewards(service);
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, "KuaFuRewardClient TimerProc loi: " + ex);
                ResetRewardService();
            }
        }

        public KuaFuRewardSubmitResult SubmitMailReward(KuaFuRewardMailData reward)
        {
            try
            {
                IKuaFuRewardService service = GetRewardService();
                if (service == null)
                {
                    return new KuaFuRewardSubmitResult
                    {
                        Success = false,
                        Message = "Khong ket noi duoc KuaFuRewardService",
                    };
                }

                return service.SubmitMailReward(reward);
            }
            catch (Exception ex)
            {
                ResetRewardService();
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Submit reward loi. RewardKey={0}, Error={1}", reward != null ? reward.RewardKey : "null", ex));
                return new KuaFuRewardSubmitResult
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
        }

        private void ProcessPendingMailRewards(IKuaFuRewardService service)
        {
            KuaFuRewardMailData[] rewards = service.PickPendingMailRewards(GameManager.ServerId, MaxPickCount);
            if (rewards == null || rewards.Length <= 0)
            {
                return;
            }

            foreach (KuaFuRewardMailData reward in rewards)
            {
                if (reward == null)
                {
                    continue;
                }

                string error;
                int mailId = SendLocalMail(reward, out error);
                if (mailId > 0)
                {
                    service.ConfirmMailRewardSent(reward.Id, GameManager.ServerId, mailId);
                    LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Gui reward mail thanh cong. RewardID={0}, RewardKey={1}, RoleID={2}, MailID={3}", reward.Id, reward.RewardKey, reward.RoleId, mailId));
                }
                else
                {
                    service.ConfirmMailRewardFailed(reward.Id, GameManager.ServerId, error);
                    LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Gui reward mail that bai. RewardID={0}, RewardKey={1}, RoleID={2}, Error={3}", reward.Id, reward.RewardKey, reward.RoleId, error));
                }
            }
        }

        private int SendLocalMail(KuaFuRewardMailData reward, out string error)
        {
            error = string.Empty;
            RewardMailText mailText;
            try
            {
                mailText = BuildMailText(reward, out error);
            }
            catch (Exception ex)
            {
                error = "LOAD_TEMPLATE_ERROR:" + ex.Message;
                return 0;
            }

            if (mailText == null)
            {
                return 0;
            }

            string strDbCmd = string.Format(
                "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}",
                -1,
                "Hệ thống",
                reward.RoleId,
                reward.RoleId.ToString(),
                mailText.Title,
                mailText.Content,
                reward.BoundMoney,
                reward.BoundToken,
                reward.MailGoods ?? string.Empty);

            LogManager.WriteLog(LogTypes.Mail, string.Format("[KuaFuReward]-[RoleID:{0}][RewardKey:{1}][Template:{2}]-[{3}] [items:{4}]", reward.RoleId, reward.RewardKey, reward.TemplateId, mailText.Content, reward.ItemLog));

            string[] fieldsData = Global.ExecuteDBCmd((int)TCPGameServerCmds.CMD_DB_SENDUSERMAIL, strDbCmd, GameManager.LocalServerId);
            if (fieldsData == null || fieldsData.Length != 3)
            {
                error = "DB khong tra dung du lieu";
                return 0;
            }

            int mailId;
            if (!int.TryParse(fieldsData[1], out mailId) || mailId <= 0)
            {
                error = "MailID khong hop le: " + string.Join("|", fieldsData);
                return 0;
            }

            NotifyOnlinePlayer(reward, mailText);
            return mailId;
        }

        private RewardMailText BuildMailText(KuaFuRewardMailData reward, out string error)
        {
            error = string.Empty;
            Dictionary<string, RewardMailText> templates = GetMailTemplates();
            RewardMailText template;
            if (!templates.TryGetValue(reward.TemplateId ?? string.Empty, out template))
            {
                error = "MISSING_TEMPLATE:" + (reward.TemplateId ?? string.Empty);
                return null;
            }

            Dictionary<string, string> args = ParseTemplateArgs(reward.TemplateArgs);
            return new RewardMailText
            {
                Title = ApplyTemplateArgs(template.Title, args),
                Content = ApplyTemplateArgs(template.Content, args),
            };
        }

        private Dictionary<string, RewardMailText> GetMailTemplates()
        {
            if (MailTemplates != null)
            {
                return MailTemplates;
            }

            lock (TemplateMutex)
            {
                if (MailTemplates != null)
                {
                    return MailTemplates;
                }

                Dictionary<string, RewardMailText> templates = new Dictionary<string, RewardMailText>(StringComparer.OrdinalIgnoreCase);
                XElement xml = Global.GetGameResXml(TemplateConfigPath);
                foreach (XElement item in xml.Elements("Template"))
                {
                    string id = Global.GetSafeAttributeStr(item, "ID");
                    if (string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    XElement contentNode = item.Element("Content");
                    templates[id] = new RewardMailText
                    {
                        Title = Global.GetSafeAttributeStr(item, "Title"),
                        Content = contentNode != null ? contentNode.Value : string.Empty,
                    };
                }

                MailTemplates = templates;
                return MailTemplates;
            }
        }

        private Dictionary<string, string> ParseTemplateArgs(string templateArgs)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(templateArgs))
            {
                return result;
            }

            string[] pairs = templateArgs.Split(';');
            foreach (string pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                {
                    continue;
                }

                string[] parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                result[parts[0]] = parts[1];
            }

            return result;
        }

        private string ApplyTemplateArgs(string value, Dictionary<string, string> args)
        {
            if (string.IsNullOrEmpty(value) || args == null || args.Count <= 0)
            {
                return value ?? string.Empty;
            }

            foreach (KeyValuePair<string, string> pair in args)
            {
                value = value.Replace("{" + pair.Key + "}", pair.Value);
            }

            return value;
        }

        private void NotifyOnlinePlayer(KuaFuRewardMailData reward, RewardMailText mailText)
        {
            KPlayer client = GameManager.ClientMgr.FindClient(reward.RoleId);
            if (client == null)
            {
                return;
            }

            ClientManager.CheckEmailCount(client);
            KTGlobal.SendNewMailNotification(client);

            MailData mailData = new MailData
            {
                GoodsList = new List<GoodsData>(),
                Subject = mailText.Title,
                Content = mailText.Content,
                BoundToken = reward.BoundToken,
                BoundMoney = reward.BoundMoney,
                SenderRName = "Hệ thống",
            };
            KTGlobal.SendNewMailNotificationPopup(client, mailData);
        }

        private IKuaFuRewardService GetRewardService(bool noWait = false)
        {
            try
            {
                lock (_mutex)
                {
                    if (string.IsNullOrEmpty(_remoteServiceUri))
                    {
                        RefreshRemoteUri();
                    }

                    if (string.IsNullOrEmpty(_remoteServiceUri))
                    {
                        LogManager.WriteLog(LogTypes.KuaFuReward, "KuaFuRewardUri rong, khong tao proxy.");
                        return null;
                    }

                    if (_rewardService == null && noWait)
                    {
                        return null;
                    }
                }

                lock (_remotingMutex)
                {
                    IKuaFuRewardService service = _rewardService;
                    if (service == null)
                    {
                        service = (IKuaFuRewardService)Activator.GetObject(typeof(IKuaFuRewardService), _remoteServiceUri);
                        if (service == null)
                        {
                            return null;
                        }
                    }

                    int clientId = service.InitializeClient(this, _clientInfo);
                    if (clientId > 0)
                    {
                        lock (_mutex)
                        {
                            _rewardService = service;
                            _clientInfo.ClientId = clientId;
                        }
                    }

                    return _rewardService;
                }
            }
            catch (Exception ex)
            {
                ResetRewardService();
                LogManager.WriteLog(LogTypes.KuaFuReward, "Tao KuaFuReward proxy loi: " + ex);
                return null;
            }
        }

        private void RefreshRemoteUri()
        {
            string uri = null;
            if (_coreInterface != null)
            {
                uri = _coreInterface.GetRuntimeVariable(RuntimeVariableNames.KuaFuRewardUri, null);
            }

            if (string.IsNullOrEmpty(uri))
            {
                ConfigurationManager.RefreshSection("appSettings");
                uri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.KuaFuRewardUri);
            }

            if (_remoteServiceUri != uri)
            {
                _remoteServiceUri = uri;
                lock (_mutex)
                {
                    _rewardService = null;
                }

                LogManager.WriteLog(LogTypes.KuaFuReward, "KuaFuRewardUri cap nhat: " + _remoteServiceUri);
            }
        }

        private void ResetRewardService()
        {
            lock (_mutex)
            {
                _rewardService = null;
            }
        }
    }
}
