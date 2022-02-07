using Microsoft.Xna.Framework;
using MoreCommands.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace MoreCommands.Tiles;

public class CommandTile : ModTile
{
    public override string Texture => "MoreCommands/Assets/CommandTile";

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;

        TileObjectData tod = TileObjectData.newTile;
        tod.CopyFrom(TileObjectData.Style2x2);
        tod.Origin = new Point16(0, 0);
        TileObjectData.addTile(Type);

        ModTranslation name = CreateMapEntryName();
        name.SetDefault("Command Tile");
        AddMapEntry(Util.ColourFromRGBInt(0xcfa68b), name);
    }

    public override bool HasSmartInteract() => true;

    public override void PlaceInWorld(int i, int j, Item item) => GetTe().Place(i, j);

    public override bool RightClick(int i, int j) => GetTeAt(i, j)?.OpenEditUI() ?? false;

    public override void HitWire(int i, int j)
    {
        GetTeAt(i, j)?.RunCommand();

        (int x, int y) = GetTopLeftCorner(i, j);
        // Skipping wires so that this multitile doesn't get triggered
        // more than once when a wire goes through it multiple times.
        Wiring.SkipWire(x + 0, y + 0); // Binary 0 (00)
        Wiring.SkipWire(x + 0, y + 1); // 1 (01)
        Wiring.SkipWire(x + 1, y + 0); // 2 (10)
        Wiring.SkipWire(x + 1, y + 1); // 3 (11) :)
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        (int x, int y) = GetTopLeftCorner(i, j);
        GetTe().Kill(x,y);
        Item.NewItem(i * 18, j * 16, 32, 32, ModContent.ItemType<CommandTileItem>());
    }

    public override void MouseOver(int i, int j) {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<CommandTileItem>();
    }
    
    private static CommandTileEntity GetTe() => ModContent.GetInstance<CommandTileEntity>();

    private static CommandTileEntity GetTeAt(int x, int y)
    {
        int id = GetTe().Find(GetTopLeftCorner(x, y).x, GetTopLeftCorner(x, y).y);
        return id == -1 ? null : (CommandTileEntity) TileEntity.ByID[id];
    }

    private static (int x, int y) GetTopLeftCorner(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        return (x - tile.frameX / 18, y - tile.frameY / 18);
    }

    public class CommandTileItem : ModItem
    {
        public override string Texture => "MoreCommands/Assets/CommandTileItem";
        
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DirtBlock);
            Item.createTile = ModContent.TileType<CommandTile>();
            
            DisplayName.SetDefault("Command Tile");
        }
    }
}