using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Context;
using Brigadier.NET.Tree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreCommands.Extensions;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace MoreCommands.IL.Manipulations;

// Applies colours and possible arguments to the chat when typing MoreCommands commands.
public class ChatCommandSyntaxManipulation : ILManipulation
{
    private static readonly Func<CommandDispatcher<CommandSource>, CommandNode<CommandSource>, StringReader, CommandContextBuilder<CommandSource>, ParseResults<CommandSource>> ParseNodes = 
        Dynamics.CreateInvoker<CommandDispatcher<CommandSource>, Func<CommandDispatcher<CommandSource>, CommandNode<CommandSource>, StringReader, CommandContextBuilder<CommandSource>, 
            ParseResults<CommandSource>>>("ParseNodes");
    public override MethodBase Target => Util.GetMethod(typeof(Main), "DrawPlayerChat", false, false);
    public override IEnumerable<ILMove> Movements => new[]
    {
        Move(inst => inst.MatchLdstr("|")),
        Move(inst => inst.MatchLdloc(3)) 
    };
    
    public override void Inject(ILCursor c)
    {
        // At this point we are at the instruction the if-statement jumps to if textBlinkerState != 1.
        // This instruction is ldloc.3 which loads the textsnippets list (local variable 3), so the list is already loaded.
        // This does mean we have to load the list again once we're finished.

        string lastContent = null;
        string usage = null;
        IDictionary<CommandNode<CommandSource>, Color> colourCache = new Dictionary<CommandNode<CommandSource>, Color>();
        ParseResults<CommandSource> results;
        IList<TextSnippet> lastSnippets = new List<TextSnippet>();

        // List is already loaded, add snippets with a delegate.
        c.EmitDelegate(delegate (List<TextSnippet> snippets)
        {
            // Honestly, at this point, idek how it works anymore.
            // It was easy enough to understand before I fixed redirects not being parsed (which took me 8 hours, including head-scratching).
            
            // I went with this method rather than how Mojang does it in Minecraft (using the parsed arguments gotten from CommandContext), because
            // 1. I hate myself and like to spend way too much time on stupid stuff.
            // 2. I didn't know that was a thing. (I found out when I was almost done with this, at the point where it only did not yet work for nested redirects)
            // 3. I prefer every argument having its own RGB colour that's always the same, rather than basing its colour off of its index in the command.
            
            if (!Main.chatText.StartsWith("/") || !Main.drawingPlayerChat) return;

            if (Main.chatText != lastContent)
            {
                lastContent = Main.chatText;
                CommandSource source = new(new ClientPlayerCommandCaller());
                List<ParsedCommandNode<CommandSource>> nodes = new();
                ParsedCommandNode<CommandSource> last = null;
                lastSnippets = new List<TextSnippet>();
                snippets.Clear();
                snippets.Add(new TextSnippet("/", Color.LightGray));
                int offset = 0, deltaOffset = 0, spaceOffset = 0;
                StringRange? lastRange = null;
                int redirects = 0;

                do
                {
                    string text = Main.chatText[(offset + 1)..];
                    int lenOld = text.Length;
                    text = text.TrimStart();
                    int prevOffset = offset;

                    results = ParseNodes(MoreCommands.Dispatcher, last?.Node.Redirect ?? MoreCommands.Dispatcher.GetRoot(), new StringReader(text), new CommandContextBuilder<CommandSource>(
                        MoreCommands.Dispatcher, source, last?.Node.Redirect ?? MoreCommands.Dispatcher.GetRoot(), 0));

                    if (last is null && results.Context.Nodes.Count == 0)
                    {
                        usage = null;
                        break;
                    }

                    bool redirect = last?.Node.Redirect is not null;

                    nodes = new List<ParsedCommandNode<CommandSource>>(results.Context.Nodes);
                    nodes.Sort((node1, node2) => node1.Range.Start.CompareTo(node2.Range.Start));
                    last = nodes.LastOrDefault() ?? new ParsedCommandNode<CommandSource>(last?.Node.Redirect ?? MoreCommands.Dispatcher.GetRoot(), StringRange.Between(offset, offset));
                    bool ignoreRangeForOffset = last.Range.Start == last.Range.End;

                    for (int i = 0; i < results!.Context.Nodes.Count; i++)
                    {
                        ParsedCommandNode<CommandSource> node = results!.Context.Nodes[i];

                        if (lastRange is not null && lastRange.Value.End != node.Range.Start)
                        {
                            bool nested = ignoreRangeForOffset || lastRange.Value.End == deltaOffset - spaceOffset;

                            int prevEnd = offset;
                            switch (nested)
                            {
                                case true:
                                {
                                    for (; prevEnd < Main.chatText.Length - 1; prevEnd++)
                                        if (Main.chatText[prevEnd + 1] != ' ')
                                            break;
                                    break;
                                }
                                case false:
                                {
                                    int from = lastRange.Value.End + (redirect && i == 0 ? offset : 0);
                                    int to = node.Range.Start;

                                    if (to > from)
                                        snippets.Add(new TextSnippet(StringRange.Between(from, to).Get(redirect && i == 0 ? Main.chatText[1..] : text)));
                                    break;
                                }
                            }

                        }

                        lastRange = node.Range;
                        int iRO = i;
                        ParseResults<CommandSource> resultsRO = results;

                        snippets.Add(new TextSnippet(node.Range.Get(text), node.Node is LiteralCommandNode<CommandSource> ? Color.LightGray :
                            colourCache.ComputeIfAbsent(node.Node, _ =>
                            {
                                StringBuilder path = new();
                                for (int x = 0; x <= iRO; x++)
                                    path.Append(resultsRO!.Context.Nodes[x].Node.Name).Append(' ');

                                // For a consistent colour even after a reload.
                                int bi = 0;
                                return Util.GetRandomBrightColour(path.ToString().TrimEnd().ToByteArray().Sum(b => b * ++bi));
                            })));

                        if (i != results!.Context.Nodes.Count - 1 || node.Node.Redirect is null || text.Length <= node.Range.End) continue;

                        int end = node.Range.End;
                        for (; end < text.Length; end++)
                            if (text[end] != ' ')
                                break;

                        snippets.Add(new TextSnippet(text[node.Range.End..end]));
                    }

                    int spaceDiff = lenOld - text.Length;
                    offset += spaceDiff;
                    spaceOffset += spaceDiff;
                    if (lastRange is not null && lastRange.Value.Start != lastRange.Value.End && !ignoreRangeForOffset)
                        offset += lastRange.Value.End;

                    deltaOffset = offset - prevOffset;
                    if (redirect) redirects++;
                } while (last.Node.Redirect is not null);

                if (offset < Main.chatText.Length - 1)
                {
                    // Add invalid arguments in red (or grey if legacy)
                    string text = Main.chatText[(offset + 1)..];
                    snippets.Add(new TextSnippet(text, redirects > 0 /* cannot redirect to legacy command */ || !MoreCommands.LegacyCommands.Contains(
                        text[..(text.Contains(' ') ? text.IndexOf(' ') : text.Length)]) ? Color.Red : Color.LightGray));
                }

                lastSnippets = snippets.ToImmutableList();
                usage = string.Join(" OR ", nodes
                    .Where(node => node.Range.Start == last?.Range.Start)
                    .SelectMany(node => MoreCommands.Dispatcher.GetSmartUsage(node.Node, source).Values));
            }
            
            snippets.Clear();
            snippets.AddRange(lastSnippets);

            if (usage is null) return;
            snippets.Add(Main.instance.textBlinkerState == 1 ? new TextSnippet("|") : new InvisibleTextSnippet("|")); // Adding invisible pipe when the visible pipe is not being drawn.
            snippets.Add(new TextSnippet(usage, Color.Gray));
        });

        c.Emit(OpCodes.Ldloc_3); // Load list again
    }
    
