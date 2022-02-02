using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.Config;

namespace MoreCommands.Misc;

[Label("Generic Client Configuration")]
public class ClientConfig : ModConfig
{
    public static ClientConfig Instance;
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Label("Default Colour")]
    [Tooltip("The default colour to use for messages sent by MoreCommands. In hex format")]
    [DefaultValue("ffaa00")]
    public string DefColour = "ffaa00";
    
    [Label("Secondary Colour")]
    [Tooltip("The secondary colour to use for messages sent by MoreCommands. In hex format")]
    [DefaultValue("ffff00")]
    public string SecColour = "ffff00";

    [Tooltip("Whether cheats are enabled in singleplayer worlds.")]
    [DefaultValue(false)]
    public bool EnableCheats = false;

    public override void OnChanged()
    {
        if (DefColour.Length == 6 && DefColour.All(ch => char.IsDigit(ch) || char.ToLower(ch) >= 'a' && char.ToLower(ch) <= 'f'))
            MoreCommands.DefColour = new Color(
                int.Parse(DefColour[..2], NumberStyles.HexNumber),
                int.Parse(DefColour[2..4], NumberStyles.HexNumber),
                int.Parse(DefColour[4..], NumberStyles.HexNumber)
            );
        
        if (SecColour.Length == 6 && SecColour.All(ch => char.IsDigit(ch) || char.ToLower(ch) >= 'a' && char.ToLower(ch) <= 'f'))
            MoreCommands.SecColour = new Color(
                int.Parse(SecColour[..2], NumberStyles.HexNumber),
                int.Parse(SecColour[2..4], NumberStyles.HexNumber),
                int.Parse(SecColour[4..], NumberStyles.HexNumber)
            );
    }
}