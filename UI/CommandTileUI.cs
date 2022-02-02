using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MoreCommands.Tiles;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace MoreCommands.UI;

public class CommandTileUI : UIState
{
    private bool _writing;
    private const float LeftOffset = .05f;
    private UIPanel _panel;
    private string _command;
    private string _lastOutput;
    private UITextBox _commandText;
    private UITextBox _lastOutputText;
    private int _boxSelected = 0;
    private int _commandOffset;
    private int _lastOutputOffset;
    
    public override void OnInitialize()
    {
        AppendPanel();
        AppendTexts();
        AppendTextBoxes();
        AppendButtons();
        SetEvents();
        Recalculate();
    }

    // UI INITIALISATION METHODS
    
    private void AppendPanel()
    {
        _panel = new UIPanel();
        _panel.Width.Set(600, 0);
        _panel.Height.Set(300, 0);
        _panel.HAlign = _panel.VAlign = .5f;
        Recalculate();
        Append(_panel);
    }

    private void AppendTexts()
    {
        UIText title = new("Command Tile", .75f, true)
        {
            HAlign = LeftOffset
        };
        title.Top.Pixels = 10;
        _panel.Append(title);

        UIText command = new("Command")
        {
            HAlign = LeftOffset
        };
        command.Top.Pixels = 50;
        _panel.Append(command);

        UIText lastOutput = new("Last output")
        {
            HAlign = LeftOffset
        };
        lastOutput.Top.Pixels = 130;
        _panel.Append(lastOutput);
    }

    private void AppendTextBoxes()
    {
        _commandText = new UITextBox("")
        {
            HAlign = LeftOffset,
            TextHAlign = 0f
        };
        _commandText.Top.Pixels = 75;
        _commandText.Width.Pixels = 600 - LeftOffset;
        _commandText.SetTextMaxLength(9999);
        _panel.Append(_commandText);
        
        _lastOutputText = new UITextBox("")
        {
            HAlign = LeftOffset,
            ShowInputTicker = false,
            TextHAlign = 0f
        };
        _lastOutputText.Top.Pixels = 155;
        _lastOutputText.Width.Pixels = 600 - LeftOffset;
        _lastOutputText.SetTextMaxLength(70);
        _panel.Append(_lastOutputText);
    }

    private void AppendButtons()
    {
        UIButton closeButton = new("Close", (_, _) => Close(false))
        {
            HAlign = .6f
        };
        closeButton.Top.Set(_panel.Height.GetValue(0) - closeButton.Height.GetValue(0) - 30, 0);
        _panel.Append(closeButton);
        
        UIButton saveButton = new("Save", (_, _) => Close(true))
        {
            Top = closeButton.Top,
            HAlign = .4f
        };
        _panel.Append(saveButton);
    }

    private void SetEvents()
    {
        OnUpdate += _ =>
        {
            if (ContainsPoint(Main.MouseScreen))
                Main.LocalPlayer.mouseInterface = true;
            
            if (Main.keyState[Keys.Up] == KeyState.Down && _boxSelected > 0)
                UpdateSelectedBox(_boxSelected - 1);
            if (Main.keyState[Keys.Down] == KeyState.Down && _boxSelected < 1)
                UpdateSelectedBox(_boxSelected + 1);
            
            if (Main.keyState[Keys.Left] == KeyState.Down || Main.keyState[Keys.Right] == KeyState.Down)
                UpdateOffset(Main.keyState[Keys.Left] == KeyState.Down ? -1 : 1);
        };
        OnClick += (evt, _) =>
        {
            if (!_panel.ContainsPoint(evt.MousePosition))
                Close(false);
        };
    }

    // TEXT WRITING METHODS
    
    private void UpdateOffset(int delta)
    {
        switch (_boxSelected)
        {
            case 0:
                UpdateOffset(delta, ref _commandOffset, ref _command, ref _commandText);
                break;
            case 1:
                UpdateOffset(delta, ref _lastOutputOffset, ref _lastOutput, ref _lastOutputText);
                break;
        }
    }

    private static void UpdateOffset(int delta, ref int offset, ref string text, ref UITextBox box)
    {
        int prev = offset;
        offset += delta;
        offset = Math.Max(Math.Min(offset, text.Length - 70), 0);

        if (offset == prev) return;
        box.SetText(text[offset..(offset + 70)]);
    }

    private void UpdateSelectedBox(int boxSelected)
    {
        _boxSelected = boxSelected;

        foreach (UITextBox box in _panel.Children.OfType<UITextBox>())
            box.BorderColor = Color.Black;
        
        (_boxSelected switch
        {
            0 => _commandText,
            1 => _lastOutputText,
            _ => throw new IndexOutOfRangeException("Selected box was out of bounds.")
        }).BorderColor = Color.Yellow;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (!_writing || _boxSelected != 0) return;
        
        // Handling getting input
        PlayerInput.WritingText = true;
        Main.instance.HandleIME();
        
        // I believe this is somehow required to get the input, idk, UISearchBar uses it.
        Vector2 position = new(Main.screenWidth / 2f, _commandText.GetDimensions().ToRectangle().Bottom + 32);
        Main.instance.DrawWindowsIMEPanel(position, 0.5f);

        // Checks whether the cursor is currently at the end of the text.
        bool atEnd = _command.Length <= 70 || _commandOffset + _commandText.Text.Length == _command.Length;

        // Gets the text that was displayed in the command textbox before the modification was made.
        string textBefore = atEnd ? _command : _command[..(_commandOffset + _commandText.Text.Length)];
        string textAfter = Main.GetInputText(textBefore);

        _command = textAfter + (!atEnd ? _command[(_commandOffset + _commandText.Text.Length)..] : "");
        _commandOffset += Math.Max(textAfter.Length - textBefore.Length, 0);
        if (_command.Length - _commandOffset < 70) _commandOffset = Math.Max(_command.Length - 70, 0);

        _commandText.SetText(_command[_commandOffset..Math.Min(_command.Length, _commandOffset + 70)]);

        if (Main.inputTextEscape || Main.inputTextEnter)
        {
            Close(Main.inputTextEnter);
            return;
        }

        position = new Vector2(Main.screenWidth / 2f, _commandText.GetDimensions().ToRectangle().Bottom + 32);
        Main.instance.DrawWindowsIMEPanel(position, 0.5f);
    }

    // MISC METHODS
    
    private void Close(bool save)
    {
        if (save)
        {
            CommandTileEntity.CurrentlyEditing.Command = _command;
            Main.NewText(_command.Length == 0 ? "Command has been cleared." : $"Command has been set to {_command}", MoreCommands.DefColour);
        }
        CommandTileEntity.CloseEditUI();
        
        Main.clrInput();
        Main.inputTextEnter = Main.inputTextEscape = false;
    }

    public override void OnActivate()
    {
        _command = CommandTileEntity.CurrentlyEditing.Command;
        _commandText.SetText(_command.Length > 70 ? _command[..70] : _command);
        
        _lastOutput = CommandTileEntity.CurrentlyEditing.LastOutput;
        _lastOutputText.SetText(_lastOutput.Length > 70 ? _lastOutput[..70] : _lastOutput);
        
        _writing = true;
        Main.CurrentInputTextTakerOverride = this;
        
        UpdateSelectedBox(_boxSelected);
    }

    public override void OnDeactivate()
    {
        _writing = false;
        Main.CurrentInputTextTakerOverride = null;
    }
}