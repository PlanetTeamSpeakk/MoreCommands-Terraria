using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using MoreCommands.IL;
using MoreCommands.IL.Detours;
using MoreCommands.Misc;
using Newtonsoft.Json;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Net;
using Terraria.ObjectData;
using Terraria.UI.Chat;

namespace MoreCommands.Utils;

public static class Util
{
    public static readonly Random Random = new();
    public static Color RandomBrightColour => GetRandomBrightColour(null);
    public static readonly Func<IDictionary<string, List<ModCommand>>> GetCommands = Dynamics.CreateStaticGetter<IDictionary<string, List<ModCommand>>>(typeof(CommandLoader), "Commands");
    public static string ConfigDirPath => Main.dedServ ? ConfigManager.ServerModConfigPath : ConfigManager.ModConfigPath; 
    private static readonly PropertyInfo[] ZoneProps = typeof(Player).GetProperties().Where(prop => prop.Name.StartsWith("Zone")).ToArray();
    private static readonly ConstructorInfo LocalizedTextCtor = typeof(LocalizedText).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

    public static string GetGameModeName(int gameMode) => gameMode switch
    {
        0 => "Classic",
        1 => "Expert",
        2 => "Master",
        3 => "Journey",
        _ => "UNKNOWN"
    };

    public static int ParseTime(int hour, int minute)
    {
        int time = (hour * 3600 + minute * 60 - 16200) % 86400;

        return time > 0 ? time : time + 86400;
    }

