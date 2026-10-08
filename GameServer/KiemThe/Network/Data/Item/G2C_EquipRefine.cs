using ProtoBuf;
using System.Collections.Generic;

namespace Server.Data
{
	/// <summary>
	/// Luyện hóa trang bị
	/// </summary>
	[ProtoContract]
	public class G2C_EquipRefine
	{
		/// <summary>
		/// DbID trang bị
		/// </summary>
		[ProtoMember(1)]
		public int EquipDbID { get; set; }

		/// <summary>
		/// DbID công thức
		/// </summary>
		[ProtoMember(2)]
		public int RecipeDbID { get; set; }

		/// <summary>
		/// Danh sách Huyền Tinh
		/// </summary>
		[ProtoMember(3)]
		public List<int> CrystalStoneDbIDs { get; set; }

		/// <summary>
		/// ID sản phẩm được chọn
		/// </summary>
		[ProtoMember(4)]
		public int ProductGoodsID { get; set; }
	}

	[ProtoContract]
	public class G2C_CNRefine
	{

		/// <summary>
		/// DbID trang bị
		/// </summary>
		[ProtoMember(1)]
		public int EquipDbID { get; set; }

		/// <summary>
		/// DbID công thức
		/// </summary>
		[ProtoMember(2)]
		public int RecipeDbID { get; set; }

		/// <summary>
		/// Danh sách Huyền Tinh
		/// </summary>
		[ProtoMember(3)]
		public List<int> CrystalStoneDbIDs { get; set; }

		/// <summary>
		/// ID sản phẩm được chọn
		/// </summary>
		[ProtoMember(4)]
		public string PropName { get; set; }
	}

	[ProtoContract]
	public class G2C_CNAHRefine
	{
		/// <summary>
		/// Chân nguyên 1
		/// </summary>
		[ProtoMember(1)]
		public int CN1ID { get; set; }

		/// <summary>
		/// Chân nguyên 2
		/// </summary>
		[ProtoMember(2)]
		public int CN2ID { get; set; }

		/// <summary>
		/// Chân nguyên 3
		/// </summary>
		[ProtoMember(3)]
		public int CN3ID { get; set; }

		/// <summary>
		/// Bồ đề quả
		/// </summary>
		[ProtoMember(4)]
		public int BDQID { get; set; }
	}

	[ProtoContract]
	public class G2C_CNUnBinding
	{
		[ProtoMember(1)]
		public List<int> ChanNguyenDbIDs { get; set; }
	}
}
