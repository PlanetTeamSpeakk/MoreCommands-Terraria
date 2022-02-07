using MoreCommands.Utils;
using Terraria;
using Terraria.DataStructures;

namespace MoreCommands.Extensions;

public static class EntityExtensions
{
    public static string GetName(this Entity self) => self switch
    {
        Player p => p.name,
        NPC npc => IdHelper.GetName(IdType.Npc, npc.type),
        _ => self.GetType().Name
    };

    public static void Kill(this Entity self, bool checkDead = true)
    {
        switch (self)
        {
            case NPC npc:
            {
                if (npc.life == 0) break;
                
                npc.life = 0;
                if (checkDead)
                    npc.checkDead();
                break;
            }
            case Player p:
                if (p.dead) break;
                
                p.KillMe(PlayerDeathReason.ByCustomReason($"{p.name} was killed."), double.MaxValue, 0);
                break;
        }
    }
}