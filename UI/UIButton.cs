using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MoreCommands.UI;

public class UIButton : UIPanel
{
    public UIButton(string text, Action<UIMouseEvent, UIElement> onClick)
    {
        Width.Set(FontAssets.MouseText.Value.MeasureString(text).X + 30, 0);
        Height.Set(30, 0);
        OnClick += (evt, element) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            onClick?.Invoke(evt, element);
        };
        OnUpdate += _ => BorderColor = IsMouseHovering ? Color.Yellow : Color.Black;
        
        UIText textElement = new(text)
        {
            HAlign = .5f
        };
        textElement.Top.Pixels = -4;
        Append(textElement);
    }
}