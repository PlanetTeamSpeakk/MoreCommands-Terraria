using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoreCommands.Utils;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MoreCommands.UI;

public class TitleUI : UIState
{
    public static Title ActiveTitle { get; private set; }
    private static readonly DynamicSpriteFont TitleFont = FontAssets.DeathText.Value, SubtitleFont = FontAssets.MouseText.Value;
    private long _start;
    private uint _totalDuration;
    private List<TextSnippet> _titleSnippets, _subtitleSnippets;
    private float _titleWidth, _titleHeight, _subtitleWidth, _subtitleHeight;

    public override void OnActivate()
    {
        _start = DateTime.Now.Ticks / 10000;
        _totalDuration = ActiveTitle.FadeIn + ActiveTitle.Duration + ActiveTitle.FadeOut;
        
        _titleSnippets = ChatManager.ParseMessage(ActiveTitle.Value, Color.White);
        _titleWidth = _titleSnippets.Select(snippet => TitleFont.MeasureString(snippet.Text).X).Sum();
        _titleHeight = _titleSnippets.Max(snippet => TitleFont.MeasureString(snippet.Text).Y);

        _subtitleSnippets = string.IsNullOrEmpty(ActiveTitle.Subtitle) ? null : ChatManager.ParseMessage(ActiveTitle.Subtitle, Color.White);
        _subtitleWidth = _subtitleSnippets?.Select(snippet => SubtitleFont.MeasureString(snippet.Text).X).Sum() ?? 0f;
        _subtitleHeight = _subtitleSnippets?.Max(snippet => SubtitleFont.MeasureString(snippet.Text).Y) ?? 0f;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (ActiveTitle is null) return;
        
        long progress = DateTime.Now.Ticks / 10000 - _start;
        float fadeMultiplier = progress >= ActiveTitle.FadeIn ?
            progress >= ActiveTitle.FadeIn + ActiveTitle.Duration ?
                (float) (ActiveTitle.FadeOut - (progress - ActiveTitle.FadeIn - ActiveTitle.Duration)) / ActiveTitle.FadeOut : // Currently in 'fade-out' timeframe 
                1f : // Currently in 'duration' timeframe
            1f - (float) (ActiveTitle.FadeIn - progress) / ActiveTitle.FadeIn; // Currently in 'fade-in' timeframe
        float shadowFadeMultiplier = fadeMultiplier is 1f ? 1 : (float) Math.Pow(fadeMultiplier, 3);

        TextSnippet[] titleSnippets = _titleSnippets.Select(snippet => Util.Make(snippet.CopyMorph(snippet.Text), snippet0 => snippet0.Color *= fadeMultiplier)).ToArray();
        Vector2 titlePos = new(Main.screenWidth / 2f - _titleWidth / 2, Main.screenHeight / 2f - _titleHeight / (_subtitleSnippets == null ? 2 : 1));
        
        ChatManager.DrawColorCodedStringShadow(spriteBatch, TitleFont, titleSnippets, titlePos, Color.Black * shadowFadeMultiplier, 0, Vector2.Zero, Vector2.One);
        ChatManager.DrawColorCodedString(spriteBatch, TitleFont, titleSnippets, titlePos, Color.White, 0, Vector2.Zero, Vector2.One, out int _, -1);

        if (_subtitleSnippets != null)
        {
            TextSnippet[] subtitleSnippets = _subtitleSnippets.Select(snippet => Util.Make(snippet.CopyMorph(snippet.Text), snippet0 => snippet0.Color *= fadeMultiplier)).ToArray();
            Vector2 subtitlePos = new(Main.screenWidth / 2f - _subtitleWidth / 2, Main.screenHeight / 2f + _subtitleHeight / 2);
            
            ChatManager.DrawColorCodedStringShadow(spriteBatch, SubtitleFont, subtitleSnippets, subtitlePos, Color.Black * shadowFadeMultiplier, 0, Vector2.Zero, Vector2.One);
            ChatManager.DrawColorCodedString(spriteBatch, SubtitleFont, subtitleSnippets, subtitlePos, Color.White, 0, Vector2.Zero, Vector2.One, out int _, -1);
        }

        if (progress >= _totalDuration)
            MoreCommands.TitleInterface.SetState(null);
    }

    public static void ShowTitle(string title, string subtitle = null, uint duration = 10000, uint fadeIn = 250, uint fadeOut = 250) =>
        ShowTitle(new Title(title, subtitle, duration, fadeIn, fadeOut));

    public static void ShowTitle(Title title)
    {
        ActiveTitle = title;
        MoreCommands.TitleInterface.SetState(MoreCommands.TitleUI);
    }

    public record Title(string Value, string Subtitle = null, uint Duration = 10000, uint FadeIn = 250, uint FadeOut = 250);
}