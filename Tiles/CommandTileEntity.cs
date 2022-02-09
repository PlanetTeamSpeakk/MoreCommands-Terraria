using System.IO;
using Microsoft.Xna.Framework;
using MoreCommands.IL.Detours;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MoreCommands.Tiles;

public class CommandTileEntity : ModTileEntity
{
    public static CommandTileEntity CurrentlyEditing { get; private set; }
    public string Command
    {
        get => _command;
        set
        {
            if (value.StartsWith("/")) value = value[1..];
            _command = value;
            
            SendUpdate();
        }
    }
    public string LastOutput
    {
        get => _lastOutput;
        private set
        {
            _lastOutput = value;
            
            SendUpdate();
        }
    }
    private string _command = "", _lastOutput = "";
    private CommandTileEntityCommandCaller _caller;
    
    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Main.tile[x, y];

        return tile.HasTile && tile.TileType == ModContent.TileType<CommandTile>();
    }

    public override void SaveData(TagCompound tag)
    {
        tag.Set("command", Command);
        tag.Set("lastOutput", LastOutput);
    }

    public override void LoadData(TagCompound tag)
    {
        if (tag.ContainsKey("command"))
            _command = tag.GetString("command") ?? "";

        if (tag.ContainsKey("lastOutput"))
            _lastOutput = tag.GetString("lastOutput") ?? "";
        
        if (tag.ContainsKey("command") || tag.ContainsKey("lastOutput"))
            SendUpdate();
    }

    public override void OnNetPlace() => SendUpdate();

    private void SendUpdate()
    {
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
    }

    public override int Hook_AfterPlacement(int i, int j, int type0, int style, int direction, int alternate)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return Place(i, j);
        NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 2);
        NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);

        return Place(i, j);
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Command ?? "");
        writer.Write(LastOutput ?? "");
    }

    public override void NetReceive(BinaryReader reader)
    {
        _command = reader.ReadString();
        _lastOutput = reader.ReadString();
    }

    public void RunCommand()
    {
        if (string.IsNullOrEmpty(Command)) return;
        CommandLoaderDetours.HandleCommand(Command, _caller ??= new CommandTileEntityCommandCaller(this));
    }

    public bool OpenEditUI()
    {
        Player player = Main.LocalPlayer;
        Main.mouseRightRelease = false;

        // Making sure no active things are ongoing when opening the UI.
        if (player.sign > -1)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            player.sign = -1;
            Main.editSign = false;
            Main.npcChatText = string.Empty;
        }
        if (Main.editChest)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.editChest = false;
            Main.npcChatText = string.Empty;
        }
        if (player.editedChestName)
        {
            NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
            player.editedChestName = false;
        }
        if (player.talkNPC > -1)
        {
            player.SetTalkNPC(-1);
            Main.npcChatCornerItem = 0;
            Main.npcChatText = string.Empty;
        }

        CurrentlyEditing = this;
        SoundEngine.PlaySound(SoundID.MenuOpen);
        MoreCommands.CommandTileUI.Activate();
        MoreCommands.CommandTileInterface?.SetState(MoreCommands.CommandTileUI);

        return true; // To simplify CommandTile::RightClick(int i, int j);
    }

    public static void CloseEditUI()
    {
        if (CurrentlyEditing is null) return;
        
        SoundEngine.PlaySound(SoundID.MenuClose);
        MoreCommands.CommandTileInterface.SetState(null);
        MoreCommands.CommandTileUI.Deactivate();
        CurrentlyEditing = null;
    }
    
    public class CommandTileEntityCommandCaller : CommandCaller
    {
        public CommandType CommandType => CommandType.Console;
        public Player Player => null;
        public readonly CommandTileEntity TileEntity;
        
        public CommandTileEntityCommandCaller(CommandTileEntity tileEntity) => TileEntity = tileEntity;

        public void Reply(string text, Color color = new()) => TileEntity.LastOutput = text;
    }
}