using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class ItemIdCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Gives the id of the item you're holding or the item passed.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("itemid")
            .Executes(ctx =>
            {
                Item item = ctx.Source.Player.inventory[ctx.Source.Player.selectedItem];
        
                Reply(ctx, item.type == ItemID.None ? "You are holding nothing (0)." : $"You are holding a(n) {IdHelper.GetName(IdType.Item, item.type)} ({item.type}).", MoreCommands.DefColour);
                return item.type;
            })
            .Then(Argument("itemname", Arguments.GreedyString())
                .Executes(ctx => IdHelper.SearchCommand(ctx, "itemname", IdType.Item))));
    }
}