    public static void SetTime(int hour, int minute)
    {
        int time = ParseTime(hour, minute);
        bool night = time >= 54000; // 4:30 + 15 hours (19:30, 7:30 pm)

        bool b = true;
        
        if (night)
        {
            Main.UpdateTime_StartNight(ref b);
            Main.time = time - 54000;
        }
        else
        {
            Main.UpdateTime_StartDay(ref b);
            Main.time = time;
        }
        
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.WorldData);
    }

    public static (int hour, int minute) GetTime()
    {
        double time = Main.time;

        int hour = (int) Math.Round(time / 3600) + 4;
        int minute = (int) (time / 60 + 30) % 60;

        if (!Main.dayTime) hour += 15;
        hour %= 24;

        return (hour, minute);
    }

    public static string[] GetZones(Player player)
    {
        return ZoneProps
            .Where(prop => (bool) prop.GetValue(player)!)
            .Select(prop => Beautify(prop.Name[4..]))
            .ToArray();
    }

    public static void Teleport(Player player, (int x, int y) pos)
    {
        (int x, int y) = pos;
        player.Teleport(new Vector2(x, y - player.height), 2);
        player.velocity = Vector2.Zero;

        if (Main.netMode != NetmodeID.Server) return;
        RemoteClient.CheckSection(player.whoAmI, player.position);
        NetMessage.SendData(MessageID.Teleport, number2: player.whoAmI, number3: x, number4: y, number5: 2);
    }

    public static Color ColourFromRGBInt(int rgb) => new()
    {
        PackedValue = 0U,
        A = byte.MaxValue,
        R = (byte) ((rgb >> 16) & 0xff),
        G = (byte) ((rgb >> 8) & 0xff),
        B = (byte) ((rgb >> 0) & 0xff)
    };

    public static string Beautify(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        string result = "";

        int spacesAdded = 0;
        bool wasUpper = char.IsUpper(s[0]);
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (i > 0 && char.IsUpper(ch) && !wasUpper)
            {
                if (i == 1 || s[i - 1] != ' ')
                {
                    result += " " + ch;
                    spacesAdded++;
                }
                else result += ch;
                wasUpper = true;
            }
            else
            {
                result += ch;
                wasUpper = i > 0 ? char.IsUpper(ch) : wasUpper;
            }

            if (i <= 1 || char.IsUpper(s[i]) || !char.IsUpper(s[i - 1]) || !char.IsUpper(s[i - 2])) continue;
            result = result[..(i - 1 + spacesAdded)] + " " + result[(i - 1 + spacesAdded)..];
            spacesAdded++;
        }

        return result;
    }

    public static LocalizedText NewLocalizedText(string key, string value) => (LocalizedText)LocalizedTextCtor.Invoke(new object[] { key, value });

    public static Color ColorFromHSV(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        return hi switch
        {
            0 => new Color(v, t, p),
            1 => new Color(q, v, p),
            2 => new Color(p, v, t),
            3 => new Color(p, q, v),
            4 => new Color(t, p, v),
            _ => new Color(v, p, q)
        };
    }

    public static RemoteAddress GetAddress(int playerId) => Netplay.Clients[playerId].Socket.GetRemoteAddress();

    public static void ForcePlaceTile(int x, int y, int type, int style = 0, int alternate = 0, int direction = 0)
    {
        bool customPlace = TileObjectData.CustomPlace(type, style) && type != TileID.ImmatureHerbs && type != TileID.DyePlants;
        TileObjectData tileData = TileObjectData.GetTileData(type, style, alternate);

        for (int tileX = 0; tileX < tileData.Width; tileX++)
        for (int tileY = 0; tileY < tileData.Height; tileY++)
            if (!WorldGen.TileEmpty(x, y)) WorldGen.KillTile(x, y, noItem: true);
        
        int rand = -1;
        
        if (type == TileID.Saplings)
        {
            Tile tile = Main.tile[x, y];
            if (tile.IsActive)
                TileLoader.SaplingGrowthType(tile.type, ref type, ref style);
        }

        if (tileData.RandomStyleRange > 0)
        {
            TileObjectPreviewData.randomCache ??= new TileObjectPreviewData();
            bool doRand = false;
            if (TileObjectPreviewData.randomCache.Type == type)
            {
                Point16 coordinates = TileObjectPreviewData.randomCache.Coordinates;
                Point16 objectStart = TileObjectPreviewData.randomCache.ObjectStart;
                int objX = coordinates.X + objectStart.X;
                int objY = coordinates.Y + objectStart.Y;
                int tileX = x - tileData.Origin.X;
                int tileY = y - tileData.Origin.Y;
                if (objX != tileX || objY != tileY)
                    doRand = true;
            }
            else doRand = true;
            rand = !doRand ? TileObjectPreviewData.randomCache.Random : Main.rand.Next(tileData.RandomStyleRange);
        }

        TileObject tileObj = TileObject.Empty;
        tileObj.xCoord = x - tileData.Origin.X;
        tileObj.yCoord = y - tileData.Origin.Y;
        tileObj.type = type;
        tileObj.style = style;
        tileObj.alternate = alternate;
        tileObj.random = rand;

        bool placed;
        if (customPlace && false)
        {
            MoreCommands.Log.Debug("Tile has custom place");
            
            placed = TileObject.Place(tileObj);
            WorldGen.SquareTileFrame(x, y);
            if (Main.netMode != NetmodeID.Server || !TileID.Sets.IsAContainer[type])
                SoundEngine.PlaySound(SoundID.Dig, x, y);
        }
        else placed = WorldGen.PlaceTile(x, y, type, forced: true, style: style);
        
        MoreCommands.Log.Debug("Placed: " + placed);

        if (!placed) return;
        if (customPlace)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && !Main.tileContainer[type] && type != TileID.LogicSensor)
                NetMessage.SendObjectPlacment(-1, x, y, type, style, alternate, rand, direction);
        }
        else NetMessage.SendData(MessageID.TileManipulation, number: 1, number2: x, number3: y, number4: type, number5: style);

        Item item = Main.item.FirstOrDefault(item => item.createTile == type);
        if (item is not null) TileLoader.PlaceInWorld(x, y, item);
    }

    public static void SendCommand(string cmd)
    {
        if (!cmd.StartsWith("/")) cmd = "/" + cmd;
        
        if (Main.netMode == NetmodeID.SinglePlayer)
            CommandLoaderDetours.HandleCommand(cmd, new ClientPlayerCommandCaller());
        else ChatHelper.SendChatMessageFromClient(new ChatMessage(cmd));
    }

    public static void SendMsg(string msg)
    {
        if (msg.StartsWith("/")) SendCommand(msg);
        else
        {
            ChatMessage outgoingMessage = new(msg);
            switch (Main.netMode)
            {
                case NetmodeID.SinglePlayer:
                    ChatManager.Commands.ProcessIncomingMessage(outgoingMessage, Main.myPlayer);
                    break;
                case NetmodeID.MultiplayerClient:
                    ChatHelper.SendChatMessageFromClient(outgoingMessage);
                    break;
            }
        }
    }

    public static MethodInfo GetMethod(Type owner, string name, bool isStatic = true, bool isPublic = true, params Type[] parameterTypes)
    {
        BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);
        MethodInfo method = owner.GetMethod(name, flags, null,
            isStatic ? CallingConventions.Standard : CallingConventions.HasThis, parameterTypes, null);

        if (method is not null) return method;
        if (parameterTypes.Length != 0) throw new ArgumentException("Could not find a method with the given name and the given parameter types in the given type.");
        
        IEnumerable<MethodInfo> methods = owner.GetMethods(flags).Where(m => m.Name == name).ToArray();
        if (!methods.Any()) throw new ArgumentException("Could not find a method with the given name in the given type.");
        if (methods.Count() > 1) throw new ArgumentException("Found ambiguous results for the given name in the given type.");

        return methods.First();
    }

    public static Rectangle CreateRectangle(Vector2 start, Vector2 end)
    {
        int x = (int) Math.Min(start.X, end.X);
        int y = (int) Math.Min(start.Y, end.Y);
        return new Rectangle(x, y, (int) (Math.Max(start.X, end.X) - x), (int) (Math.Max(start.Y, end.Y) - y));
    }

    public static Color GetRandomBrightColour(int? seed)
    {
        Random random = seed is null ? Random : new Random(seed.Value);
        return ColorFromHSV(random.Next(0, 256), random.NextDouble() * .5 + .25, .9);
    }
}