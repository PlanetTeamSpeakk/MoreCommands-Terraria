using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brigadier.NET;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.ArgumentTypes;
using MoreCommands.Extensions;
using MoreCommands.IL;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MoreCommands.Commands.Server.Elevated;

public class NPCBlacklistCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Prevent certain types of NPCs from spawning in your world.";
    private static readonly HashSet<int> Blacklist = new();

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("npcblacklist")
            .Then(Literal("add")
                .Then(Argument("type", IdArgumentType.Npc)
                    .Executes(ctx =>
                    {
                        int type = ctx.GetArgument<int>("type");
                        if (!Blacklist.Contains(type)) Blacklist.Add(type);
                        
                        Reply(ctx, $"NPCs of type {Coloured(type)} ({Coloured(IdHelper.GetName(IdType.Npc, type))}) will no longer spawn.");
                        return Blacklist.Count;
                    })))
            
            .Then(Literal("remove")
                .Then(Argument("type", IdArgumentType.Npc)
                    .Executes(ctx =>
                    {
                        int type = ctx.GetArgument<int>("type");
                        if (Blacklist.Contains(type))
                        {
                            Blacklist.Remove(type);
                            Reply(ctx, $"NPCs of type {Coloured(type)} ({Coloured(IdHelper.GetName(IdType.Npc, type))}) will now spawn again.");

                            return Blacklist.Count;
                        }
                        
                        Error(ctx, $"Type {type} ({IdHelper.GetName(IdType.Npc, type)}) is not blacklisted.");
                        return 0;
                    })))
            
            .Then(Literal("list")
                .Executes(ctx =>
                {
                    Reply(ctx, $"Currently blacklisted NPCs: {JoinNicely(Blacklist.Select(type => Coloured(type) + $" ({Coloured(IdHelper.GetName(IdType.Npc, type))})"))}");
                    return Blacklist.Count;
                })));
    }

    public class DataSystemHook : ModSystem
    {
        public override void SaveWorldData(TagCompound tag) => tag.Set("NPC_Blacklist", Blacklist);

        public override void LoadWorldData(TagCompound tag)
        {
            if (!tag.ContainsKey("NPC_Blacklist")) return;
            
            Blacklist.Clear();
            tag.Get<List<int>>("NPC_Blacklist").ForEach(id => Blacklist.Add(id));
        }
    }
    
    public class NPCBlacklistManipulation : ILManipulation
    {
        public override MethodBase Target => Util.GetMethod(typeof(NPC), "NewNPC");
        public override IEnumerable<ILMove> Movements => Array.Empty<ILMove>();
        
        public override void Inject(ILCursor c)
        {
            ILLabel label = c.DefineLabel();
            c.EmitDelegate((int type) => Blacklist.Contains(type), ILParameter.LoadArg(2));
            c.Emit(OpCodes.Brfalse, label); // Jump to the original code if type is not in blacklist.
            c.Emit(OpCodes.Ldc_I4, 200); // Load constant 200 as 4-byte int.
            c.Emit(OpCodes.Ret); // Return 200
            c.MarkLabel(label); // Mark the label on the next instruction (original code)
        }
    }
}