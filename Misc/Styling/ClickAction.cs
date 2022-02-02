using System;
using MoreCommands.Utils;
using ReLogic.OS;
using Terraria;

namespace MoreCommands.Misc.Styling;

public class ClickAction
{
    public static ClickAction Empty => new(Action.None, null);
    // ReSharper disable once InconsistentNaming    Conflicts with Action enum
    public Action Action_ { get; }
    public string Value { get; }

    public ClickAction(Action action, string value)
    {
        Action_ = action;
        Value = action == Action.None ? value : value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public static ClickAction RunCommand(string command) => new(Action.RunCommand, command ?? throw new ArgumentNullException(nameof(command)));
    
    public static ClickAction SuggestCommand(string command) => new(Action.SuggestCommand, command ?? throw new ArgumentNullException(nameof(command)));
    
    public static ClickAction Copy(string value) => new(Action.Copy, value ?? throw new ArgumentNullException(nameof(value)));
    
    public static ClickAction OpenUrl(string url) => new(Action.OpenUrl, url ?? throw new ArgumentNullException(nameof(url)));

    internal void OnClick()
    {
        switch (Action_)
        {
            case Action.RunCommand:
                Util.SendCommand(Value);
                break;
            case Action.SuggestCommand:
                Main.OpenPlayerChat();
                Main.chatText = Value.StartsWith("/") ? Value : "/" + Value;
                break;
            case Action.OpenUrl:
                Terraria.Utils.OpenToURL($"{(Value.StartsWith("http://") && !Value.StartsWith("https://") ? "https://" : "")}{Value}");
                break;
            case Action.Copy:
                Platform.Get<IClipboard>().Value = Value;
                break;
            case Action.None:
            default:
                return;
        }
    }
    
    public enum Action
    {
        None,
        RunCommand,
        SuggestCommand,
        Copy,
        OpenUrl
    }
}