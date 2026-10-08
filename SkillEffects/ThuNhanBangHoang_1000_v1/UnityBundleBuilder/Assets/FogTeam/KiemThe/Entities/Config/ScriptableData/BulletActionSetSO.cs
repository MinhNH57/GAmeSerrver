using System.Collections.Generic;
using UnityEngine;

namespace FogTeam.KiemThe.Entities.Config.ScriptableData
{
    public class BulletActionSetSO : ScriptableObject
    {
        public string SoundBundleDir;
        public List<BulletActionSetXML.BulletResData> data;
    }
}