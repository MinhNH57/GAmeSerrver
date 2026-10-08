using GameServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace GameServer.KiemThe.Core.Repute
{
    public class ReputeManager
    {
        public static string KT_Repute_XML = "Config/KT_Repute/repute.xml";

        public static Repute _ReputeConfig = new Repute();

        /// <summary>
        /// Loading all Drop
        /// </summary>
        public static void Setup()
        {
            Console.WriteLine("Loading KT_Repute Profile..");

            string Files = Global.GameResPath(KT_Repute_XML);

            using (var stream = System.IO.File.OpenRead(Files))
            {
                var serializer = new XmlSerializer(typeof(Repute));
                _ReputeConfig = serializer.Deserialize(stream) as Repute;
            }
        }


        public static int GetTotalEXp(int CampInput,int ClassInput,int LevelInput)
        {
            int SUM = 0;


            Camp _Camp = _ReputeConfig.Camp.Where(x => x.Id == CampInput).FirstOrDefault();


            if(_Camp!=null)
            {
                Class _Class = _Camp.Class.Where(x => x.Id == ClassInput).FirstOrDefault();

                List<Level> TotalLevel = _Class.Level.Where(x => x.Id < LevelInput).ToList();

                 SUM = TotalLevel.Sum(x => x.LevelUp);
            }

            return SUM;

        }
        public static string GetNameReputeClass(KPlayer player, int PointType)
        {
            string reputeName = "";
            var FindPoint = player.GetRepute().Where(x => x.DBID == PointType).FirstOrDefault();

            if (FindPoint != null)
            {
                int LevelCur = FindPoint.Level;

                int _Class = FindPoint.Class;

                int _Camp = FindPoint.Camp;

                Camp _CampFind = ReputeManager._ReputeConfig.Camp.Where(x => x.Id == _Camp).FirstOrDefault();
                if (_CampFind != null)
                {
                    Class _ClassFind = _CampFind.Class.Where(x => x.Id == _Class).FirstOrDefault();
                    if (_ClassFind != null)
                    {
                        reputeName = _ClassFind.Name;
                    }
                }
            }
                        
            return reputeName;
        }

        public static string GetReputeCampAndClass(int reputeType)
        {
            var _camp = reputeType / 100;
            var _class = reputeType % 100;
            var campFind = ReputeManager._ReputeConfig.Camp.Where(x => x.Id == _camp).FirstOrDefault();
            if (campFind == null)
            {
                return "";
            }

            var classFind = campFind.Class.Where(x => x.Id == _class).FirstOrDefault();
            if (classFind == null)
                return "";

            return classFind.Name;
        }




    }
}