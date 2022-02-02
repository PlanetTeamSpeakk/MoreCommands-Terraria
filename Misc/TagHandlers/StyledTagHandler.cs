using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Design;
using MoreCommands.Misc.Styling;
using MoreCommands.Utils;
using Newtonsoft.Json.Linq;
using Terraria.UI.Chat;

namespace MoreCommands.Misc.TagHandlers;

public class StyledTagHandler : ITagHandler
{
    public TextSnippet Parse(string text, Color baseColor = new(), string options = null)
    {
        JObject data;
        try
        {
            data = JObject.Parse(text);
        }
        catch
        {
            return new TextSnippet(text, baseColor);
        }
        
        if (!data.ContainsKey("text") || data["text"]!.ToString().Length == 0)
            return new TextSnippet(text, baseColor);

        JObject click = (JObject) data["click"];
        ClickAction.Action action = ClickAction.Action.None;
        string value = null;
        
        if (click is not null && click.ContainsKey("action") && click.ContainsKey("value") &&
            Enum.TryParse(click["action"]?.ToString(), true, out action))
            value = click["value"]?.ToString();

        string hover = data["hover"]?.ToString();
        bool underline = false;
        if (data.ContainsKey("underline") && !bool.TryParse(data["underline"]!.ToString(), out underline))
            underline = false;
        
        StyledTextSnippet snippet = new(data["text"].ToString(), new ClickAction(action, value), hover, underline);
        if (!data.ContainsKey("color") && !data.ContainsKey("colour")) return snippet;
        
        string colour = data[data.ContainsKey("color") ? "color" : "colour"]?.ToString();
        if (colour is not null && int.TryParse(colour.StartsWith("#") ? colour[1..] : colour, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb))
            snippet.Color = Util.ColourFromRGBInt(rgb);

        return snippet;
    }
}