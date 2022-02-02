using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoreCommands.Misc;
using MoreCommands.Tiles;
using MoreCommands.Utils;
using ReLogic.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace MoreCommands.Hooks;

public class SystemHooks : ModSystem
{
    public IList<string> Operators => _operators.ToImmutableList();
    public GameTime LastGameTime { get; private set; }
    private readonly IList<string> _operators = new List<string>();

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int i = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text"); // Draw UIs after the cursor and related text have been drawn.

        layers.Insert(i, new LegacyGameInterfaceLayer("MoreCommands: command tile", delegate
        {
            if (LastGameTime is not null && MoreCommands.CommandTileInterface?.CurrentState is not null)
                MoreCommands.CommandTileInterface.Draw(Main.spriteBatch, LastGameTime);
            
            return true;
        }, InterfaceScaleType.UI));
        this.PostSetupContent();
    }

    public override void PostUpdateInput()
    {
        foreach (Command command in MoreCommands.Commands)
            command.OnUpdate();

        if (MoreCommands.Rainbow)
            Filters.Scene["MCRainbow"].GetShader().UseTargetPosition(
                new Vector2(Main.MouseScreen.X / Main.screenWidth, Main.MouseScreen.Y / Main.screenHeight) +
                new Vector2(Main.offScreenRange, Main.offScreenRange)); // offScreenRange gets subtracted causing the entire shader to completely fail to make a radial gradient.
    }

    public override void UpdateUI(GameTime time)
    {
        MoreCommands.CommandTileInterface.Update(LastGameTime = time);
        MoreCommands.SuggestionsInterface.Update(time);
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag.Add("Operators", Operators);
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (!tag.ContainsKey("Operators")) return;
        
        _operators.Clear();
        tag.Get<List<string>>("Operators").ForEach(_operators.Add);
    }

    public void Op(int player)
    {
        string identifier = Util.GetAddress(player).GetIdentifier();

        if (_operators.Contains(identifier)) return;
        _operators.Add(identifier);
        SendOpPacket(player, true);
    }

    public void Deop(int player)
    {
        string identifier = Util.GetAddress(player).GetIdentifier();
        
        if (_operators.Contains(identifier)) return;
        _operators.Remove(identifier);
        SendOpPacket(player, false);
    }

    private static void SendOpPacket(int player, bool isOp)
    {
        ModPacket packet = MoreCommands.Instance.GetPacket();
        packet.Write((byte) 0);
        packet.Write(isOp);
        packet.Send(player);
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (messageType != MessageID.TileEntitySharing || Main.netMode != NetmodeID.Server) return false;
        
        long startPos = reader.BaseStream.Position;
        int id = reader.ReadInt32();
        reader.BaseStream.Position = startPos;
        
        // Player tried to set content of Command Tile, but is not an operator; ignore the packet and inform the player.
        if (!TileEntity.ByID.ContainsKey(id) || TileEntity.ByID[id].type != ModContent.TileEntityType<CommandTileEntity>() || MoreCommands.IsOp(playerNumber)) return false;
        ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral("You must be an operator to set the content of or place command tiles."), Color.Red, playerNumber);
        return true;
    }
}