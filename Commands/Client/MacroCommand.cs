using System.Collections.Generic;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Context;
using Microsoft.Xna.Framework;
using MoreCommands.Extensions;
using MoreCommands.Misc;
using MoreCommands.Utils;
using Terraria.Chat;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class MacroCommand : Command
{
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Manage or run macros.";
    public override bool IgnoreAmbiguities => true;

    private readonly IDictionary<string, IList<string>> _macros = new Dictionary<string, IList<string>>();

	internal override void Init() {
		if (!MoreCommands.ConfigExists("Macros")) SaveData();
		else _macros.AddAll(MoreCommands.ReadJson<Dictionary<string, IList<string>>>("Macros"));
	}

	public override void Register(CommandDispatcher<CommandSource> dispatcher) {
		dispatcher.Register(RootLiteral("macro")
			.Then(Literal("create")
				.Then(Argument("name", Arguments.GreedyString())
					.Executes(ctx => 
					{
						string name = ctx.GetArgument<string>("name");
						
						if (_macros.ContainsKey(name)) Reply(ctx, "A macro by that name already exists, to add commands to it, use " + Coloured("/macro add <macro> <command>") + ".");
						else {
							_macros[name] = new List<string>();
							SaveData();
							
							Reply(ctx, "The macro has been created, you can add commands to it with " + Coloured("/macro add " + (name.Contains(' ') ? "\"" + name + "\"" : name) + " <command>") + ".");
							return 1;
						}
						
						return 0;
					})))
			
			.Then(Literal("add")
				.Then(Argument("macro", Arguments.String())
					.Then(Argument("msg", Arguments.GreedyString())
						.Executes(ctx => ExecuteAdd(ctx, -1)))
					.Then(Argument("index", Arguments.Integer())
						.Then(Argument("msg", Arguments.GreedyString())
							.Executes(ctx => ExecuteAdd(ctx, ctx.GetArgument<int>("index")))))))
			
			.Then(Literal("remove")
				.Then(Argument("macro", Arguments.GreedyString())
					.Executes(ctx => {
						string macro = ctx.GetArgument<string>("macro");
						
						if (!_macros.ContainsKey(macro)) Error(ctx, "A macro by the given name could not be found.");
						else {
							_macros.Remove(macro);
							SaveData();
							Reply(ctx, "The macro has been removed.");
							return 1;
						}
						
						return 0;
					}))
				
				.Then(Argument("macro", Arguments.String())
					.Then(Argument("index", Arguments.Integer(1))
						.Executes(ctx => {
							string macro = ctx.GetArgument<string>("macro");
							int index = ctx.GetArgument<int>("index") - 1;
							
							if (!_macros.ContainsKey(macro)) Error(ctx, "A macro by the given name could not be found.");
							else if (index >= _macros[macro].Count) Error(ctx, "The given index was greater than the amount of commands in this macro (" + Coloured(_macros[macro].Count) + ").");
							else {
								string cmd = _macros[macro][index];
								_macros[macro].RemoveAt(index);
								SaveData();
								
								Reply(ctx, "The command " + Coloured(cmd) + " with an index of " + Coloured(index) + " has been removed.");
								return _macros[macro].Count + 1;
							}
							
							return 0;
						}))))
			
			.Then(Literal("view")
				.Then(Argument("macro", Arguments.GreedyString())
					.Executes(ctx => {
						string macro = ctx.GetArgument<string>("macro");
						
						if (!_macros.ContainsKey(macro)) Error(ctx, "A macro by the given name could not be found.");
						else if (_macros[macro].Count == 0) Error(ctx, "The given macro does not yet have any commands added, consider adding some with " + "/macro add " +
						                                               (macro.Contains(' ') ? "\"" + macro + "\"" : macro) + " <command>.");
						else {
							StringBuilder msg = new("Commands of macro " + Coloured(macro) + ":");
							for (int i = 0; i < _macros[macro].Count; i++)
								msg.Append("\n  ").Append(i + 1).Append(". ").Append(SF).Append(_macros[macro][i]);
							
							Reply(ctx, msg.ToString());
							return 1;
						}
						return 0;
					})))
			
			.Then(Literal("list")
				.Executes(ctx => {
					if (_macros.Count == 0) Error(ctx, "You have not made any macros yet, consider making some with /macro create <name>.");
					else Reply(ctx, "You have the following macros: " + JoinNicelyColoured(_macros.Keys) + ".");
					
					return _macros.Count;
				}))
			
			.Then(Argument("macro", Arguments.GreedyString())
				.Executes(ctx => {
					string macro = ctx.GetArgument<string>("macro");
					
					if (!_macros.ContainsKey(macro)) Error(ctx, "A macro by the given name could not be found.");
					else {
						foreach (string msg in _macros[macro]) Util.SendMsg(msg);
						return _macros[macro].Count;
					}
					
					return 0;
			})));
	}

	private int ExecuteAdd(CommandContext<CommandSource> ctx, int index) {
		string macro = ctx.GetArgument<string>("macro");
		string msg = ctx.GetArgument<string>("msg");
		
		if (!_macros.ContainsKey(macro)) Error(ctx, "A macro by the given name could not be found.");
		else {
			_macros[macro].Insert(index == -1 ? _macros[macro].Count : index, msg);
			SaveData();
			
			if (!msg.StartsWith("/")) Reply(ctx, "The message does not start with a slash and thus is " + Coloured("a normal message ") + "and " +
			                                     Styled("not ").WithUnderline().WithColour(Color.Red) + Coloured("a command") + ".");
			Reply(ctx, "The message has been added to macro " + Coloured(macro) + " with an index of " + Coloured(_macros[macro].Count - 1) + ".");
			
			return _macros[macro].Count;
		}
		
		return 0;
	}

	private void SaveData() => MoreCommands.SaveJson("Macros", _macros);
}