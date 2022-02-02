using System;
using System.Collections.Generic;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MoreCommands.ArgumentTypes;
using MoreCommands.Extensions;
using MoreCommands.IL;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class BindCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Send messages or commands when pressing a key on your keyboard.";
    private static readonly Func<Main, bool> GetImeToggle = Dynamics.CreateGetter<Main, bool>("_imeToggle");
    private static readonly IDictionary<Keys, string> Bindings = new Dictionary<Keys, string>();
    private Keys[] _lastPressedKeys = Array.Empty<Keys>();
    private int _record;

    internal override void Init()
    {
	    if (!MoreCommands.ConfigExists("Bindings")) SaveData();
	    else Bindings.AddAll(MoreCommands.ReadJson<Dictionary<Keys, string>>("Bindings"));
    }

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("bind")
	        .Then(Literal("set")
		        .Then(Argument("key", KeyArgumentType.Key)
			        .Then(Argument("msg", Arguments.GreedyString())
				        .Executes(ctx =>
				        {
					        Keys key = ctx.GetArgument<Keys>("key");
							if (Bindings.ContainsKey(key)) Error(ctx, $"A binding has already been made with this key, if you want this key to send multiple messages, " +
							                                          $"consider using macros, otherwise consider removing the binding with {Coloured("/bind remove " + key)} first.");
							else {
								Bindings[key] = ctx.GetArgument<string>("msg");
								SaveData();
								
								if (!Bindings[key].StartsWith("/")) 
									Reply(ctx, $"The message does not start with a slash and thus is {Coloured("a normal message ")}" + 
									           $"and {Coloured("not", Color.Red)} {Coloured("a command")}.");
								Reply(ctx, $"A binding for key {Coloured(Enum.GetName(key))} has been saved.");
								
								return 1;
							}
							return 0; 
				        }))))
	        
	        .Then(Literal("remove")
		        .Then(Argument("key", KeyArgumentType.Key)
			        .Executes(ctx =>
			        {
				        Keys key = ctx.GetArgument<Keys>("key");
				        
						if (!Bindings.ContainsKey(key)) Error(ctx, "No binding has been set for that key yet.");
						else {
							Bindings.Remove(key);
							SaveData();
							
							Reply(ctx, $"The binding for key {Coloured(Enum.GetName(key))} has been removed.");
							return 1;
						}
						return 0;
			        })))
	        
	        .Then(Literal("list")
		        .Executes(ctx => {
					if (Bindings.Count == 0) Error(ctx, "You have no bindings set yet, consider setting one with /bind set <key> <msg>.");
					else {
						Reply(ctx, "You currently have the following bindings:");
						foreach ((Keys key, string msg) in Bindings)
							Reply(ctx, "  " + Enum.GetName(key) + ": " + Coloured(msg));
						
						return Bindings.Count;
					}
					return 0;
		        }))
	        
	        .Then(Literal("listkeys")
		        .Executes(ctx => {
					Reply(ctx, "The following keys are to your disposal: " + JoinNicelyColoured(Enum.GetValues<Keys>().Select(Enum.GetName)) + ".");
					return Enum.GetValues<Keys>().Length;
		        }))
	        
	        .Then(Literal("record")
		        .Executes(ctx => ExecuteRecord(ctx, 2))
		        .Then(Argument("amount", Arguments.Integer(1))
			        .Executes(ctx => ExecuteRecord(ctx, ctx.GetArgument<int>("amount"))))));
    }
    
    private int ExecuteRecord(CommandContext<CommandSource> ctx, int amount) {
	    _record = amount;
	    Reply(ctx, $"The next {Coloured(amount)} key{(amount == 1 ? "" : "s")} pressed will have their name be printed in chat.");
	    return amount;
    }

    public override void OnUpdate()
    {
	    if (PlayerInput.WritingText || Main.drawingPlayerChat || GetImeToggle(Main.instance)) return;
	    
	    Keys[] pressed = Main.keyState.GetPressedKeys();
	    foreach (Keys key in pressed)
		    if (!_lastPressedKeys.Contains(key))
		    {
			    if (_record > 0)
			    {
				    Main.NewText($"You just pressed the {Coloured(Enum.GetName(key))} key.", DF.R, DF.G, DF.B);
				    _record--;
			    } 
			    else if (Bindings.ContainsKey(key))
				    Util.SendCommand(Bindings[key]);
		    }

	    _lastPressedKeys = pressed;
    }

    private static void SaveData() => MoreCommands.SaveJson("Bindings", Bindings);
}