using System;
using Microsoft.Xna.Framework;
using MoreCommands.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoreCommands.Misc.Styling;

public class StyledTextBuilder
{
    private readonly string _text;
    private Color _colour = Color.White;
    private ClickAction _action = ClickAction.Empty;
    private string _hoverText;
    private bool _underline;

    private StyledTextBuilder(string text) => _text = text;

    public static StyledTextBuilder Builder<T>(T text) => new(text?.ToString() ?? throw new ArgumentNullException(nameof(text)));

    public StyledTextBuilder WithColour(Color colour)
    {
        _colour = colour;
        return this;
    }

    public StyledTextBuilder WithColour(int rgb)
    {
        _colour = Util.ColourFromRGBInt(rgb);
        return this;
    }
    
    public StyledTextBuilder WithClick(ClickAction action)
    {
        _action = action;
        return this;
    }

    public StyledTextBuilder WithHover(string hoverText)
    {
        _hoverText = hoverText;
        return this;
    }
    
    public StyledTextBuilder WithUnderline() => WithUnderline(true);

    public StyledTextBuilder WithUnderline(bool underline)
    {
        _underline = underline;
        return this;
    }

    public StyledTextSnippet BuildSnippet() => new(_text, _action, _hoverText, _underline)
    {
        Color = _colour
    };

    public string BuildString()
    {
        JObject data = new()
        {
            ["text"] = _text,
            ["click"] = new JObject
            {
                ["action"] = Enum.GetName(_action.Action_),
                ["value"] = _action.Value
            },
            ["hover"] = _hoverText,
            ["colour"] = $"#{_colour.R:X2}{_colour.G:X2}{_colour.B:X2}",
            ["underline"] = _underline
        };

        return $"[styled:{data.ToString(Formatting.None)}]";
    }

    public override string ToString() => BuildString();
}