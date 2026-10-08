using GameServer.KiemThe.CopySceneEvents.Model;
using GameServer.KiemThe.Core.Item;
using GameServer.KiemThe.Entities;
using GameServer.KiemThe.Logic;
using GameServer.Logic;

namespace GameServer.KiemThe.CopySceneEvents.MiJingFuBen
{
	/// <summary>
	/// Script phụ bản Bí Cảnh
	/// </summary>
	public class MiJing_Script_Main : CopySceneEvent
	{
		#region Constants
		/// <summary>
		/// Thời gian mỗi lần thông báo thông tin sự kiện tới người chơi
		/// </summary>
		private const int NotifyActivityInfoToPlayersEveryTick = 5000;
		#endregion

		#region Private fields
		/// <summary>
		/// Thời điểm thông báo cập nhật thông tin sự kiện tới tất cả người chơi lần trước
		/// </summary>
		private long LastNotifyTick;

		private int _MasterRoleId;
		#endregion

		#region Constructor
		/// <summary>
		/// Script phụ bản Bí Cảnh
		/// </summary>
		/// <param name="copyScene"></param>
		public MiJing_Script_Main(KTCopyScene copyScene) : base(copyScene)
		{

		}
		#endregion

		#region Core CopySceneEvent
		/// <summary>
		/// Sự kiện bắt đầu bí cảnh
		/// </summary>
		protected override void OnStart()
		{
			// Tạo quái
			this.CreateMonsters();
		}

		/// <summary>
		/// Sự kiện Tick
		/// </summary>
		protected override void OnTick()
		{
			// Nếu đã đến thời gian thông báo thông tin sự kiện
			if (KTGlobal.GetCurrentTimeMilis() - this.LastNotifyTick >= MiJing_Script_Main.NotifyActivityInfoToPlayersEveryTick)
			{
				// Đánh dấu thời gian thông báo thông tin sự kiện
				this.LastNotifyTick = KTGlobal.GetCurrentTimeMilis();
				// Cập nhật thông tin sự kiện
				this.UpdateEventDetailsToPlayers(this.CopyScene.Name, MiJing.Duration - this.LifeTimeTicks, "Bí cảnh duy trì");
			}
		}

		/// <summary>
		/// Sự kiện đóng bí cảnh
		/// </summary>
		protected override void OnClose()
		{
			// Xóa toàn bộ quái
			this.RemoveAllMonsters();
		}


		public int MasterRoleId {
            get
            {
				return _MasterRoleId;
            }
            set
            {
				_MasterRoleId = value;
            }
		}

		/// <summary>
		/// Sự kiện khi người chơi vào phụ bản
		/// </summary>
		/// <param name="player"></param>
		public override void OnPlayerEnter(KPlayer player)
		{
			base.OnPlayerEnter(player);

			// Mở bảng thông báo hoạt động
			this.OpenEventBroadboard(player, MiJing.EventID);
			// Chuyển trạng thái PK hòa bình
			player.PKMode = (int) PKMode.Peace;
			// Cập nhật thông tin sự kiện
			this.UpdateEventDetailsToPlayers(this.CopyScene.Name, MiJing.Duration - this.LifeTimeTicks, "Bí cảnh duy trì");

			// Nếu là đội trưởng thì thêm Buff x2 kinh nghiệm vào
			if (player.RoleID == MasterRoleId)
			{
				// Xóa Buff x4 kinh nghiệm cũ
				player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
				// Nhận Buff x4 kinh nghiệm mới
				player.Buffs.AddBuff(MiJing.DoubleExpBuff, 1);

				player.m_buffIDMijing = MiJing.DoubleExpBuff;
			}
			else 
            {

				// Xóa buff cũ
				player.Buffs.RemoveBuff(2000);
				// nhận buff x2
				player.Buffs.AddBuff(2000, 1);

				player.m_buffIDMijing = 2000;

				var requiredMaps = MiJing_EventScript.GetRequiredNumberOfMijingMap(player, false);

				//. gửi thông báo đến người chơi nếu có bản đồ bí cảnh mang theo
				if (ItemManager.GetItemCountInBag(player, MiJing.MapItem) >= requiredMaps)
                {
					PlayerManager.ShowMessageBox(player, "Chú ý",
					$"Bạn có muốn sử dụng {requiredMaps} Bản đồ bí cảnh đang mang trong người để nhận x6 kinh nghiệm trong bí cảnh hiện tại không", () =>
					{
						// xóa vật phẩm
						ItemManager.RemoveItemFromBag(player, MiJing.MapItem, requiredMaps, -1, "Bí Cảnh");
						// Xóa buff cũ
						player.Buffs.RemoveBuff(2000);
						player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
						// thêm buff mới
						player.Buffs.AddBuff(MiJing.DoubleExpBuff, 1);

						player.m_buffIDMijing = MiJing.DoubleExpBuff;

					}, true);
				}				
            }
		}

