using System.Collections.Generic;
using System.Reflection;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using Microsoft.Xna.Framework.Input;
using MonoMod.Cil;
using MoreCommands.Hooks;
using MoreCommands.UI;
using MoreCommands.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.ModLoader;

namespace MoreCommands.IL.Manipulations;

// Adds a suggestions UI to the chat when typing MoreCommands commands.
public class ChatCommandSuggestionsManipulation : ILManipulation
{
    public static SuggestionsUI UI { get; private set; }
    public override MethodBase Target => Util.GetMethod(typeof(Main), "DrawPlayerChat", false, false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchCallvirt(typeof(IChatMonitor).GetMethod("DrawChat")))
    };
    
    public override void Inject(ILCursor c)
    {
        string lastContent = null;
        uint suggestionsRequestId = 0U;
        bool wasTabDown = false;
        c.EmitDelegate(delegate ()
        {
            if (!Main.chatText.StartsWith("/") || !Main.drawingPlayerChat)
            {
                MoreCommands.SuggestionsInterface.SetState(UI = null);
                return;
            }
            
            if (Main.chatText == lastContent)
            {
                if (MoreCommands.RequestedSuggestions.requestId == suggestionsRequestId && MoreCommands.RequestedSuggestions.suggestions is not null &&
                    MoreCommands.RequestedSuggestions.suggestions.List.Count > 0 && MoreCommands.SuggestionsInterface.CurrentState is null)
                {
                    Suggestions suggestions = MoreCommands.RequestedSuggestions.suggestions;
                    float x = FontAssets.MouseText.Value.MeasureString(Main.chatText[..(suggestions.Range.Start + 1)]).X + 78f;
                    MoreCommands.SuggestionsInterface.SetState(UI = new SuggestionsUI(suggestions, x));
                }
            }
            else
            {
                lastContent = Main.chatText;
                MoreCommands.RequestSuggestions(++suggestionsRequestId);
                MoreCommands.SuggestionsInterface.SetState(UI = null);
            }

            UI?.UpdateIndex();
            if (Main.keyState[Keys.Tab] == KeyState.Down)
            {
                if (UI is not null && !wasTabDown)
                {
                    string text = Main.chatText;
                    StringRange range = UI.SelectedSuggestion.Range;
                    string suggestionText = UI.SelectedSuggestion.Text;

                    Main.chatText = text[..(range.Start + 1)] + suggestionText + (range.End < text.Length - 1 ? text[(range.End + 1)..] : "");
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }

                wasTabDown = true;
            }
            else wasTabDown = false;

            if (MoreCommands.SuggestionsInterface.CurrentState is not null && ModContent.GetInstance<SystemHooks>().LastGameTime is not null)
                MoreCommands.SuggestionsInterface.Draw(Main.spriteBatch, ModContent.GetInstance<SystemHooks>().LastGameTime);
        });
    }
}