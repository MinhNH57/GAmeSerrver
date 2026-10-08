using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Tmsk.Contract;
using KF.Contract.Data;
using KF.Contract.Interface;
using Tmsk.Tools;
using Server.Tools;
using GameServer.Logic;
using GameManager = GameServer.Logic.GameManager;
using KF.Contract;
using GameServer.KiemThe;
using System.Configuration;
using GameServer.KiemThe.GameEvents.VongQuayMayMan;

namespace KF.Client
{
    public class JackpotClient : MarshalByRefObject, IKuaFuClient, IManager2
    {
        #region Singleton
        private static readonly JackpotClient instance = new JackpotClient();
        public static JackpotClient getInstance() { return instance; }
        #endregion

        private object Mutex = new object();
        private object RemotingMutex = new object();

        private string RemoteServiceUri = null;
        private IJackpotService JackpotService = null;
        private List<TopRoleInfo> _rankingCache = null;

        private ICoreInterface CoreInterface = null;
        private KuaFuClientContext ClientInfo = new KuaFuClientContext();
        private int JarPrimedFlag = 0;

        public bool initialize(ICoreInterface coreInterface)
        {
            CoreInterface = coreInterface;
            ClientInfo.ServerId = CoreInterface.GetLocalServerId();
            ClientInfo.GameType = (int)KF.Contract.Data.GameTypes.Jackpot;
            ClientInfo.Token = CoreInterface.GetLocalAddressIPs();
            return true;
        }

        public bool startup() { return true; }
        public bool showdown() { return true; }
        public bool destroy() { return true; }

        public bool isConnected = false;

        public void TimerProc(object sender, EventArgs e)
        {
            try
            {
                string uri = null;
                if (CoreInterface != null)
                {
                    uri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.JackpotUri, null);
                }
                
                if (string.IsNullOrEmpty(uri))
                {
                    ConfigurationManager.RefreshSection("appSettings");
                    uri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.JackpotUri);
                }
                if (RemoteServiceUri != uri)
                {
                    RemoteServiceUri = uri;
                    lock (Mutex)
                    {
                        JackpotService = null;
                    }
                    LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient: URI updated to {0}", RemoteServiceUri));
                }
                isConnected= false;
                var svc = GetJackpotService();
                if (svc != null)
                {
                    isConnected = true;
                    var items = svc.GetClientCacheItems(ClientInfo.ServerId);
                    if (items != null && items.Length > 0)
                    {
                        foreach (var item in items)
                        {
                            EventCallBackHandler((int)item.EventType, item.Args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
            }
        }

        private void ResetJackpotService()
        {
            if (CoreInterface != null)
            {
                RemoteServiceUri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.JackpotUri, null);
            }
            
            if (string.IsNullOrEmpty(RemoteServiceUri))
            {
                ConfigurationManager.RefreshSection("appSettings");
                RemoteServiceUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.JackpotUri);
            }
            lock (Mutex)
            {
                JackpotService = null;
            }
        }

