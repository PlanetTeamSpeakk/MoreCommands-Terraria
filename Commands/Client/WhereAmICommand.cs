using System;
using Brigadier.NET;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class WhereAmICommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override bool Console => false;
    public override string Description => "Tells you where in the world you are.";

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("whereami")
            .Executes(ctx =>
            {
                float x = ctx.Source.Player.Center.X;
                float y = ctx.Source.Player.Center.Y;

                int lat = (int) (x * 2.0 / 16.0 - Main.maxTilesX);
                int lon = (int) (y * 2.0 / 16.0 - Main.worldSurface * 2.0);
                
                float spaceDistance = (float) ((y / 16.0 - (65.0 + 10.0 * (Main.maxTilesX / 4200f) * (Main.maxTilesX / 4200f))) / (Main.worldSurface / 5.0));
                
                string layer = y > (double) ((Main.maxTilesY - 204) * 16) ? Language.GetTextValue("GameUI.LayerUnderworld") : 
                    y > Main.rockLayer * 16.0 + 600 + 16.0 ? Language.GetTextValue("GameUI.LayerCaverns") : 
                    lon > 0 ? Language.GetTextValue("GameUI.LayerUnderground") : spaceDistance < 1.0 ? Language.GetTextValue("GameUI.LayerSpace") : Language.GetTextValue("GameUI.LayerSurface");
                string[] zones = Util.GetZones(ctx.Source.Player);
                
                Reply(ctx, $"Your location is {(int) x / 16} X, {(int) y / 16} Y ({Math.Abs(lat)}' {(lat > 0 ? "East" : lat < 0 ? "West" : "Center")}, {Math.Abs(lon)}' {layer}) " +
                           $"in zone{(zones.Length == 1 ? "" : "s")} {JoinNicely(zones)}.");

                return 1;
            }));
    }
}