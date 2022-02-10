using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static Terraria.ID.TileID;
using static Terraria.ID.WallID;

namespace MoreCommands.Commands.Server.Elevated;

public class CleanseCommand : Command
{
    public override CommandType Type => CommandType.World;
    public override string Description => "Cleanses the entire world or an area of the given radius around you.";
    
    // Gets bad blocks to avoid spread.
    private static readonly IDictionary<ushort, ushort> TileBlacklist = new Dictionary<ushort, ushort>
    {
        // Corruption
        [CorruptGrass] = TileID.Grass,
        [Ebonstone] = TileID.Stone,
        [Ebonsand] = Sand,
        [CorruptIce] = IceBlock,
        [TileID.CorruptHardenedSand] = TileID.HardenedSand,
        [TileID.CorruptSandstone] = TileID.Sandstone,

        // Crimson
        [CrimsonGrass] = TileID.Grass,
        [Crimstone] = TileID.Stone,
        [Crimsand] = Sand,
        [FleshIce] = IceBlock,
        [CrimsonVines] = Vines,
        [TileID.CrimsonHardenedSand] = TileID.HardenedSand,
        [TileID.CrimsonSandstone] = TileID.Sandstone
    }.ToImmutableDictionary();
    // Optionally remove Hallow as well.
    private static readonly IDictionary<ushort, ushort> HallowTileBlacklist = new Dictionary<ushort, ushort>
    {
        [HallowedGrass] = TileID.Grass,
        [Pearlstone] = TileID.Stone,
        [Pearlsand] = Sand,
        [HallowedIce] = IceBlock,
        [HallowedVines] = Vines,
        [TileID.HallowHardenedSand] = TileID.HardenedSand,
        [TileID.HallowSandstone] = TileID.Sandstone,
    }.ToImmutableDictionary();
    // Gets the bad walls because it spreads on walls too.
    private static readonly IDictionary<ushort, ushort> WallBlacklist = new Dictionary<ushort, ushort>
    {
        // Corrupt
        [EbonstoneUnsafe] = WallID.Stone,
        [CorruptGrassUnsafe] = WallID.Grass,
        [WallID.CorruptHardenedSand] = WallID.HardenedSand,
        [WallID.CorruptSandstone] = WallID.Sandstone,
        
        // Crimson
        [CrimstoneUnsafe] = WallID.Stone,
        [WallID.CrimsonHardenedSand] = WallID.HardenedSand,
        [CrimsonGrassUnsafe] = WallID.Grass,
        [WallID.CrimsonSandstone] = WallID.Sandstone,
    }.ToImmutableDictionary();
    // Optionally remove Hallow as well.
    private static readonly IDictionary<ushort, ushort> HallowWallBlacklist = new Dictionary<ushort, ushort>
    {
        [PearlstoneBrickUnsafe] = WallID.Stone,
        [HallowedGrassUnsafe] = WallID.Grass,
        [WallID.HallowHardenedSand] = WallID.HardenedSand,
        [WallID.HallowSandstone] = WallID.Sandstone
    }.ToImmutableDictionary();

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteralReq("cleanse")
            .Executes(ctx=> Execute(ctx, null, false))
            .Then(Argument("radius", Arguments.Integer(1))
                .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("radius"), false))
                .Then(Argument("removeHallow", Arguments.Bool())
                    .Executes(ctx => Execute(ctx, ctx.GetArgument<int>("radius"), ctx.GetArgument<bool>("removeHallow")))))
            .Then(Argument("removeHallow", Arguments.Bool())
                .Executes(ctx => Execute(ctx, null, ctx.GetArgument<bool>("removeHallow")))));
    }

    private static int Execute(CommandContext<CommandSource> ctx, int? radius, bool removeHallow)
    {
        int count = 0;
        Vector2 center = ctx.Source.TilePos;
        IDictionary<ushort, ushort> tileBlacklist = removeHallow ? TileBlacklist.Concat(HallowTileBlacklist).ToImmutableDictionary() : TileBlacklist;
        IDictionary<ushort, ushort> wallBlacklist = removeHallow ? WallBlacklist.Concat(HallowWallBlacklist).ToImmutableDictionary() : WallBlacklist;
        
        int xMin = radius is null ? 0 : (int) center.X - radius.Value;
        int yMin = radius is null ? 0 : (int) center.Y - radius.Value;
        int xMax = radius is null ? Main.tile.Width : (int) center.X + radius.Value;
        int yMax = radius is null ? Main.tile.Height : (int) center.Y + radius.Value;
        
        MoreCommands.Log.Debug($"x min: {xMin}, x max: {xMax}, y min: {yMin}, y max: {yMax}");
        
        for (int x = xMin; x < xMax; x++)
        for (int y = yMin; y < yMax; y++)
        {
            if (radius is not null && ctx.Source.TilePos.DistanceSQ(new Vector2(x, y)) > radius * radius) continue;
            
            Tile tile = Main.tile[x, y];
            bool incrementedCount = false;
            
            if (tileBlacklist.ContainsKey(tile.TileType))
            {
                tile.TileType = tileBlacklist[tile.TileType];
                count++;
                incrementedCount = true;
            }

            if (!wallBlacklist.ContainsKey(tile.WallType)) continue;
            tile.WallType = wallBlacklist[tile.WallType];
            if (!incrementedCount)
                count++;
        }
        
        Reply(ctx, $"Successfully cleansed {Coloured(count)} tile{(count == 1 ? "" : "s")} in {Coloured(radius == null ? "the world" : $"a radius of {radius.Value} tiles")}.");
        return count;
    }
}