        private IJackpotService GetJackpotService(bool noWait = false)
        {
            try
            {
                lock (Mutex)
                {
                    if (string.IsNullOrEmpty(RemoteServiceUri))
                    {
                        if (CoreInterface != null)
                        {
                            RemoteServiceUri = CoreInterface.GetRuntimeVariable(RuntimeVariableNames.JackpotUri, null);
                        }

                        if (string.IsNullOrEmpty(RemoteServiceUri))
                        {
                            ConfigurationManager.RefreshSection("appSettings");
                            RemoteServiceUri = ConfigurationManager.AppSettings.Get(RuntimeVariableNames.JackpotUri);
                            if (string.IsNullOrEmpty(RemoteServiceUri))
                            {
                                LogManager.WriteLog(LogTypes.JackPot, "JackpotClient: RemoteServiceUri is empty, cannot create proxy.");
                                return null;
                            }
                        }
                    }
                    if (JackpotService == null && noWait) return null;
                }

                IJackpotService tmp = null;
                lock (RemotingMutex)
                {
                    if (JackpotService == null)
                    {
                        try
                        {
                            tmp = (IJackpotService)Activator.GetObject(typeof(IJackpotService), RemoteServiceUri);
                            if (tmp == null)
                            {
                                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient: Failed to create proxy for URI: {0}", RemoteServiceUri));
                                return null;
                            }
                            LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient: Successfully created proxy for URI: {0}", RemoteServiceUri));
                        }
                        catch (Exception ex)
                        {
                            LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient: Exception creating proxy for URI {0}: {1}", RemoteServiceUri, ex.ToString()));
                            return null;
                        }
                    }
                    else
                    {
                        tmp = JackpotService;
                    }

                    int clientId = tmp.InitializeClient(this, ClientInfo);
                    if (tmp != null && (clientId != ClientInfo.ClientId || JackpotService != tmp))
                    {
                        lock (Mutex)
                        {
                            if (clientId > 0)
                            {
                                JackpotService = tmp;
                            }
                            else
                            {
                                JackpotService = null;
                            }
                            ClientInfo.ClientId = clientId;
                            return JackpotService;
                        }
                    }

                    return JackpotService;
                }
            }
            catch (Exception ex)
            {
                ResetJackpotService();
                LogManager.WriteExceptionUseCache(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Call remote JackpotService.TryHitKnbJackpot
        /// </summary>
        public JackpotResult TryHitKnbJackpot(int playerId, string playerName, long betKnb, string itemName, bool isBigWin, bool isSmallWin, bool notify)
        {
            try
            {
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient.TryHitKnbJackpot: Calling for playerId={0}, betKnb={1}", playerId, betKnb));
                var svc = GetJackpotService();
                if (svc == null)
                {
                    LogManager.WriteLog(LogTypes.JackPot, "JackpotClient.TryHitKnbJackpot: Failed to get JackpotService proxy");
                    return null;
                }

                if (System.Threading.Interlocked.CompareExchange(ref JarPrimedFlag, 1, 0) == 0)
                {
                    try
                    {
                        long amt = svc.GetJarAmount();
                        LogManager.WriteLog(LogTypes.Error, string.Format("JackpotClient.TryHitKnbJackpot: Primed jar amount = {0}", amt));
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteExceptionUseCache(ex.ToString());
                    }
                }

                LogManager.WriteLog(LogTypes.JackPot, "JackpotClient.TryHitKnbJackpot: Got service proxy, calling TryHitKnbJackpot");
                var result = svc.TryHitKnbJackpot(GameManager.ServerId, playerId, playerName, betKnb, itemName, isBigWin, isSmallWin, notify);
                LogManager.WriteLog(LogTypes.Error, string.Format("JackpotClient.TryHitKnbJackpot: Result = IsBigWin:{0}, Result = IsSmallWin:{1}, Award:{2}", result != null ? result.IsBigWin.ToString() : "null", result != null ? result.IsSmallWin.ToString() : "null", result != null ? result.AwardKnb.ToString() : "null"));
                return result;
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return null;
            }
        }

        public long AdjustJarAmount(long delta)
        {
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return -1;
                return svc.AdjustJarAmount(GameManager.ServerId, delta);
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return -1;
            }
        }

        public long ResetJarAmount()
        {
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return -1;
                long jarAfter = svc.ResetJarAmount(GameManager.ServerId);
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient.ResetJarAmount: serverId={0}, jarAfter={1}", GameManager.ServerId, jarAfter));
                return jarAfter;
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return -1;
            }
        }


        public Dictionary<int, JackpotResult> GetJackpotResults()
        {
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return null;
                return svc.GetJackpotResults();
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return null;
            }
        }
        public List<JackpotResult> GetJackpotHistory()
        {
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return null;
                return svc.GetJackpotHistory();
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return null;
            }
        }

