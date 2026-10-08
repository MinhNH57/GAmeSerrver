using GameServer.Logic;

namespace GameServer.Core.GameEvent.EventOjectImpl
{
    /// <summary>
    /// 怪物死亡事件(人打怪，怪死了)
    /// </summary>
    public class MonsterBloodChangeEventObject : EventObject
    {
        public Monster victim;
        public KPlayer attacker;
        public int oldHP;
        public int newHP;


        public MonsterBloodChangeEventObject(Monster victim, KPlayer attacker, int oldHP, int newHP)
            : base((int)EventTypes.MonsterBloodChanged)
        {
            this.victim = victim;
            this.attacker = attacker;
            this.oldHP = oldHP;
            this.newHP = newHP;
        }

    }
}