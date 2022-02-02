using System;
using Microsoft.Xna.Framework;
using MoreCommands.Misc.Styling;
using Terraria.UI.Chat;

namespace MoreCommands.Misc.TagHandlers;

public class ClickTagHandler : ITagHandler
{
    public TextSnippet Parse(string text, Color baseColor = new(), string options = null)
    {
        if (options is null) return new TextSnippet(text, baseColor);
        string[] args = options.Split(";", 2);
        if (args.Length != 2 || args[0].ToLower() == "none") return new TextSnippet(text, baseColor);

        bool valid = Enum.TryParse(args[0], true, out ClickAction.Action action);
        return !valid ? new TextSnippet(text, baseColor) : new StyledTextSnippet(text, new ClickAction(action, args[1]), null, false);
    }
}