		public override void OnPlayerReconnected(KPlayer player)
        {
			base.OnPlayerReconnected(player);

			player.m_buffIDMijing = Global.GetRoleParamsInt32FromDB(player, RoleParamName.MijingBuffID);
			// Xóa buff cũ
			player.Buffs.RemoveBuff(2000);
			player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
			// nếu trước đó có buff nào thì adđ buff đó vào
			if (player.m_buffIDMijing == MiJing.DoubleExpBuff)
			{
				// Xóa Buff x4 kinh nghiệm cũ
				player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
				// Nhận Buff x4 kinh nghiệm mới
				player.Buffs.AddBuff(MiJing.DoubleExpBuff, 1);
			}
			else
			{
				// Xóa buff cũ
				player.Buffs.RemoveBuff(2000);
				// nhận buff x2
				player.Buffs.AddBuff(2000, 1);
			}
		}

		public override void OnPlayerDisconnected(KPlayer player)
        {
			Global.SaveRoleParamsInt32ValueToDB(player, RoleParamName.MijingBuffID, player.m_buffIDMijing, true);

			base.OnPlayerDisconnected(player);
		}

		public override void OnPlayerRelive(KPlayer player)
        {
			base.OnPlayerRelive(player);
			if (player.m_buffIDMijing == MiJing.DoubleExpBuff)
			{
				// Xóa Buff x4 kinh nghiệm cũ
				player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
				// Nhận Buff x4 kinh nghiệm mới
				player.Buffs.AddBuff(MiJing.DoubleExpBuff, 1);
			}
			else
			{
				// Xóa buff cũ
				player.Buffs.RemoveBuff(2000);
				// nhận buff x2
				player.Buffs.AddBuff(2000, 1);
			}

		}

		/// <summary>
		/// Sự kiện khi người chơi rời phụ bản
		/// </summary>
		/// <param name="player"></param>
		/// <param name="toMap"></param>
		public override void OnPlayerLeave(KPlayer player, GameMap toMap)
		{
			base.OnPlayerLeave(player, toMap);

			// Đóng bảng thông báo hoạt động
			this.CloseEventBroadboard(player, MiJing.EventID);

			// Xóa Buff x4 kinh nghiệm
			player.Buffs.RemoveBuff(MiJing.DoubleExpBuff);
			// xóa buff x2
			player.Buffs.RemoveBuff(2000);
		}
		#endregion

		#region Private methods
		/// <summary>
		/// Tạo quái
		/// </summary>
		private void CreateMonsters()
		{
			foreach (MiJing.MonsterInfo monsterInfo in MiJing.Monsters)
			{
				// Ngũ hành
				KE_SERIES_TYPE series = KE_SERIES_TYPE.series_none;
				// Hướng quay
				KiemThe.Entities.Direction dir = KiemThe.Entities.Direction.NONE;
				// Mức máu
				int hp = monsterInfo.BaseHP + monsterInfo.HPIncreaseEachLevel * this.CopyScene.Level;
				// Tạo quái
				GameManager.MonsterZoneMgr.AddDynamicMonsters(this.CopyScene.MapCode, monsterInfo.ID, this.CopyScene.ID, 1, monsterInfo.PosX, monsterInfo.PosY, monsterInfo.Name, monsterInfo.Title, hp, this.CopyScene.Level, dir, series, monsterInfo.AIType, monsterInfo.RespawnTicks, -1, monsterInfo.AIScriptID, null, null, 65535);
			}
		}
		#endregion
	}
}
