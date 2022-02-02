using Terraria;
using Terraria.UI.Chat;

namespace MoreCommands.Misc.Styling;

public class StyledTextSnippet : TextSnippet
{
    public bool IsUnderline { get; }
    private readonly ClickAction _clickAction;
    private readonly string _hoverText;

    public StyledTextSnippet(string text, ClickAction clickAction, string hoverText, bool underline) : base(text)
    {
        _clickAction = clickAction;
        _hoverText = hoverText;
        IsUnderline = underline;
    }

    public override void OnClick() => _clickAction?.OnClick();

    public override void OnHover()
    {
        if (!string.IsNullOrEmpty(_hoverText)) Main.instance.MouseText(_hoverText);
    }

    public override TextSnippet CopyMorph(string newText)
    {
        StyledTextSnippet clone = (StyledTextSnippet) MemberwiseClone();
        clone.Text = newText;
        return this;
    }
}