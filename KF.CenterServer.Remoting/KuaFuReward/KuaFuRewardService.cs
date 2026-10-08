using KF.Contract;
using KF.Contract.Data;
using KF.Contract.Interface;
using Maticsoft.DBUtility;
using MySql.Data.MySqlClient;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using Tmsk.Contract;

namespace KF.Remoting
{
    public class KuaFuRewardService : MarshalByRefObject, IKuaFuRewardService
    {
        private const int MaxRetryCount = 5;
        private static readonly object StorageInitMutex = new object();
        private static bool StorageInitialized = false;
        private readonly GameTypes _gameType = GameTypes.KuaFuReward;

        static KuaFuRewardService()
        {
            EnsureStorageInitialized();
        }

        public int InitializeClient(IKuaFuClient callback, KuaFuClientContext clientInfo)
        {
            try
            {
                if (clientInfo.ServerId > 0 && clientInfo.GameType == (int)_gameType)
                {
                    bool firstInit;
                    return ClientAgentManager.Instance().InitializeClient(callback, clientInfo, out firstInit);
                }

                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("KuaFuReward InitializeClient bi tu choi. ServerId={0}, GameType={1}", clientInfo.ServerId, clientInfo.GameType));
                return StdErrorCode.Error_Invalid_GameType;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("KuaFuReward InitializeClient loi. ServerId={0}, ClientId={1}, Error={2}", clientInfo.ServerId, clientInfo.ClientId, ex));
                return StdErrorCode.Error_Server_Internal_Error;
            }
        }

        public AsyncDataItem[] GetClientCacheItems(int serverId)
        {
            return ClientAgentManager.Instance().PickAsyncEvent(serverId, _gameType);
        }

        public KuaFuRewardSubmitResult SubmitMailReward(KuaFuRewardMailData reward)
        {
            EnsureStorageInitialized();

            if (reward == null || string.IsNullOrEmpty(reward.RewardKey) || reward.SourceServer <= 0 || reward.RoleId <= 0)
            {
                return new KuaFuRewardSubmitResult
                {
                    Success = false,
                    Message = "Du lieu reward khong hop le",
                };
            }

            try
            {
                const string insertSql = @"
INSERT INTO t_kuafu_reward_mail
(reward_key,event_type,source_server,role_id,template_id,template_args,bound_money,bound_token,mail_goods,item_log,status,retry_count,create_time,update_time)
VALUES
(@reward_key,@event_type,@source_server,@role_id,@template_id,@template_args,@bound_money,@bound_token,@mail_goods,@item_log,0,0,NOW(),NOW())
ON DUPLICATE KEY UPDATE update_time=update_time";

                DbHelperMySQL.ExecuteSql(insertSql,
                    new MySqlParameter("@reward_key", reward.RewardKey),
                    new MySqlParameter("@event_type", reward.EventType ?? string.Empty),
                    new MySqlParameter("@source_server", reward.SourceServer),
                    new MySqlParameter("@role_id", reward.RoleId),
                    new MySqlParameter("@template_id", reward.TemplateId ?? string.Empty),
                    new MySqlParameter("@template_args", reward.TemplateArgs ?? string.Empty),
                    new MySqlParameter("@bound_money", reward.BoundMoney),
                    new MySqlParameter("@bound_token", reward.BoundToken),
                    new MySqlParameter("@mail_goods", reward.MailGoods ?? string.Empty),
                    new MySqlParameter("@item_log", reward.ItemLog ?? string.Empty));

                KuaFuRewardMailData saved = GetRewardByKey(reward.RewardKey);
                bool duplicate = saved != null && saved.CreateTime != DateTime.MinValue && saved.Status != (int)KuaFuRewardMailStatus.Pending;
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Submit reward. RewardKey={0}, RoleID={1}, SourceServer={2}, Status={3}", reward.RewardKey, reward.RoleId, reward.SourceServer, saved != null ? saved.Status : -1));

                return new KuaFuRewardSubmitResult
                {
                    Success = saved != null,
                    IsDuplicate = duplicate,
                    RewardId = saved != null ? saved.Id : 0,
                    Status = saved != null ? saved.Status : -1,
                    Message = saved != null ? "OK" : "Khong doc duoc reward sau khi ghi",
                };
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Submit reward loi. RewardKey={0}, RoleID={1}, SourceServer={2}, Error={3}", reward.RewardKey, reward.RoleId, reward.SourceServer, ex));
                return new KuaFuRewardSubmitResult
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
        }

        public KuaFuRewardMailData[] PickPendingMailRewards(int sourceServer, int maxCount)
        {
            EnsureStorageInitialized();

            if (sourceServer <= 0)
            {
                return new KuaFuRewardMailData[0];
            }

            int takeCount = Math.Max(1, Math.Min(maxCount, 50));
            List<KuaFuRewardMailData> result = new List<KuaFuRewardMailData>();

            try
            {
                RecoverStaleProcessing(sourceServer);

                string selectSql = string.Format(@"
SELECT * FROM t_kuafu_reward_mail
WHERE source_server=@source_server
  AND status IN (0,3)
  AND retry_count < {0}
  AND (next_retry_time IS NULL OR next_retry_time <= NOW())
ORDER BY id ASC
LIMIT {1}", MaxRetryCount, takeCount);

                using (MySqlDataReader reader = DbHelperMySQL.ExecuteReader(selectSql, new MySqlParameter("@source_server", sourceServer)))
                {
                    while (reader != null && reader.Read())
                    {
                        KuaFuRewardMailData reward = ReadReward(reader);
                        if (TryMarkProcessing(reward.Id, sourceServer))
                        {
                            reward.Status = (int)KuaFuRewardMailStatus.Processing;
                            reward.RetryCount++;
                            result.Add(reward);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Pick reward loi. SourceServer={0}, Error={1}", sourceServer, ex));
            }

            return result.ToArray();
        }

        public bool ConfirmMailRewardSent(long rewardId, int sourceServer, int mailId)
        {
            try
            {
                const string sql = @"
UPDATE t_kuafu_reward_mail
SET status=2, mail_id=@mail_id, last_error='', sent_time=NOW(), update_time=NOW()
WHERE id=@id AND source_server=@source_server";

                int rows = DbHelperMySQL.ExecuteSql(sql,
                    new MySqlParameter("@mail_id", mailId),
                    new MySqlParameter("@id", rewardId),
                    new MySqlParameter("@source_server", sourceServer));

                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Confirm sent. RewardID={0}, SourceServer={1}, MailID={2}, Rows={3}", rewardId, sourceServer, mailId, rows));
                return rows > 0;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Confirm sent loi. RewardID={0}, SourceServer={1}, Error={2}", rewardId, sourceServer, ex));
                return false;
            }
        }

        public bool ConfirmMailRewardFailed(long rewardId, int sourceServer, string error)
        {
            try
            {
                const string sql = @"
UPDATE t_kuafu_reward_mail
SET status=IF(retry_count >= @max_retry, 4, 3),
    last_error=@last_error,
    next_retry_time=DATE_ADD(NOW(), INTERVAL 30 SECOND),
    update_time=NOW()
WHERE id=@id AND source_server=@source_server";

                int rows = DbHelperMySQL.ExecuteSql(sql,
                    new MySqlParameter("@max_retry", MaxRetryCount),
                    new MySqlParameter("@last_error", Truncate(error, 1000)),
                    new MySqlParameter("@id", rewardId),
                    new MySqlParameter("@source_server", sourceServer));

                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Confirm failed. RewardID={0}, SourceServer={1}, Rows={2}, Error={3}", rewardId, sourceServer, rows, error));
                return rows > 0;
            }
            catch (Exception ex)
            {
                LogManager.WriteLog(LogTypes.KuaFuReward, string.Format("Confirm failed loi. RewardID={0}, SourceServer={1}, Error={2}", rewardId, sourceServer, ex));
                return false;
            }
        }

        private static bool TryMarkProcessing(long rewardId, int sourceServer)
        {
            const string sql = @"
UPDATE t_kuafu_reward_mail
SET status=1, retry_count=retry_count+1, update_time=NOW()
WHERE id=@id AND source_server=@source_server AND status IN (0,3)";

            int rows = DbHelperMySQL.ExecuteSql(sql,
                new MySqlParameter("@id", rewardId),
                new MySqlParameter("@source_server", sourceServer));

            return rows > 0;
        }

        private static void RecoverStaleProcessing(int sourceServer)
        {
            const string sql = @"
UPDATE t_kuafu_reward_mail
SET status=3, last_error='Processing timeout', next_retry_time=NOW(), update_time=NOW()
WHERE source_server=@source_server AND status=1 AND update_time < DATE_SUB(NOW(), INTERVAL 2 MINUTE)";

            DbHelperMySQL.ExecuteSql(sql, new MySqlParameter("@source_server", sourceServer));
        }

        private static KuaFuRewardMailData GetRewardByKey(string rewardKey)
        {
            const string sql = "SELECT * FROM t_kuafu_reward_mail WHERE reward_key=@reward_key LIMIT 1";
            using (MySqlDataReader reader = DbHelperMySQL.ExecuteReader(sql, new MySqlParameter("@reward_key", rewardKey)))
            {
                if (reader != null && reader.Read())
                {
                    return ReadReward(reader);
                }
            }

            return null;
        }

        private static KuaFuRewardMailData ReadReward(IDataRecord reader)
        {
            return new KuaFuRewardMailData
            {
                Id = Convert.ToInt64(reader["id"]),
                RewardKey = reader["reward_key"].ToString(),
                EventType = reader["event_type"].ToString(),
                SourceServer = Convert.ToInt32(reader["source_server"]),
                RoleId = Convert.ToInt32(reader["role_id"]),
                TemplateId = reader["template_id"].ToString(),
                TemplateArgs = reader["template_args"].ToString(),
                BoundMoney = Convert.ToInt32(reader["bound_money"]),
                BoundToken = Convert.ToInt32(reader["bound_token"]),
                MailGoods = reader["mail_goods"].ToString(),
                ItemLog = reader["item_log"].ToString(),
                Status = Convert.ToInt32(reader["status"]),
                RetryCount = Convert.ToInt32(reader["retry_count"]),
                MailId = reader["mail_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["mail_id"]),
                LastError = reader["last_error"].ToString(),
                CreateTime = Convert.ToDateTime(reader["create_time"]),
                UpdateTime = Convert.ToDateTime(reader["update_time"]),
            };
        }

        private static void EnsureStorageInitialized()
        {
            StorageInitialized = true;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength);
        }
    }
}
