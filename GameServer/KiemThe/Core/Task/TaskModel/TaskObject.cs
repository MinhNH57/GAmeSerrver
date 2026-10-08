using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Task
{


	public class TaskCallBack
    {
		public int SelectID { get; set; }

		public Dictionary<int, string> OtherParams { get; set; }

	}


	[XmlRoot(ElementName = "Task")]
	public class Task
	{
		[XmlAttribute(AttributeName = "ID")]
		public int ID { get; set; }
		[XmlAttribute(AttributeName = "TaskClass")]
		public int TaskClass { get; set; }
		[XmlAttribute(AttributeName = "Title")]
		public string Title { get; set; }
	
		[XmlAttribute(AttributeName = "PrevTask")]
		public int PrevTask { get; set; }
		[XmlAttribute(AttributeName = "NextTask")]
		public int NextTask { get; set; }
	
		[XmlAttribute(AttributeName = "MinLevel")]
		public int MinLevel { get; set; }
	
		[XmlAttribute(AttributeName = "MaxLevel")]
		public int MaxLevel { get; set; }
		[XmlAttribute(AttributeName = "SexCondition")]
		public int SexCondition { get; set; }
		[XmlAttribute(AttributeName = "OccupCondition")]
		public int OccupCondition { get; set; }

		[XmlAttribute(AttributeName = "AcceptTalk")]
		public string AcceptTalk { get; set; }
		[XmlAttribute(AttributeName = "DoingTalk")]
		public string DoingTalk { get; set; }

		[XmlAttribute(AttributeName = "QuestionTable")]
		public string QuestionTable { get; set; }

		[XmlAttribute(AttributeName = "CompleteTalk")]
		public string CompleteTalk { get; set; }
		[XmlAttribute(AttributeName = "SourceNPC")]
		public int SourceNPC { get; set; }
		[XmlAttribute(AttributeName = "DestNPC")]
		public int DestNPC { get; set; }
		[XmlAttribute(AttributeName = "TargetType")]
		public int TargetType { get; set; }
		[XmlAttribute(AttributeName = "TargetNPC")]
		public int TargetNPC { get; set; }
		[XmlAttribute(AttributeName = "PropsName")]
		public string PropsName { get; set; }
		[XmlAttribute(AttributeName = "FallPercent")]
		public int FallPercent { get; set; }
		[XmlAttribute(AttributeName = "TargetNum")]
		public int TargetNum { get; set; }
		[XmlAttribute(AttributeName = "TargetMapCode")]
		public int TargetMapCode { get; set; }
		[XmlAttribute(AttributeName = "TargetPos")]
		public string TargetPos { get; set; }
		
		[XmlAttribute(AttributeName = "Taskaward")]
		public string Taskaward { get; set; }
		[XmlAttribute(AttributeName = "OtherTaskaward")]
		public string OtherTaskaward { get; set; }
		[XmlAttribute(AttributeName = "RandomTaskaward")]
		public string RandomTaskaward { get; set; }
		[XmlAttribute(AttributeName = "BacKhoa")]
		public int BacKhoa { get; set; }
		[XmlAttribute(AttributeName = "Bac")]
		public int Bac { get; set; }
		[XmlAttribute(AttributeName = "DongKhoa")]
		public int DongKhoa { get; set; }
		[XmlAttribute(AttributeName = "Experienceaward")]
		public int Experienceaward { get; set; }
	
		[XmlAttribute(AttributeName = "Point1")]
		public int Point1 { get; set; }
		[XmlAttribute(AttributeName = "Point2")]
		public int Point2 { get; set; }
		[XmlAttribute(AttributeName = "Point3")]
		public int Point3 { get; set; }
		[XmlAttribute(AttributeName = "Point4")]
		public int Point4 { get; set; }
		[XmlAttribute(AttributeName = "Point5")]
		public int Point5 { get; set; }
		
		[XmlAttribute(AttributeName = "GoodsEndTime")]
		public int GoodsEndTime { get; set; }
		
		[XmlAttribute(AttributeName = "BuffID")]
		public int BuffID { get; set; }
		[XmlAttribute(AttributeName = "Teleports")]
		public string Teleports { get; set; }
		
	}

	[XmlRoot(ElementName = "Tasks")]
	public class Tasks
	{
		[XmlElement(ElementName = "Task")]
		public List<Task> Task { get; set; }
	}

	[XmlRoot(ElementName = "Config")]
	public class Config
	{
		[XmlElement(ElementName = "Tasks")]
		public Tasks Tasks { get; set; }
	}

    /// <summary>
    /// Nhiệm vụ phụ tuyến
    /// </summary>
    [XmlRoot(ElementName = "SideTasks")]
    public class SideTasks
	{
        [XmlElement(ElementName = "Task")]
        public List<SideTask> sideTasks { get; set; }
	}
    /// <summary>
    /// Nhiệm vụ phụ tuyến
    /// </summary>
    [XmlRoot(ElementName = "Task")]
    public class SideTask
	{
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "ID")]
        public int ID { get; set; }
        [XmlAttribute(AttributeName = "Intro")]
        public string Intro { get; set; }
        [XmlAttribute(AttributeName = "AwardText")]
        public string AwardText { get; set; }
        [XmlAttribute(AttributeName = "Type")]
        public int Type { get; set; }
        [XmlAttribute(AttributeName = "EventType")]
        public int EventType { get; set; }
        [XmlAttribute(AttributeName = "NumberItem")]
        public int NumberItem { get; set; }
        [XmlAttribute(AttributeName = "Param")]
        public string Param { get; set; }
		[XmlAttribute(AttributeName = "GoTo")]
		public string GoTo { get; set; }
        [XmlElement(ElementName = "Award")]
        public List<AwardInfo> Award { get; set; }
        [XmlAttribute(AttributeName = "IndexRepeat")]
        public int IndexRepeat { get; set; }
    }

    [XmlRoot(ElementName = "Award")]
    public class AwardInfo
	{
        [XmlAttribute(AttributeName = "ItemID")]
        public int IDItem { get; set; }

        [XmlAttribute(AttributeName = "Number")]
        public int NumberItem { get; set; }
	}
	

}
