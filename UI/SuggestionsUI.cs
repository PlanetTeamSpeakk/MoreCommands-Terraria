using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Brigadier.NET.Suggestion;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoreCommands.Extensions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MoreCommands.UI;

public class SuggestionsUI : UIState
{
    public Suggestion SelectedSuggestion => _suggestions.List[_selectedIndex];
    private const int MaxSuggestions = 10;
    private readonly UIPanel _panel = new();
    private readonly Suggestions _suggestions;
    private readonly float _x;
    private readonly List<ToggleableUIText> _topLines = new(), _bottomLines = new();
    private long _lastKeyCheck = Stopwatch.GetTimestamp();
    private int _selectedIndex;
    private int _shownIndex;
    private readonly List<ToggleableUIText> _texts = new();
    private bool _downDown, _upDown;

    public SuggestionsUI(Suggestions suggestions, float x)
    {
        _suggestions = suggestions;
        _x = x;
    }

    private static string GetText(Suggestion suggestion) => suggestion.Tooltip?.String ?? suggestion.Text;
    
    private static float GetWidth(Suggestion suggestion) => FontAssets.MouseText.Value.MeasureString(GetText(suggestion)).X;

    public override void OnInitialize()
    {
        _panel.Left.Pixels = _x;
        
        List<Suggestion> sortedSuggestions = new(_suggestions.List);
        sortedSuggestions.Sort((first, second) => GetWidth(first).CompareTo(GetWidth(second)));
        
        _panel.Width.Pixels = GetWidth(sortedSuggestions.Last()) + 24;
        _panel.Height.Pixels = Math.Min(_suggestions.List.Count, MaxSuggestions) * 19 + 24 + (_suggestions.List.Count > MaxSuggestions ? 6 : 0);
        _panel.Top.Pixels = Main.screenHeight - 33 - _panel.Height.Pixels - 8;
        _panel.BackgroundColor.WithAlpha(.6f);
        _panel.BorderColor.WithAlpha(.6f);
        
        Append(_panel);

        for (int i = 0; i < _suggestions.List.Count; i++)
        {
            ToggleableUIText text = new(GetText(_suggestions.List[i]))
            {
                Top = new StyleDimension(i * 19 + (_suggestions.List.Count > MaxSuggestions ? 4 : 1.5f), 0),
                TextColor = Color.White * .9f,
                Visible = i < MaxSuggestions
            };
            _texts.Add(text);
            _panel.Append(text);
        }

        _topLines.Clear();
        _bottomLines.Clear();

        if (_suggestions.List.Count > MaxSuggestions)
        {
            _topLines.AddRange(AddDottedLine(_panel.Width.Pixels - 26, false, -10));
            _bottomLines.AddRange(AddDottedLine(_panel.Width.Pixels - 26, true, _panel.Height.Pixels - 30));
        }

        UpdateIndex(true); // Initialises index
    }

    private IEnumerable<ToggleableUIText> AddDottedLine(float totalWidth, bool visible, float topOffset)
    {
        float lineWidth = FontAssets.MouseText.Value.MeasureString("-").X;
        int lineCount = (int) (totalWidth / (lineWidth * 1.2));
        float gapWidth = (totalWidth - lineCount * lineWidth) / (lineCount - 1);

        IList<ToggleableUIText> lines = new List<ToggleableUIText>();
        for (int i = 0; i < lineCount; i++)
        {
            ToggleableUIText text = new("-")
            {
                Visible = visible,
                TextColor = Color.Gray,
                Left = new StyleDimension(i * (lineWidth + gapWidth), 0),
                Top = new StyleDimension(topOffset, 0)
            };
            
            lines.Add(text);
            _panel.Append(text);
        }

        return lines.ToImmutableList();
    }

    internal void UpdateIndex() => UpdateIndex(false);
    
    private void UpdateIndex(bool initialising)
    {
        try
        {
            if (!initialising && (_downDown && IsDown(Keys.Down) || _upDown && IsDown(Keys.Up)) && Stopwatch.GetTimestamp() - _lastKeyCheck < 1000000) return;

            _lastKeyCheck = Stopwatch.GetTimestamp();
            int prevIndex = _selectedIndex;

            if (IsDown(Keys.Down)) _selectedIndex++;
            if (IsDown(Keys.Up)) _selectedIndex--;

            if (prevIndex != _selectedIndex)
                SoundEngine.PlaySound(SoundID.MenuTick);

            if (_selectedIndex < 0) _selectedIndex += _suggestions.List.Count;
            _selectedIndex %= _suggestions.List.Count;

            _texts.ForEach(text => text.TextColor = Color.White * .9f);
            _texts[_selectedIndex].TextColor = Color.Yellow * .9f;

            int prevShownIndex = _shownIndex;
            if (_selectedIndex < _shownIndex) _shownIndex = _selectedIndex;
            if (_selectedIndex > _shownIndex + MaxSuggestions - 1) _shownIndex = _selectedIndex - MaxSuggestions + 1;

            if (_shownIndex == prevShownIndex) return;
            for (int i = 0; i < _texts.Count; i++)
            {
                ToggleableUIText text = _texts[i];
                if (i >= _shownIndex && i < _shownIndex + MaxSuggestions)
                {
                    text.Visible = true;
                    text.Top.Pixels = (i - _shownIndex) * 19 + (_suggestions.List.Count > MaxSuggestions ? 4 : 1.5f);
                }
                else text.Visible = false;
            }

            _topLines.ForEach(line => line.Visible = _shownIndex != 0);
            _bottomLines.ForEach(line => line.Visible = _shownIndex != _texts.Count - MaxSuggestions);
        }
        finally
        {
            // When clicking/spamming the arrow keys, always update the index.
            // When holding the arrow keys, only update every 100 ms.
            _downDown = IsDown(Keys.Down);
            _upDown = IsDown(Keys.Up);
        }
    }

    private static bool IsDown(Keys key) => Main.keyState[key] == KeyState.Down;
}