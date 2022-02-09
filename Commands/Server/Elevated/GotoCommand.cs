using System;
using System.Collections.Generic;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Server.Elevated;

public class GotoCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override bool Console => false;
    public override string Description => "Teleport to various locations in the world quickly.";
    
    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        IDictionary<string, Func<Player, (int x, int y)?>> locations = new Dictionary<string, Func<Player, (int x, int y)?>>
        {
            {"temple", GetTemplePos},
            {"dungeon", GetDungeonPos},
            {"hell", GetHellPos},
            {"spawn", GetSpawnPos},
            {"random", GetRandomPos}
        };

        LiteralArgumentBuilder<CommandSource> gotoCmd = Literal("goto");
        foreach ((string key, Func<Player, (int x, int y)?> value) in locations)
            gotoCmd.Then(Literal(key)
                .Executes(ctx =>
                {
                    (int x, int y)? pos = value(ctx.Source.Player)!;

                    if (pos is null)
                    {
                        Error(ctx, "Could not find a suitable position to teleport to.");
                        return 0;
                    }
                    
                    Util.Teleport(ctx.Source.Player, pos.Value);
                    Reply(ctx, "You have been teleported.");

                    return 1;
                }));

        dispatcher.Register(gotoCmd);
    }

    // Thanks to Cheat Sheet for help with code for these methods.
    // https://github.com/JavidPack/CheatSheet/blob/1.4/Menus/QuickTeleportHotbar.cs
    private static (int x, int y)? GetTemplePos(Player player)
    {
        Vector2 prePos = player.position;
        Vector2 pos = prePos;
                
        for (int x = 0; x < Main.maxTilesX; ++x)
            for (int y = 0; y < Main.maxTilesY; ++y)
            {
                if (Main.tile[x, y].TileType != TileID.LihzahrdAltar) continue;
                                
                pos = new Vector2((x + 2) * 16, y * 16);
                break;
            }

        return pos == prePos ? null : ((int) pos.X, (int) pos.Y);
    }

    private static (int x, int y)? GetDungeonPos(Player player) => (Main.dungeonX * 16 + 8 - player.width / 2, Main.dungeonY * 16 - player.height);

    private static (int x, int y)? GetSpawnPos(Player player) => (Main.spawnTileX * 16 + 8 - player.width / 2, Main.spawnTileY * 16 - player.height);

    private static (int x, int y)? GetHellPos(Player player)
    {
        int findTeleportDestinationAttempts = 0;
        int width = player.width;
        int height = player.height;

        while (findTeleportDestinationAttempts++ < 1000)
        {
            int tileX = Main.rand.Next(Main.maxTilesX - 200);
            int tileY = Main.rand.Next(Main.maxTilesY - 200, Main.maxTilesY);
            Vector2 teleportPosition = new Vector2(tileX, tileY) * 16f + new Vector2((float)(-width / 2.0 + 8.0), -height);
            
            if (Collision.SolidCollision(teleportPosition, width, height)) continue;

            if (Main.tile[tileX, tileY].WallType == 87 && !(tileY <= Main.worldSurface) && !NPC.downedPlantBoss || 
                Main.wallDungeon[Main.tile[tileX, tileY].WallType] && !(tileY <= Main.worldSurface) && !NPC.downedBoss3) continue;
            
            int num4 = 0;
            while (num4 < 100 && WorldGen.InWorld(tileX, tileY + num4, 20))
            {
                Tile tile = Main.tile[tileX, tileY + num4];
                teleportPosition = new Vector2(tileX, tileY + num4) * 16f + new Vector2((float)(-(double)width / 2.0 + 8.0), -height);
                Collision.SlopeCollision(teleportPosition, player.velocity, width, height, player.gravDir);
                bool flag2 = !Collision.SolidCollision(teleportPosition, width, height);
                        
                if (flag2 || !tile.HasTile || !Main.tileSolid[tile.TileType]) ++num4;
                else break;
            }

            if (Collision.LavaCollision(teleportPosition, width, height) || !(Collision.HurtTiles(teleportPosition, player.velocity, width, height).Y <= 0.0)) continue;
            Collision.SlopeCollision(teleportPosition, player.velocity, width, height, player.gravDir);

            if (!Collision.SolidCollision(teleportPosition, width, height) || num4 >= 99) continue;
            Vector2 velocity1 = Vector2.UnitX * 16f;

            if (Collision.TileCollision(teleportPosition - velocity1, velocity1, width, height, false, false, (int)player.gravDir) != velocity1) continue;
            Vector2 velocity2 = -Vector2.UnitX * 16f;

            if (Collision.TileCollision(teleportPosition - velocity2, velocity2, width, height, false, false, (int)player.gravDir) != velocity2) continue;
            Vector2 velocity3 = Vector2.UnitY * 16f;

            if (Collision.TileCollision(teleportPosition - velocity3, velocity3, width, height, false, false, (int)player.gravDir) != velocity3) continue;
            Vector2 velocity4 = -Vector2.UnitY * 16f;

            if (Collision.TileCollision(teleportPosition - velocity4, velocity4, width, height, false, false, (int)player.gravDir) != velocity4) continue;

            return ((int) teleportPosition.X, (int) teleportPosition.Y);
        }

        return null;
    }

    private static (int x, int y)? GetRandomPos(Player player)
    {
        bool canSpawn = false;
        const int teleportStartX = 100;
        int teleportRangeX = Main.maxTilesX - 200;
        const int teleportStartY = 100;
        int underworldLayer = Main.UnderworldLayer;
        
        Vector2 pos = player.CheckForGoodTeleportationSpot(ref canSpawn, teleportStartX, teleportRangeX, teleportStartY, underworldLayer, new Player.RandomTeleportationAttemptSettings()
        {
            avoidLava = true,
            avoidHurtTiles = true,
            maximumFallDistanceFromOrignalPoint = 100,
            attemptsBeforeGivingUp = 1000
        });

        return canSpawn ? ((int) pos.X, (int) pos.Y) : null;
    }
}