using System.Collections.Generic;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Suggestion;
using Terraria;

namespace MoreCommands.ArgumentTypes;

public class PositionArgumentType : ArgumentType<PositionArgumentType.PositionArgument>
{
    private static readonly SimpleCommandExceptionType Missing = new(new LiteralMessage("Position requires two coordinates."));
    private static readonly SimpleCommandExceptionType OutsideOfWorld = new(new LiteralMessage($"The given position was outside of the world ({Main.ActiveWorldFileData.WorldSizeX}x{Main.ActiveWorldFileData.WorldSizeY})."));
    private readonly bool _tilePos;

    private PositionArgumentType(bool tilePos) => _tilePos = tilePos;

    public static PositionArgumentType Pos => new(false);

    public static PositionArgumentType TilePos => new(true);
    
    public override PositionArgument Parse(IStringReader reader)
    {
        int i = reader.Cursor;
        CoordinateArgument coordArgX = CoordinateArgument.Parse(reader, _tilePos);
        if (!reader.CanRead() || reader.Peek() != ' ') {
            reader.Cursor = i;
            throw Missing.CreateWithContext(reader);
        }

        if (!coordArgX.Relative && coordArgX.Value > Main.ActiveWorldFileData.WorldSizeX)
            throw OutsideOfWorld.CreateWithContext(reader);
        
        reader.Skip();
        CoordinateArgument coordArgY = CoordinateArgument.Parse(reader, _tilePos);
        if (!coordArgY.Relative && coordArgY.Value > Main.ActiveWorldFileData.WorldSizeY)
            throw OutsideOfWorld.CreateWithContext(reader);

        return new PositionArgument(coordArgX, coordArgY);
    }

    public override Task<Suggestions> ListSuggestions<TS>(CommandContext<TS> context, SuggestionsBuilder builder) => builder.BuildFuture();

    public override IEnumerable<string> Examples => new List<string> {"~ ~"};

    public static (float x, float y) GetPosition(CommandContext<CommandSource> ctx, string argName)
    {
        PositionArgument arg = ctx.GetArgument<PositionArgument>(argName);

        (float x, float y) = arg.ToAbsolutePosition(ctx.Source.Pos.X, ctx.Source.Pos.Y);
        if (x < 0 || x > Main.ActiveWorldFileData.WorldSizeX * 16 || y < 0 || y > Main.ActiveWorldFileData.WorldSizeY * 16)
            throw OutsideOfWorld.Create();

        return (x, y);
    }

    public static Vector2 GetPositionVec(CommandContext<CommandSource> ctx, string argName)
    {
        (float x, float y) = GetPosition(ctx, argName);
        return new Vector2(x, y);
    }
     
    public record CoordinateArgument(float Value, bool Relative, bool TilePos)
    {
        private static readonly SimpleCommandExceptionType Negative = new(new LiteralMessage("Coordinates cannot be negative."));
        
        public float ToAbsoluteValue(float offset) => Relative ? offset / (TilePos ? 16 : 1) + Value * (TilePos ? 16 : 1) : Value * (TilePos ? 16 : 1);

        public static CoordinateArgument Parse(IStringReader reader, bool tilePos)
        {
            if (!reader.CanRead())
                throw Missing.CreateWithContext(reader);

            bool relative = false;
            if (reader.Peek() == '~')
            {
                relative = true;
                reader.Skip();
            }

            if (relative && (!reader.CanRead() || reader.Peek() == ' '))
                return new CoordinateArgument(0, true, tilePos);

            int cursor = reader.Cursor;
            float value = reader.ReadFloat();
            string s = reader.String[cursor..reader.Cursor];

            if (relative && s.Length == 0)
                return new CoordinateArgument(0, true, tilePos);
            
            return value < 0 && !relative ? throw Negative.CreateWithContext(reader) : new CoordinateArgument(value, relative, tilePos);
        }
    }

    public record PositionArgument(CoordinateArgument CoordArgX, CoordinateArgument CoordArgY)
    {
        public bool IsTilePos => CoordArgX.TilePos && CoordArgY.TilePos;
        private static readonly SimpleCommandExceptionType PlayerRelative = new(new LiteralMessage("You must be a player to use relative positions."));
    
        public (float x, float y) ToAbsolutePosition(float? offsetX, float? offsetY)
        {
            if (offsetX is null && CoordArgX.Relative || offsetY is null && CoordArgY.Relative)
                throw PlayerRelative.Create();
        
            return (CoordArgX.ToAbsoluteValue(offsetX ?? 0), CoordArgY.ToAbsoluteValue(offsetY ?? 0));
        }
    }
}