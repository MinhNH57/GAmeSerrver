using GameServer.KiemThe.CopySceneEvents.Model;
using GameServer.KiemThe.Core;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.KiemThe.Logic.Manager;
using GameServer.Logic;
using Server.Data;
using Server.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.KiemThe.CopySceneEvents.Family
{

	public class FamilyFuBen_RoomInfo
    {
		/// <summary>
		/// Bước của luật chơi theo trình tự
		/// </summary>
		public int roomID = 0;

		/// <summary>
		/// Phòng hiện tại của trò chơi đã hoàn tất chưa
		/// </summary>
		public bool currentGameStepCompleted = false;

		public List<int> listGuardian = new List<int>();

		public List<int> listGrowPoint = new List<int>();
		
		
		/// <summary>
		/// config phòng hiện tại
		/// </summary>
		public FamilyFuBen.GameRule.StepInfo stepInfo = null;

		public void checkRoomIsComplete()
        {
			if (this.roomID == FamilyFuBen.Config.BossRoomId)
			{
				if (this.listGuardian.Count == 0)
				{
					this.currentGameStepCompleted = true;
				}
			}
			else
			{
				if (this.listGuardian.Count == 0 && this.listGrowPoint.Count == 0)
				{
					this.currentGameStepCompleted = true;
				}
			}
        }
	}

	/// <summary>
	/// Script chính điều khiển phụ bản Vượt Ải Gia Tộc
	/// </summary>
	public class FamilyFuBen_Script_Main : CopySceneEvent
	{
		#region Constants
		/// <summary>
		/// Thời gian mỗi lần thông báo thông tin sự kiện tới người chơi
		/// </summary>
		private const int NotifyActivityInfoToPlayersEveryTick = 5000;
		#endregion

		#region Private fields
		/// <summary>
		/// Đối tượng Mutex dùng khóa Lock
		/// </summary>
		private readonly object Mutex = new object();

		/// <summary>
		/// Thời điểm thông báo cập nhật thông tin sự kiện tới tất cả người chơi lần trước
		/// </summary>
		private long LastNotifyTick;

		/// <summary>
		/// Bước hiện tại của hoạt động
		/// </summary>
		private int nStep;

		public List<FamilyFuBen_RoomInfo> listOpenedRoom = new List<FamilyFuBen_RoomInfo>();

		public Dictionary<int, int> completedRoom = new Dictionary<int, int>();

		public List<int> listRoomLocked = new List<int>();

		public List<int> listRandomBossTrigger = new List<int>();

		public List<int> listRoleGetReward = new List<int>();

		/// <summary>
		/// instance id tăng dần của quái
		/// </summary>
		private int nCurMonsterId;

		/// <summary>
		/// Đã hoàn thành vượt ải chưa
		/// </summary>
		private bool isCompleted = false;

		/// <summary>
		/// Tổng số Boss được triệu hồi nhờ Câu Hồn Ngọc
		/// </summary>
		private int totalBossesSummoned = 0;

		private NPC IdeNPC;
		#endregion

		#region Core CopySceneEvent
		/// <summary>
		/// Script chính điều khiển phụ bản Vượt Ải Gia Tộc
		/// </summary>
		/// <param name="copyScene"></param>
		public FamilyFuBen_Script_Main(KTCopyScene copyScene) : base(copyScene)
		{
		}

		/// <summary>
		/// Hàm này gọi khi bắt đầu ải
		/// </summary>
		protected override void OnStart()
		{
			/// Xóa toàn bộ NPC
			this.RemoveAllNPCs();
			/// Xóa toàn bộ quái
			this.RemoveAllMonsters();
			/// Xóa toàn bộ điểm thu thập
			this.RemoveAllGrowPoints();
			/// Xóa toàn bộ cổng dịch chuyển
			this.RemoveAllDynamicAreas();
			/// Cập nhật thời điểm thông báo gần nhất
			this.LastNotifyTick = 0;
			/// Cập nhật Step của hoạt động
			this.nStep = 0;
			this.nCurMonsterId = 0;

			this.completedRoom.Clear();
			this.listOpenedRoom.Clear();
			this.listRoomLocked.Clear();
			this.listRandomBossTrigger.Clear();
			foreach (var item in FamilyFuBen.Rule.Steps)
			{
				if (!string.IsNullOrEmpty(item.NeedStep))
				{
					this.listRoomLocked.Add(item.ID);
				}
			}

			FamilyFuBen.GameRule.StepInfo stepInfo = FamilyFuBen.Rule.Steps.Where(x => x.ID == FamilyFuBen.Config.BossRoomId).FirstOrDefault();
			while (listRandomBossTrigger.Count < FamilyFuBen.Config.BossTriggerNum)
            {
				Random rand = new Random();
				var index = 1 + rand.Next(stepInfo.Triggers.Count);
				bool isExist = false;
                foreach (var triggerIndex in this.listRandomBossTrigger)
                {
					if(index== triggerIndex)
                    {
						isExist = true;
					}
				}
                if (!isExist)
                {
					this.listRandomBossTrigger.Add(index);
				}
				
            }
            NPCGeneralManager.AddNewNPC(this.CopyScene.MapCode, this.CopyScene.ID, 2955, 16229, 9057, "Mã Xuyên Sơn", "Nhận thưởng vượt ải", Entities.Direction.DOWN, -1, "FamilyFuBen", (npc) =>
            {
                /// Lưu lại thông tin NPC vừa tạo
                this.IdeNPC = npc;
                npc.MinimapName = "Nhận thưởng vượt ải";
                npc.VisibleOnMinimap = true;
            });
        }

		/// <summary>
		/// Hàm này gọi liên tục trong ải
		/// </summary>
		protected override void OnTick()
		{
			/// Đang trong thời gian chờ mở ải
			if (this.LifeTimeTicks <= FamilyFuBen.Config.PrepareTime)
			{
				/// Nếu đã đến thời gian thông báo thông tin sự kiện
				if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= FamilyFuBen_Script_Main.NotifyActivityInfoToPlayersEveryTick)
				{
					/// Đánh dấu thời gian thông báo thông tin sự kiện
					this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
					/// Cập nhật thông tin sự kiện
					this.UpdateEventDetailsToPlayers(this.CopyScene.Name, FamilyFuBen.Config.PrepareTime - this.LifeTimeTicks, "Chuẩn bị mở ải");
				}
			}
			/// Nếu đã bắt đầu ải
			else
			{
				/// Nếu Step = 0
				if (this.nStep == 0)
				{
					/// Chuyển qua Step 1
					this.nStep = 1;
				}
				/// Nếu Step = 1
				else if (this.nStep == 1)
				{
					/// Nếu đã đến thời gian thông báo thông tin sự kiện
					if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= FamilyFuBen_Script_Main.NotifyActivityInfoToPlayersEveryTick)
					{
						/// Đánh dấu thời gian thông báo thông tin sự kiện
						this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
						/// Cập nhật thông tin sự kiện
						this.UpdateEventDetailsToPlayers(this.CopyScene.Name, this.CopyScene.DurationTicks - this.LifeTimeTicks, "Khai mở toàn bộ cơ quan và tiêu diệt hộ vệ");
					}

					lock (this.Mutex)
                    {
						bool hadRoomComplete = false;

                        for (int idx =0; idx<this.listOpenedRoom.Count; idx++)
                        {
							var openRoom = this.listOpenedRoom[idx];
                            if (openRoom.currentGameStepCompleted)
                            {
								/// Thực thi Logic khi hoàn thành bước hiện tại
								this.CompleteRoom(openRoom);

								hadRoomComplete = true;
								idx--;
                            }
                        }

                        if (hadRoomComplete)
                        {
							for (var i = 0; i < this.listRoomLocked.Count; i++)
                            {
								FamilyFuBen.GameRule.StepInfo stepInfo = FamilyFuBen.Rule.Steps.Where(x => x.ID == this.listRoomLocked[i]).FirstOrDefault();
								string[] fields = stepInfo.NeedStep.Split('_');

								bool isCanOpen = true;
								foreach (var field in fields)
                                {
									if(!this.completedRoom.TryGetValue(Convert.ToInt32(field), out int complete))
                                    {
										isCanOpen = false;
										break;
                                    }
                                }

                                if (isCanOpen)
                                {
									this.BeginNewRoom(stepInfo.ID);
									i--;
                                }
							}
							
                        }

						

						/// Nếu không thể bắt đầu tức đã qua hết toàn bộ các bước
						if (this.listOpenedRoom.Count==0 && this.completedRoom.Count !=0 && this.listRoomLocked.Count ==0)
						{
							/// Chuyển qua Step = 2
							this.nStep = 2;

							/// Đánh dấu thời gian thông báo thông tin sự kiện
							this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
							/// Cập nhật thông tin sự kiện
							this.UpdateEventDetailsToPlayers(this.CopyScene.Name, this.CopyScene.DurationTicks - this.LifeTimeTicks, "Hoàn thành vượt ải");
						}
						// nếu chưa mở phòng nào + chưa hoàn thành phòng nào -> mở phòng 1
						else if(this.listOpenedRoom.Count == 0 && this.completedRoom.Count == 0)
                        {
							this.BeginNewRoom(1);
                        }
					}
				}
				/// Nếu Step = 2
				else if (this.nStep == 2)
				{
					/// Đánh dấu đã hoàn thành vượt ải
					this.isCompleted = true;
					/// Chuyển qua Step = 100
					this.nStep = 100;
				}
			}
		}

		/// <summary>
		/// Hàm này gọi khi kết thúc ải
		/// </summary>
		protected override void OnClose()
		{
			/// Đưa người chơi về bản đồ báo danh
			this.KickOutPlayers();
		}

		/// <summary>
		/// Hàm này gọi đến khi người chơi giết quái
		/// </summary>
		/// <param name="player"></param>
		/// <param name="obj"></param>
		public override void OnKillObject(KPlayer player, GameObject obj)
		{
			base.OnKillObject(player, obj);

			//lock (this.Mutex)
			//{

			//	/// Nếu vẫn còn cơ quan chưa mở
			//	if (this.GetGrowPoints().Any(x => x.Data.ResID != 20016))
			//	{
			//		//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:OnKillObject step {0} => growpoint still exist. RoleID = {1}, RoleName = {2}, Family = {3}", this.currentGameStep, player?.RoleID, player?.RoleName, player?.FamilyName));
			//		/// Bỏ qua
			//		return;
			//	}

			//	/// Tổng số quái
			//	int monsterCount = this.GetTotalMonsters();
			//	/// Nếu vẫn còn quái
			//	if (monsterCount > 0)
			//	{
			//		//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:OnKillObject step {0} => monster left: {1}. RoleID = {2}, RoleName = {3}, Family = {4}", this.currentGameStep, monsterCount, player?.RoleID, player?.RoleName, player?.FamilyName));
			//		/// Bỏ qua
			//		return;
			//	}

			//	/// Đánh dấu đã hoàn thành bước hiện tại
			//	//this.currentGameStepCompleted = true;
			//}
		}

		/// <summary>
		/// Hàm này gọi đến khi người chơi chết
		/// </summary>
		/// <param name="killer"></param>
		/// <param name="player"></param>
		public override void OnPlayerDie(GameObject killer, KPlayer player)
		{
			base.OnPlayerDie(killer, player);
		}

		/// <summary>
		/// Hàm này gọi đến khi người chơi vào phụ bản
		/// </summary>
		/// <param name="player"></param>
		public override void OnPlayerEnter(KPlayer player)
		{
			base.OnPlayerEnter(player);
			/// Mở bảng thông báo hoạt động
			this.OpenEventBroadboard(player, FamilyFuBen.Config.EventID);
			/// Chuyển trạng thái PK hòa bình
			player.PKMode = (int) PKMode.Peace;
			/// Cập nhật thông tin sự kiện
			this.UpdateEventDetailsToPlayers(this.CopyScene.Name, FamilyFuBen.Config.PrepareTime - this.LifeTimeTicks, "Chuẩn bị mở ải");
		}

		/// <summary>
		/// Hàm này gọi đến khi người chơi rời phụ bản
		/// </summary>
		/// <param name="player"></param>
		/// <param name="toMap"></param>
		public override void OnPlayerLeave(KPlayer player, GameMap toMap)
		{
			base.OnPlayerLeave(player, toMap);
			/// Chuyển trạng thái PK hòa bình
			player.PKMode = (int) PKMode.Peace;
		}
		#endregion

		#region Private methods
		/// <summary>
		/// Đưa người chơi trở lại bản đồ báo danh
		/// </summary>
		/// <param name="player"></param>
		private void KickOutPlayer(KPlayer player)
		{
			GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, this.CopyScene.OutMapCode, this.CopyScene.OutPosX, this.CopyScene.OutPosY, (int) player.CurrentDir);
		}

		/// <summary>
		/// Đưa tất cả người chơi trở lại bản đồ báo danh
		/// </summary>
		private void KickOutPlayers()
		{
			foreach (KPlayer player in this.GetPlayers())
			{
				this.KickOutPlayer(player);
			}
		}

		/// <summary>
		/// Thực hiện Logic cổng dịch chuyển
		/// </summary>
		/// <param name="teleportInfo"></param>
		/// <param name="player"></param>
		private void DoTeleportLogic(FamilyFuBen.GameRule.TeleportInfo teleportInfo, KPlayer player)
		{
			/// Nếu là dịch đến cùng vị trí trong bản đồ
			if (teleportInfo.ToMapID == this.CopyScene.MapCode || teleportInfo.ToMapID == -1)
			{
				/// Nếu là mặc định dịch ra ngoài
				if (teleportInfo.ToPosX == -1 && teleportInfo.ToPosY == -1)
				{
					/// Trở về vị trí báo danh
					this.KickOutPlayer(player);
				}
				else
				{
					/// Thực hiện chuyển vị trí
					GameManager.ClientMgr.NotifyOthersGoBack(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, teleportInfo.ToPosX, teleportInfo.ToPosY, (int) player.CurrentDir);
				}
			}
			/// Nếu là dịch đến bản đồ khác
			else
			{
				/// Thực hiện dịch đến bản đồ tương ứng
				GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, teleportInfo.ToMapID, teleportInfo.ToPosX, teleportInfo.ToPosY, (int) player.CurrentDir);
			}
		}

		/// <summary>
		/// Tạo các cổng dịch chuyển khởi tạo cùng bước trò chơi
		/// </summary>
		/// <param name="step"></param>
		private void CreateImmediateTeleports(FamilyFuBen.GameRule.StepInfo step)
		{
			/// Duyệt danh sách cổng dịch chuyển
			foreach (FamilyFuBen.GameRule.TeleportInfo teleportInfo in step.Teleports)
			{
				/// Nếu là cổng dịch chuyển khởi tạo cùng bước trò chơi
				if (teleportInfo.SpawnImmediate)
				{
					/// Tạo cổng dịch chuyển
					KDynamicArea teleport = KTDynamicAreaManager.Add(this.CopyScene.MapCode, this.CopyScene.ID, teleportInfo.Name, 5340, teleportInfo.PosX, teleportInfo.PosY, this.DurationTicks - this.LifeTimeTicks, 2f, 100, -1, null);
					teleport.OnEnter = (obj) => {
						if (obj is KPlayer player)
						{
							this.DoTeleportLogic(teleportInfo, player);
						}
					};
				}
			}
		}

		/// <summary>
		/// Tạo các cổng dịch chuyển khởi tạo khi hoàn thành bước trò chơi
		/// </summary>
		/// <param name="step"></param>
		private void CreateCompleteStageTeleports(FamilyFuBen.GameRule.StepInfo step)
		{
			/// Duyệt danh sách cổng dịch chuyển
			foreach (FamilyFuBen.GameRule.TeleportInfo teleportInfo in step.Teleports)
			{
				/// Nếu là cổng dịch chuyển khởi tạo cùng bước trò chơi
				if (!teleportInfo.SpawnImmediate)
				{
					/// Tạo cổng dịch chuyển
					KDynamicArea teleport = KTDynamicAreaManager.Add(this.CopyScene.MapCode, this.CopyScene.ID, teleportInfo.Name, 5340, teleportInfo.PosX, teleportInfo.PosY, this.DurationTicks - this.LifeTimeTicks, 2f, 100, -1, null);
					teleport.OnEnter = (obj) => {
						if (obj is KPlayer player)
						{
							this.DoTeleportLogic(teleportInfo, player);
						}
					};
				}
			}
		}

		/// <summary>
		/// Tải cơ quan sinh ra
		/// </summary>
		/// <param name="step"></param>
		private void CreateImmediateTriggers(FamilyFuBen_RoomInfo roomInfo )
		{
			try
			{
				FamilyFuBen.GameRule.StepInfo step = roomInfo.stepInfo;

				/// Duyệt danh sách cơ quan
				foreach (FamilyFuBen.GameRule.TriggerInfo triggerInfo in step.Triggers)
				{

					GrowPoint growPoint = KTGrowPointManager.Add(this.CopyScene.MapCode, this.CopyScene.ID, GrowPointXML.Parse(triggerInfo.ID, triggerInfo.Name, -1, -1, triggerInfo.CollectTick, true), triggerInfo.PosX, triggerInfo.PosY);
					roomInfo.listGrowPoint.Add(triggerInfo.Index);

					growPoint.GrowPointCollectCompleted = (player) =>
					{

						LogManager.WriteLog(LogTypes.CopyScene, $"FamilyFuBen: kích hoạt cơ quan {triggerInfo.Index} - {step.StepName}");
						/// Xóa cơ quan
						this.RemoveGrowPoint(growPoint);

						/// Danh sách hộ vệ
						List<FamilyFuBen.GameRule.TriggerGuardianInfo> guardians = step.Guardians.Where(x => x.TriggerIndex == triggerInfo.Index).ToList();

						if (guardians.Count > 0)
						{
							/// Thông báo người chơi
							this.NotifyAllPlayers("Hộ vệ cơ quan đã xuất hiện, mau mau tiêu diệt toàn bộ!");
						}
						else
						{
							this.NotifyAllPlayers("Đã mở khóa cơ quan!");
						}

						/// Tổng số người chơi trong phụ bản
						int totalPlayers = this.GetTotalPlayers();
						/// Duyệt danh sách hộ vệ
						foreach (FamilyFuBen.GameRule.TriggerGuardianInfo guardianInfo in guardians)
						{
							/// Ngũ hành
							KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;
							/// Hướng quay
							KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
							/// Mức máu
							int hp = guardianInfo.BaseHP + guardianInfo.HPIncreaseEachLevel * totalPlayers;
							/// Tạo quái
							int nCurGuardianId = nCurMonsterId++;
							roomInfo.listGuardian.Add(nCurGuardianId);
							LogManager.WriteLog(LogTypes.CopyScene, $"FamilyFuBen: kích hoạt cơ quan {triggerInfo.Index} - {step.StepName} - tạo quái {nCurGuardianId} tại {guardianInfo.PosX} - {guardianInfo.PosY}");
							try
							{
								GameManager.MonsterZoneMgr.AddDynamicMonsters(this.CopyScene.MapCode, guardianInfo.ID, this.CopyScene.ID, 1, guardianInfo.PosX, guardianInfo.PosY, guardianInfo.Name, guardianInfo.Title, hp, 80, dir, series, guardianInfo.AIType, -1, -1, guardianInfo.AIScriptID, "Monster", (monster) =>
								{
								/// Nếu có kỹ năng
								if (guardianInfo.Skills.Count > 0)
									{
									/// Duyệt danh sách kỹ năng
									foreach (SkillLevelRef skill in guardianInfo.Skills)
										{
										/// Thêm vào danh sách kỹ năng dùng của quái
										monster.CustomAISkills.Add(skill);
										}
									}

								/// Nếu có vòng sáng
								if (guardianInfo.Auras.Count > 0)
									{
									/// Duyệt danh sách vòng sáng
									foreach (SkillLevelRef aura in guardianInfo.Auras)
										{
										/// Kích hoạt vòng sáng
										monster.UseSkill(aura.SkillID, aura.Level, monster);
										}
									}
									LogManager.WriteLog(LogTypes.CopyScene, $"FamilyFuBen: kích hoạt cơ quan {triggerInfo.Index} - {step.StepName} - tạo quái {nCurGuardianId} tại {guardianInfo.PosX} - {guardianInfo.PosY} [Thành công]");
								}, 65535, null, (obj) =>
								{
									var indexToRemove = roomInfo.listGuardian.FindIndex(r => r == nCurGuardianId);
									if (indexToRemove != -1)
									{
										roomInfo.listGuardian.RemoveAt(indexToRemove);
									}
									roomInfo.checkRoomIsComplete();
									LogManager.WriteLog(LogTypes.CopyScene, $"FamilyFuBen: kích hoạt cơ quan {triggerInfo.Index} - {step.StepName} - giết quái {nCurGuardianId} tại {guardianInfo.PosX} - {guardianInfo.PosY}");
								});
							}
							catch (Exception ex1)
							{
								LogManager.WriteLog(LogTypes.Exception, ex1.ToString());
							}
						}

						var indexTrigger = roomInfo.listGrowPoint.FindIndex(r => r == triggerInfo.Index);
						if (indexTrigger != -1)
						{
							roomInfo.listGrowPoint.RemoveAt(indexTrigger);
						}
						roomInfo.checkRoomIsComplete();

						if (roomInfo.roomID == FamilyFuBen.Config.BossRoomId)
						{
							if (this.listRandomBossTrigger.Count > 0)
							{
								var indexToRemove = this.listRandomBossTrigger.FindIndex(r => r == triggerInfo.Index);
								if (indexToRemove != -1)
								{
									this.listRandomBossTrigger.RemoveAt(indexToRemove);
									this.NotifyAllPlayers("Đã khai mở đúng cơ quan, hãy tiếp tục!");
								}

								if (this.listRandomBossTrigger.Count == 0)
								{
									this.NotifyAllPlayers("Đã kích hoạt buff, hãy tiêu diệt boss trong 10 phút!");

									SkillDataEx skillData = KSkill.GetSkillData(2007);
									if (skillData == null)
									{
										return;
									}

									SkillLevelRef skill = new SkillLevelRef()
									{
										Data = skillData,
										AddedLevel = 30,
										BonusLevel = 0,
										CanStudy = false,
									};


									foreach (KPlayer member in this.GetPlayers())
									{
										member.Buffs.AddBuff(new BuffDataEx()
										{
											Skill = skill,
											Duration = 10 * 60 * 1000,
											LoseWhenUsingSkill = false,
											SaveToDB = true,
											StartTick = KTGlobal.GetCurrentTimeMilis(),
										});
									}
								}
							}
						}
						//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:DoingStep step {0} => create guardians ok, total = {1}. RoleID = {2}, RoleName = {3}, Family = {4}", this.currentGameStep, guardians.Count, player?.RoleID, player?.RoleName, player?.FamilyName));
					};
				}

				/// Thông báo người chơi
				if (step.Triggers.Count > 0)
					this.NotifyAllPlayers("Cơ quan đã xuất hiện, hãy mau mau khai mở!");
				/// Người chơi bất kỳ
				KPlayer _player = this.GetPlayers().FirstOrDefault();
				//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:DoingStep step {0} => create trigger ok, total = {1}. RoleID = {2}, RoleName = {3}, Family = {4}", this.currentGameStep, step.Triggers.Count, _player?.RoleID, _player?.RoleName, _player?.FamilyName));
			}
			catch (Exception ex)
            {
				LogManager.WriteLog(LogTypes.Exception, ex.ToString());
            }
		}

		/// <summary>
		/// Tạo Boss tương ứng
		/// </summary>
		/// <param name="step"></param>
		private void CreateImmediateBosses(FamilyFuBen_RoomInfo roomInfo)
		{
			FamilyFuBen.GameRule.StepInfo step = roomInfo.stepInfo;
			/// Tổng số người chơi trong phụ bản
			int totalPlayers = this.GetTotalPlayers();
			/// Duyệt danh sách Boss
			foreach (FamilyFuBen.GameRule.BossInfo bossInfo in step.Bosses)
			{
				/// Ngũ hành
				KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;
				/// Hướng quay
				KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
				/// Mức máu
				int hp = bossInfo.BaseHP + bossInfo.HPIncreaseEachLevel * totalPlayers;
				/// Tạo Boss
				int nCurGuardianId = nCurMonsterId++;
				roomInfo.listGuardian.Add(nCurGuardianId);
				int levelBoss = 80;
				if (roomInfo.roomID == FamilyFuBen.Config.BossRoomId)
					levelBoss = 120;
				Monster thisBoss = null;
				GameManager.MonsterZoneMgr.AddDynamicMonsters(this.CopyScene.MapCode, bossInfo.ID, this.CopyScene.ID, 1, bossInfo.PosX, bossInfo.PosY, bossInfo.Name, bossInfo.Title, hp, levelBoss, dir, series, bossInfo.AIType, -1, -1, bossInfo.AIScriptID, "Boss", (monster) => {
					/// Nếu có kỹ năng
					if (bossInfo.Skills.Count > 0)
					{
						/// Duyệt danh sách kỹ năng
						foreach (SkillLevelRef skill in bossInfo.Skills)
						{
							/// Thêm vào danh sách kỹ năng dùng của quái
							monster.CustomAISkills.Add(skill);
						}
					}

					/// Nếu có vòng sáng
					if (bossInfo.Auras.Count > 0)
					{
						/// Duyệt danh sách vòng sáng
						foreach (SkillLevelRef aura in bossInfo.Auras)
						{
							/// Kích hoạt vòng sáng
							monster.UseSkill(aura.SkillID, aura.Level, monster);
						}
					}

					thisBoss = monster;
				}, 65535, null, (obj) => {
					var indexToRemove = roomInfo.listGuardian.FindIndex(r => r == nCurGuardianId);
					if (indexToRemove != -1)
					{
						roomInfo.listGuardian.RemoveAt(indexToRemove);
					}
					roomInfo.checkRoomIsComplete();

					if (roomInfo.roomID == FamilyFuBen.Config.BossRoomId)
					{
						if (thisBoss != null)
						{
							var itemNum = Global.GetRandomNumber(20, 30);

							List<ItemData> dropItems = new List<ItemData>();
							ItemData itemData = ItemManager.GetItemTemplate(371);

							itemData.BindType = 1;

							for (int i = 0; i < itemNum; i++)
							{
								dropItems.Add(itemData);
							}


							/// Nếu tồn tại danh sách vật phẩm sẽ rơi
							if (dropItems.Count > 0)
							{
								/// Tạo vật phẩm rơi dưới đất tương ứng
								ItemDropManager.CreateDropToMap(dropItems, thisBoss);

								dropItems.Clear();
							}
						}
					}
				});
			}

			/// Thông báo người chơi
			if (step.Bosses.Count > 0) 
				this.NotifyAllPlayers("Boss đã xuất hiện, hãy mau mau tiêu diệt!");
			/// Người chơi bất kỳ
			KPlayer player = this.GetPlayers().FirstOrDefault();
			//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:DoingStep step {0} => create boss ok, total = {1}. RoleID = {2}, RoleName = {3}, Family = {4}", this.currentGameStep, step.Bosses.Count, player?.RoleID, player?.RoleName, player?.FamilyName));
		}

		/// <summary>
		/// Thực hiện Logic của bước hiện tại
		/// </summary>
		private void CompleteRoom(FamilyFuBen_RoomInfo roomInfo)
		{
			/// Người chơi bất kỳ
			KPlayer player = this.GetPlayers().FirstOrDefault();
			
			FamilyFuBen.GameRule.StepInfo stepInfo = roomInfo.stepInfo;
			/// Nếu không tìm thấy
			if (stepInfo == null)
			{
				//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:CompleteCurrentStep step {0} => stepInfo = null. RoleID = {1}, RoleName = {2}, Family = {3}", this.currentGameStep, player?.RoleID, player?.RoleName, player?.FamilyName));
				/// Bỏ qua
				return;
			}

			/// Tạo cổng Teleport sinh ra khi hoàn thành
			this.CreateCompleteStageTeleports(stepInfo);

			//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:CompleteCurrentStep step {0} => OK. RoleID = {1}, RoleName = {2}, Family = {3}", this.currentGameStep, player?.RoleID, player?.RoleName, player?.FamilyName));

			string strNext = stepInfo.NextStep;
			if (!string.IsNullOrEmpty(strNext))
			{
				string[] fields = strNext.Split('_');
				foreach (var roomId in fields)
				{
					this.BeginNewRoom(Convert.ToInt32(roomId));
				}
			}
			this.completedRoom.Add(roomInfo.roomID, 1);

			var indexToRemove = this.listOpenedRoom.FindIndex(r => r.roomID == roomInfo.roomID);
			if (indexToRemove != -1)
			{
				this.listOpenedRoom.RemoveAt(indexToRemove);
			}
		}

		/// <summary>
		/// Bắt đầu trò chơi ở bước tương ứng
		/// </summary>
		/// <param name="step"></param>
		private bool BeginNewRoom(int roomId)
		{
			/// Bước hiện tại
			FamilyFuBen.GameRule.StepInfo stepInfo = FamilyFuBen.Rule.Steps.Where(x => x.ID == roomId).FirstOrDefault();
			/// Người chơi bất kỳ
			KPlayer player = this.GetPlayers().FirstOrDefault();
			/// Nếu không tìm thấy
			if (stepInfo == null)
			{
				//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:BeginNewStep step {0} => stepInfo = null. RoleID = {1}, RoleName = {2}, Family = {3}", this.currentGameStep, player?.RoleID, player?.RoleName, player?.FamilyName));
				/// Trả về kết quả không thể bắt đầu bước mới
				return false;
			}

			var indexToRemove = this.listRoomLocked.FindIndex(r => r == roomId);
			if (indexToRemove != -1)
			{
				this.listRoomLocked.RemoveAt(indexToRemove);
			}

			FamilyFuBen_RoomInfo newRoomInfo = new FamilyFuBen_RoomInfo();
			newRoomInfo.roomID = roomId;
			newRoomInfo.stepInfo = stepInfo;

			/// Nếu không có gì
			if (stepInfo.Triggers.Count <= 0 && stepInfo.Bosses.Count <= 0)
			{
				this.CreateImmediateTeleports(stepInfo);
				/// Đánh dấu hoàn thành bước hiện tại

				//this.completedRoom.Add(roomId, 1);
				this.CompleteRoom(newRoomInfo);
			}
			else
			{
				this.listOpenedRoom.Add(newRoomInfo);

				/// Tạo cổng Teleport mặc định cùng bước tương ứng
				this.CreateImmediateTeleports(stepInfo);

				/// Tạo cơ quan
				this.CreateImmediateTriggers(newRoomInfo);

				/// Tạo Boss
				this.CreateImmediateBosses(newRoomInfo);
			}

			if (roomId == 10)
			{
				this.NotifyAllPlayers($"Lửa trại gia tộc đã xuất hiện tại phòng {stepInfo.StepName}!");
				GrowPoint growPoint = KTGrowPointManager.Add(this.CopyScene.MapCode, this.CopyScene.ID, GrowPointXML.Parse(20016, "Đống củi", -1, -1, 10000, true), 12174, 6804);
				growPoint.GrowPointCollectCompleted = (host) =>
				{
					/// Xóa cơ quan
					this.RemoveGrowPoint(growPoint);

					KDynamicArea dynArea = KTDynamicAreaManager.Add(this.CopyScene.MapCode, this.CopyScene.ID, "Lửa Trại", 20022, 12174, 6804, 60 * 5, 5, 1500, -1, "LUATRAI_" + host.RoleID + "");
					dynArea.OnStayTick = (go) =>
					{
						int tickCount = 60;
						int EXPGAIN = 28000 * go.m_Level / tickCount;

						EXPGAIN += EXPGAIN * (go as KPlayer).m_nExpEnhancePercent / 100;
						EXPGAIN += EXPGAIN * (go as KPlayer).m_nExpAddtionP / 100;

						var x = go as KPlayer;
						if (x.Buffs.HasBuff(378))
						{
							var teamMatesCount = 0;
							if (x.Teammates != null)
							{
								// đếm số lượng thành viên tổ đội có uống rượu
								teamMatesCount = x.Teammates.Count(p => p.Buffs.HasBuff(378));
							}
							// mỗi thành viên trong tổ đội mà có rượu được nhận thêm 20% exp
							var expRate = teamMatesCount * 0.2f;
							// nhưng không được quá 60%
							expRate = Math.Min(expRate, 0.6f);

							EXPGAIN = (int)(EXPGAIN * (1f + expRate));
						}

						if (x.Buffs.HasBuff(332))
						{
							/// trừ kinh nghiệm của tu luyện châu
							if (x.m_longMaxExpXiulianzhu < 0)
							{
								x.Buffs.RemoveBuff(332);
							}
							else
							{
								x.m_longMaxExpXiulianzhu -= EXPGAIN;
								PlayerManager.ShowNotification(x, string.Format("Quỹ kinh nghiệm tu luyện châu còn lại: {0}", x.m_longMaxExpXiulianzhu));
							}
						}
						else
						{
							// reset lại quỹ kn đánh quái
							x.m_longMaxExpXiulianzhu = 0;
						}


						GameManager.ClientMgr.ProcessRoleExperience(go as KPlayer, EXPGAIN, true, false, true, "LUATRAI_FAMILY");

						player.m_nTickCampFire = KTGlobal.GetCurrentTimeMilis();

					};
				};
			}

			if (roomId == FamilyFuBen.Config.BossRoomId)
			{
				//NPCGeneralManager.AddNewNPC(this.CopyScene.MapCode, this.CopyScene.ID, 2955, 16229, 9057, "Mã Xuyên Sơn", "Nhận thưởng vượt ải", Entities.Direction.DOWN, -1, "FamilyFuBen", (npc) => {
				//	/// Lưu lại thông tin NPC vừa tạo
				//	this.IdeNPC = npc;
				//	npc.MinimapName = "Nhận thưởng vượt ải";
				//	npc.VisibleOnMinimap = true;
				//});
			}
			/// Thông báo người chơi
			this.NotifyAllPlayers($"Cửa phòng {stepInfo.StepName} đã mở, hãy nhanh chân!");

			//LogManager.WriteLog(LogTypes.CopyScene, string.Format("FamilyFuBen:BeginNewStep step {0} => OK. RoleID = {1}, RoleName = {2}, Family = {3}", this.currentGameStep, player?.RoleID, player?.RoleName, player?.FamilyName));

			/// Trả về kết quả bắt đầu thành công
			return true;
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Kiểm tra điều kiện dùng Câu Hồn Ngọc để tạo Boss
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public string UseCallBossItem_CheckCondition(KPlayer player)
		{
			/// Nếu chưa hoàn thành vượt ải
			if (!this.isCompleted)
			{
				return "Chưa hoàn thành Vượt ải, không thể sử dụng Câu Hồn Ngọc!";
			}
			/// Nếu đã triệu hồi quá số lượng
			else if (this.totalBossesSummoned >= FamilyFuBen.Config.MaxCallBoss)
			{
				return string.Format("Chỉ có thể triệu hồi tối đa {0} Boss Võ lâm cao thủ!", FamilyFuBen.Config.MaxCallBoss);
			}

			/// Trả về kết quả triệu hồi thành công
			return "OK";
		}

		/// <summary>
		/// Sử dụng Câu Hồn Ngọc tạo Boss
		/// </summary>
		/// <param name="player"></param>
		/// <param name="bossID"></param>
		public void UseCallBossItem(KPlayer player, int bossID)
		{
			/// Nếu chưa hoàn thành vượt ải
			if (!this.isCompleted)
			{
				PlayerManager.ShowNotification(player, "Chưa hoàn thành Vượt ải, không thể sử dụng Câu Hồn Ngọc!");
				return;
			}
			/// Nếu đã triệu hồi quá số lượng
			else if (this.totalBossesSummoned >= FamilyFuBen.Config.MaxCallBoss)
			{
				PlayerManager.ShowNotification(player, string.Format("Chỉ có thể triệu hồi tối đa {0} Boss Võ lâm cao thủ!", FamilyFuBen.Config.MaxCallBoss));
				return;
			}

			/// Tăng số lượng Boss đã triệu hồi
			this.totalBossesSummoned++;

			/// Tạo Boss tương ứng
			GameManager.MonsterZoneMgr.AddDynamicMonsters(this.CopyScene.MapCode, bossID, this.CopyScene.ID, 1, player.PosX, player.PosY, "", "", -1, -1, Entities.Direction.NONE, KE_SERIES_TYPE.series_none, MonsterAIType.Boss, -1, -1, -1, "SpecialBoss", null, 65535);

			/// Thông báo triệu hồi thành công
			PlayerManager.ShowNotification(player, "Triệu hồi thành công!");
		}

		public string UseGetReward_CheckCondition(KPlayer player)
		{
			var playerIndex = this.listRoleGetReward.FindIndex(x => x == player.RoleID);
            if (playerIndex >= 0)
            {
				return "Bạn đã nhận thưởng Vượt ải gia tộc lần này rồi!";
            }


			/// Nếu hoàn thành vượt ải
			if (this.isCompleted)
			{
				return "OK";
			}
			
			else 
			{
				var indexBossRoom = this.listOpenedRoom.FindIndex(x => x.roomID == FamilyFuBen.Config.BossRoomId);
                if (indexBossRoom >= 0)
                {
					return "OK1";
                }
			}

			return "Chưa đủ điều kiện nhận thưởng! Cần mở được phòng hậu viện hoặc hoàn thành phó bản!";
		}

		public void UserGetReward(KPlayer player)
		{
			var rand = new Random();
			var coutItem = 0;
			if (this.isCompleted)
			{
				coutItem = rand.Next(50, 151);
				/// Thêm danh vọng tương ứng
				KTGlobal.AddRepute(player, 401, 100);
			}

			else
			{
				var indexBossRoom = this.listOpenedRoom.FindIndex(x => x.roomID == FamilyFuBen.Config.BossRoomId);
				if (indexBossRoom >= 0)
				{
					coutItem = rand.Next(30, 101);
					/// Thêm danh vọng tương ứng
					KTGlobal.AddRepute(player, 401, 30);
				}
			}

            if (coutItem > 0)
            {
				this.listRoleGetReward.Add(player.RoleID);

				string TimeUsing = Global.ConstGoodsEndTime;

				ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, 371, coutItem, 0, "VUOT_AI_GIA_TOC",
					true, 1, false, TimeUsing);
			}
		}

		public void NPCClick(NPC npc, KPlayer player)
		{
			/// Nếu NPC không tồn tại
			if (npc != this.IdeNPC)
			{
				/// Thông báo
				PlayerManager.ShowNotification(player, "Không tìm thấy NPC tương ứng!");
				/// Bỏ qua
				return;
			}
			

			try
			{
				
				/// Tạo Dialog
				KNPCDialog dialog = new KNPCDialog();
				dialog.Text = "Thì ra bảo tàng là nguyên bộ tịch tà 12 cấp từ giáo dục từ tiểu học lên trung học phổ thông.Thật là võ công chi bí đâu đâu cũng có khác biệt";
				/// Duyệt danh sách câu trả lời
				var playerIndex = this.listRoleGetReward.FindIndex(x => x == player.RoleID);
				if (playerIndex >= 0)
				{
					dialog.Text = "Bạn đã nhận thưởng vượt ải lần này rồi!";
					dialog.Selections = new Dictionary<int, string>()
						{
							{ 3, "Dùng Đồng Tiền Cổ đổi phần thưởng"},
							{ 1000, "Kết thúc đối thoại" },
							{ 1001, "Ta muốn rời khỏi đây..." }
						};
				}
				else
				{
						dialog.Selections = new Dictionary<int, string>()
						{
							{ 1, "Ta muốn nhận thưởng"},
							{ 3, "Dùng Đồng Tiền Cổ đổi phần thưởng"},
							{ 1000, "Kết thúc đối thoại" },
							{ 1001, "Ta muốn rời khỏi đây..." }
						};
				}
				dialog.OnSelect = (data) => {
					
					/// Nếu đáp án không chính xác
					if (data.SelectID == 1)
					{
						string ret = this.UseGetReward_CheckCondition(player);
						if(ret!="OK"  && ret != "OK1")
                        {
							dialog.Text = ret;
							dialog.Selections = new Dictionary<int, string>()
							{
								{ 1000, "Kết thúc đối thoại" }
							};
							dialog.ShowDialog(player);
						}
                        else
                        {
							if (ret == "OK")
							{
								dialog.Text = "Bạn đã hoàn thành Phó bản Vượt ải gia tộc, sẽ nhận được 50-150 xu khóa gia tộc.";
								dialog.Selections = new Dictionary<int, string>()
								{
									{ 2, "Nhận thưởng phó bản" }
								};
								dialog.ShowDialog(player);
							}
							else
							{
								dialog.Text = "Bạn chưa hoàn thành Phó bản Vượt ải gia tộc, chỉ có thể nhận được 30-100 xu khóa gia tộc. Nếu hoàn thành phó bản, bạn có thể nhận 50-150 xu khóa gia tộc. Bạn có muốn nhận thưởng ngay lúc này không?";
								dialog.Selections = new Dictionary<int, string>()
								{
									{ 2, "Nhận thưởng phó bản" },
									{ 1000, "Kết thúc đối thoại" }
								};
								dialog.ShowDialog(player);
							}
                        }
					}
					else if (data.SelectID == 2)
					{
						this.UserGetReward(player);
						KT_TCPHandler.CloseDialog(player);
						KTNPCDialogManager.RemoveNPCDialog(dialog);
					}
					else if (data.SelectID == 3)
					{
						dialog.Text = "Ta đã không nhìn lầm người, ngươi thực sự rất mạnh mẽ! Ngươi thu thập được rất nhiều Đồng Tiền Cổ, phải không? Ta sẽ đổi kho báu của ta để lấy nó!";
						dialog.Selections = new Dictionary<int, string>()
								{
									{ 4, "Dùng 100 Đồng Tiền Cổ đổi phần thưởng" },
									{ 1000, "Ta suy nghĩ đã" }
								};
						dialog.ShowDialog(player);
					}
					else if (data.SelectID == 4)
					{
						int CountItemInBag = ItemManager.GetItemCountInBag(player, 371);
						if (CountItemInBag < 100)
						{
							dialog.Text = "Ta cần 100 Đồng Tiền Cổ, có đủ rồi hãy đưa ta. Ta đang rất bận!";
							dialog.Selections = new Dictionary<int, string>()
								{
									{ 1000, "Kết thúc đối thoại" }
								};
							dialog.ShowDialog(player);
						}
						else
						{
							int SpaceNeed = ItemManager.TotalSpaceNeed(626, 1);

							if (!KTGlobal.IsHaveSpace(SpaceNeed, player))
							{
								PlayerManager.ShowNotification(player, "Túi đồ không đủ chỗ");
							}
							else
							{
								if (ItemManager.RemoveItemFromBag(player, 371, 100, -1, "Đổi thưởng vượt ải gia tộc"))
								{
									//ProcessTask.Process(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, -1, -1, gd.GoodsID, TaskTypes.UseSomething);

									//var nExpAdd = KPlayerSetting.GetBaseExpLevel(player.m_Level) * 30 * 2;
									//GameManager.ClientMgr.ProcessRoleExperience(player, nExpAdd, true, false, true, "ThuongVuotAiGiaToc");

									string TimeUsing = Global.ConstGoodsEndTime;


									if (!ItemManager.CreateItem(Global._TCPManager.TcpOutPacketPool, player, 626, 1, 0, "ThuongVuotAiGiaToc", false, 1, false, TimeUsing, "", -1, "", 0, 0, true))
									{
										PlayerManager.ShowNotification(player, "Có lỗi khi add vật phẩm");
										LogManager.WriteLog(LogTypes.Error, $"Add Rương ThuongVuotAiGiaToc lỗi {player.RoleID}-{player.RoleName}");
									}
								}
							}
							

							dialog.Text = "Cảm ơn ngươi về những Đồng Tiền Cổ này, đây là phần thưởng của ngươi, hãy nhận nó!";
							dialog.Selections = new Dictionary<int, string>()
								{
									{ 1000, "Kết thúc đối thoại" }
								};
							dialog.ShowDialog(player);
						}
					}
					/// Nếu đáp án chính xác
					else if( data.SelectID == 1000)
					{
						KT_TCPHandler.CloseDialog(player);
						KTNPCDialogManager.RemoveNPCDialog(dialog);
					}
					else if ( data.SelectID == 1001)
                    {
						KT_TCPHandler.CloseDialog(player);
						KTNPCDialogManager.RemoveNPCDialog(dialog);

						GameManager.ClientMgr.NotifyChangeMap(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, player, 23, 4200, 3600, (int)player.CurrentDir);

					}
				};
				/// Hiện khung
				dialog.ShowDialog(player);
			}
			catch (Exception ex)
			{
				LogManager.WriteLog(LogTypes.Exception, ex.ToString());
			}
		}
		#endregion
	}
}