        public List<TopRoleInfo> GetTopRankings()
        {
            if (_rankingCache != null) return _rankingCache;
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return null;
                return svc.GetTopRankings();
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return null;
            }
        }

        public int ClearLuckySpinServerPointData()
        {
            try
            {
                var svc = GetJackpotService();
                if (svc == null) return -1;
                int rows = svc.ClearLuckySpinServerPointData(GameManager.ServerId);
                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient.ClearLuckySpinServerPointData: serverId={0}, rows={1}", GameManager.ServerId, rows));
                return rows;
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
                ResetJackpotService();
                return -1;
            }
        }

        public void EventCallBackHandler(int eventType, params object[] args)
        {
            try
            {
                switch (eventType)
                {
                    case (int)KF.Contract.Data.KuaFuEventTypes.TopRoleRankingUpdated:
                        {
                            if (args != null && args.Length > 0 && args[0] is List<TopRoleInfo> rankingList)
                            {
                                _rankingCache = rankingList;
                                LogManager.WriteLog(LogTypes.JackPot, string.Format("JackpotClient: Received TopRoleRankingUpdated with {0} items.", rankingList.Count));
                                LuckySpinManager.LoadTopRanking(rankingList);
                            }
                            else
                            {
                                LogManager.WriteLog(LogTypes.JackPot, "JackpotClient: Received TopRoleRankingUpdated but args invalid or empty.");
                            }
                        }
                        break;
                    case (int)KF.Contract.Data.KuaFuEventTypes.JackpotWin:
                        {
                            if (args != null && args.Length >= 10)
                            {
                                int id = (int)args[0];
                                int serverId = (int)args[1];
                                int playerId = (int)args[2];
                                string playerName = (string)args[3];
                                long award = (long)args[4];
                                int winType = (int)args[5]; // 1=Big,2=Small,0=None
                                long jarBefore = (long)args[6];
                                long jarAfter = (long)args[7];
                                string itemName = (string)args[8];
                                int notify = (int)args[9]; // 1=true,0=false

                                //var player = GameManager.ClientMgr.FindClient(playerId);
                               // string roleName = player != null ? Global.FormatRoleNameWithZoneId(player) : ("RoleID=" + playerId);
                                string winText = winType == 1 ? "Big" : winType == 2 ? "Small" : "None";

                                if(winType != 0)
                                {
                                    string msg = winType == 1 ? $"Phúc tinh cao chiếu <color=#20FF00>{playerName}</color> khám phá <color=#FFEB00>bảo tàng đồ</color> nhận được thưởng lớn <color=#FFEB00>{award}</color> nguyên bảo, thật là may mắn!":
                                           $"<color=#20FF00>{playerName}</color> trong lúc chuẩn bị đóng thùng Bảo Rương lại phát hiện trong góc thùng có <color=#FFEB00>1 túi nguyên bảo nhỏ</color>. Bên trong chứa <color=#FFEB00>{award}</color> nguyên bảo, thật là may mắn!";
                                    //KTGlobal.SendSystemChat(msg);
                                    KTGlobal.SendSystemEventNotification(msg);
                                }
                                
                                LuckySpinManager.PushNewNotification(jarAfter,id, playerName, itemName, (int)award, notify);
                                LogManager.WriteLog(LogTypes.JackPot, $"JackpotWin Event: ServerID = {serverId}, PlayerID = {playerId}, Award = {award}, ItemName = {itemName}, JarBefore = {jarBefore}, JarAfter = {jarAfter}, Notify = {notify}");
                            }
                        }
                        break;
                    case (int)KF.Contract.Data.KuaFuEventTypes.JackpotJarAdjusted:
                        {
                            if (args != null && args.Length >= 4)
                            {
                                int serverId = (int)args[0];
                                long jarBefore = (long)args[1];
                                long jarAfter = (long)args[2];
                                long delta = (long)args[3];

                                LuckySpinManager.SetJarAmount(jarAfter);
                                LogManager.WriteLog(LogTypes.JackPot, $"JackpotJarAdjusted Event: ServerID={serverId}, JarBefore={jarBefore}, JarAfter={jarAfter}, Delta={delta}");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteExceptionUseCache(ex.ToString());
            }
        }
        public object GetDataFromClientServer(int dataType, params object[] args) { return null; }
        public int GetNewFuBenSeqId() { return CoreInterface != null ? CoreInterface.GetNewFuBenSeqId() : StdErrorCode.Error_Operation_Faild; }
        public int UpdateRoleData(KuaFuRoleData kuaFuRoleData, int roleId = 0) { return 0; }
        public int OnRoleChangeState(int roleId, int state, int age) { return 0; }
    }
}
