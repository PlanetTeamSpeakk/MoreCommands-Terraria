using Microsoft.Xna.Framework;
using MoreCommands.Misc.Styling;
using Terraria;
using Terraria.UI.Chat;

namespace MoreCommands.Misc.TagHandlers;

public class HoverTagHandler : ITagHandler
{
    public TextSnippet Parse(string text, Color baseColor = new(), string options = null) => 
        new StyledTextSnippet(text, ClickAction.Empty, options ?? "", false);
}