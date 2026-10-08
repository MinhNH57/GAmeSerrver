
using System.Collections.Generic;
using GameServer.KiemThe.GameEvents.BaoThach;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        private Dictionary<string, BaoThachUserData.Hole> _cacheResetBT =
            new Dictionary<string, BaoThachUserData.Hole>();
        private string GetResetKey(int v1, int v2)
        {
            return v1 + "&&" + v2;
        }
        public void AddResetQueue(int itemDBId, int indexHole, BaoThachUserData.Hole hole)
        {
            var key = GetResetKey(itemDBId, indexHole);
            if (_cacheResetBT.ContainsKey(key))
            {
                _cacheResetBT[key] = hole;
            }
            else
            {
                _cacheResetBT.Add(key, hole);
            }
        }
        public BaoThachUserData.Hole GetResetQueue(int itemDBId, int indexHole)
        {
            var key = GetResetKey(itemDBId, indexHole);
            if(_cacheResetBT.ContainsKey(key))
            {
                return _cacheResetBT[key];
            }
            return null;
        }
    }
}