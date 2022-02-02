using Brigadier.NET;
using MoreCommands.Misc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class WipeOutCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Kill every NPC in the world, excluding townfolk.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("wipeout")
            .Executes(ctx =>
            {
                int count = 0;

                foreach (NPC npc in Main.npc)
                {
                    if (npc.isLikeATownNPC || npc.type == NPCID.None) continue;

                    npc.life = 0;
                    count++;
                }

                Reply(ctx, $"{count} NPC{(count == 1 ? "" : "s")} have been killed.");
                return count;
            }));
    }
}