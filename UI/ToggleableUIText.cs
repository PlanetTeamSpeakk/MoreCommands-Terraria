using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MoreCommands.UI;

public class ToggleableUIText : UIText
{
    public bool Visible = true;

    public ToggleableUIText(string text, float textScale = 1, bool large = false) : base(text, textScale, large) {}

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (Visible) base.DrawSelf(spriteBatch);
    }
}