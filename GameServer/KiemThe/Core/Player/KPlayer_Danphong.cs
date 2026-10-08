using System.Collections.Generic;
using System.Linq;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic
{
    public partial class KPlayer
    {
        public List<int> _danphongIndex;
        public List<int> _danphongNumber;


        public int GetDanphongNumber(int id)
        {
            List<int> x = new List<int>();
            List<int> y = new List<int>();
            lock (_danphongIndex)
            {
                x = _danphongIndex;
            }

            lock (_danphongNumber)
            {
                y = _danphongNumber;
            }

            int index = x.IndexOf(id);
            if (index == -1)
                return 0;
            return y[index]; //[ id]
        }        
        
        public int[] GetDanphongNumberArray(int baseid)
        {
            int[] result = new int[21];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = GetDanphongNumber(baseid + i);
            }
            return result;
        }

        public void AddDanphongNumber(int id, int amount)
        {
            List<int> x = new List<int>();
            List<int> y = new List<int>();
            lock (_danphongIndex)
            {
                x = _danphongIndex;
            }

            lock (_danphongNumber)
            {
                y = _danphongNumber;
            }

            if (x.IndexOf(id) == -1)
            {
                x.Add(id);
                y.Add(0);
                Global.SaveRoleParamsIntListToDB(this, x, RoleParamName.Danphong_ID, true);
            }

            y[x.IndexOf(id)] += amount;
            Global.SaveRoleParamsIntListToDB(this, y, RoleParamName.Danphong_Total, true);
        }
    }
}