    // Old method that does not work with redirects.
    // Left in for clarity as the above is a whole mess.
    
    // ReSharper disable once UnusedMember.Local
    private void InjectOld(ILCursor c)
    {
        // At this point we are at the instruction the if-statement jumps to if textBlinkerState != 1.
        // This instruction is ldloc.3 which loads the textsnippets list (local variable 3), so the list is already loaded.
        // This does mean we have to load the list again once we're finished.

        string lastContent = null;
        string suggestion = null;
        IDictionary<CommandNode<CommandSource>, Color> colourCache = new Dictionary<CommandNode<CommandSource>, Color>();
        ParseResults<CommandSource> results;
        IList<TextSnippet> lastSnippets = new List<TextSnippet>();

        // List is already loaded, add snippets with a delegate.
        c.EmitDelegate(delegate (List<TextSnippet> snippets)
        {
            if (!Main.chatText.StartsWith("/") || !Main.drawingPlayerChat) return;

            if (Main.chatText != lastContent)
            {
                CommandSource source = new(new ClientPlayerCommandCaller());
                results = MoreCommands.Dispatcher.Parse(Main.chatText[1..], source);

                if (results.Context.Nodes.Count == 0)
                {
                    suggestion = null;
                    return;
                }

                List<ParsedCommandNode<CommandSource>> nodes = new(results.Context.Nodes);
                nodes.Sort((node1, node2) => node1.Range.Start.CompareTo(node2.Range.Start));
                ParsedCommandNode<CommandSource> last = nodes.Last();

                suggestion = string.Join(" OR ", nodes
                    .Where(node => node.Range.Start == last.Range.Start)
                    .SelectMany(node => MoreCommands.Dispatcher.GetSmartUsage(node.Node, source).Values));
                lastContent = Main.chatText;

                string text = Main.chatText[1..];
                snippets.Clear();
                snippets.Add(new TextSnippet("/", Color.LightGray));

                StringRange? lastRange = null;
                for (int i = 0; i < results!.Context.Nodes.Count; i++)
                {
                    ParsedCommandNode<CommandSource> node = results!.Context.Nodes[i];
                    
                    if (lastRange != null && lastRange.Value.End != node.Range.Start)
                        snippets.Add(new TextSnippet(StringRange.Between(lastRange.Value.End, node.Range.Start).Get(text)));
                    lastRange = node.Range;

                    int iRO = i;
                    snippets.Add(new TextSnippet(node.Range.Get(text), node.Node is LiteralCommandNode<CommandSource> ? Color.LightGray :
                        colourCache.ComputeIfAbsent(node.Node, _ =>
                        {
                            StringBuilder path = new();
                            for (int x = 0; x <= iRO; x++)
                                path.Append(results!.Context.Nodes[x].Node.Name).Append(' ');
                            
                            // For a consistent colour even after a reload.
                            int bi = 0;
                            return Util.GetRandomBrightColour(path.ToString().TrimEnd().ToByteArray().Sum(b => b * ++bi));
                        })));
                }

                if (lastRange != null && lastRange.Value.End != text.Length)
                    snippets.Add(new TextSnippet(text[lastRange.Value.End..], Color.Red));

                lastSnippets = snippets.ToImmutableList();
            }
            
            snippets.Clear();
            snippets.AddRange(lastSnippets);

            if (suggestion is null) return;
            snippets.Add(Main.instance.textBlinkerState == 1 ? new TextSnippet("|") : new InvisibleTextSnippet("|")); // Adding invisible pipe when the visible pipe is not being drawn.
            snippets.Add(new TextSnippet(suggestion, Color.Gray));
        });

        c.Emit(OpCodes.Ldloc_3); // Load list again
    }
    
    private class InvisibleTextSnippet : TextSnippet
    {
        internal InvisibleTextSnippet(string text, float scale = 1f) : base(text, Color.Transparent, scale) {}

        public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = new(), Color color = new(), float scale = 1)
        {
            size = FontAssets.MouseText.Value.MeasureString(Text) * scale;
            return true; // Preventing Terraria's draw method from drawing this while still affecting the drawing position.
        }
    }
}