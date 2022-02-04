using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using Brigadier.NET.Tree;
using log4net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoMod.RuntimeDetour;
using MoreCommands.Extensions;
using MoreCommands.Hooks;
using MoreCommands.IL;
using MoreCommands.IL.Detours;
using MoreCommands.Misc;
using MoreCommands.Misc.TagHandlers;
using MoreCommands.UI;
using MoreCommands.Utils;
using Newtonsoft.Json;
using ReLogic.Content;
using Terraria;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.GameContent.NetModules;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace MoreCommands;

public class MoreCommands : Mod
{
	public static MoreCommands Instance { get; private set; }
	public static ILog Log => Instance?.Logger;
	public static CommandDispatcher<CommandSource> Dispatcher { get; private set; } = new();
	public static Color DefColour { get; set; } = Color.Orange;
	public static Color SecColour { get; set; } = Color.Yellow;
	public static bool IsClientOp { get; private set; }
	public static (uint requestId, Suggestions suggestions) RequestedSuggestions { get; private set; } = (0, null);
	public static IEnumerable<Command> Commands => CommandsBackend.ToImmutableList();
	public static bool Rainbow
	{
		get => _rainbow;
		set
		{
			_rainbow = value;

			Filters.Scene["MCRainbow"].GetShader().UseProgress(value ? 1 : 0); // Deactivating isn't instant, setting a variable is.
			if (value) Filters.Scene.Activate("MCRainbow");
			else Filters.Scene.Deactivate("MCRainbow");
		}
	}
	public static IEnumerable<string> LegacyCommands => LegacyCommandsBackend.ToImmutableList();
	private static bool _rainbow;
	private static readonly Func<ChatCommandProcessor, Dictionary<LocalizedText, ChatCommandId>> GetLocalizedCommands = 
		Dynamics.CreateGetter<ChatCommandProcessor, Dictionary<LocalizedText, ChatCommandId>>("_localizedCommands");
	private static readonly Func<ChatCommandProcessor, Dictionary<ChatCommandId, IChatCommand>> GetVanillaCommands = 
		Dynamics.CreateGetter<ChatCommandProcessor, Dictionary<ChatCommandId, IChatCommand>>("_commands");
	private static readonly List<string> LegacyCommandsBackend = new();
	internal static ModKeybind CommandKeybind { get; private set; }
	internal static UserInterface CommandTileInterface;
	internal static CommandTileUI CommandTileUI;
	internal static UserInterface SuggestionsInterface;
	internal static UserInterface DisposalInterface;
	internal static DisposalUI DisposalUI;
	private static (uint requestId, Suggestions suggestions) _clientSuggestions = (0, null);
	private static readonly IList<Command> CommandsBackend = new List<Command>();
	private static readonly IList<Detour> Detours = new List<Detour>();
	private static readonly IDictionary<CommandNode<CommandSource>, Command> CommandOrigins = new Dictionary<CommandNode<CommandSource>, Command>();
	private static readonly IDictionary<CommandNode<CommandSource>, CommandNode<CommandSource>> AbsoluteParents = new Dictionary<CommandNode<CommandSource>, CommandNode<CommandSource>>(); 

	public override void Load()
	{
		Instance = this;

		MonoModHooks.RequestNativeAccess();
		CreateDetour(typeof(CommandLoader).GetMethod("HandleCommand", BindingFlags.NonPublic | BindingFlags.Static), typeof(CommandLoaderDetours).GetMethod("HandleCommand", 
			BindingFlags.Public | BindingFlags.Static, new []{typeof(string), typeof(CommandCaller)}));
		CreateDetour(typeof(CommandLoader).GetMethod("GetCommand", BindingFlags.NonPublic | BindingFlags.Static), typeof(CommandLoaderDetours).GetMethod("GetCommand"));
		CreateDetour(typeof(NetTextModule).GetMethod("DeserializeAsServer", BindingFlags.NonPublic | BindingFlags.Instance), typeof(NetTextModuleDetours).GetMethod("DeserializeAsServer"));

		ILManipulator.RegisterManipulations();
		LangHelper.LoadEnglishLanguage();
		RegisterCommands();

		if (!Main.dedServ)
		{
			CommandTileInterface = new UserInterface();
			CommandTileUI = new CommandTileUI();
			CommandTileUI.Initialize();
			
			SuggestionsInterface = new UserInterface();

			DisposalInterface = new UserInterface();
			DisposalUI = new DisposalUI();
			DisposalUI.Initialize();
			
			ChatManager.Register<ClickTagHandler>("click");
			ChatManager.Register<HoverTagHandler>("hover");
			ChatManager.Register<StyledTagHandler>("styled");

			ScreenShaderData rainbowScreenData = new(new Ref<Effect>(Assets.Request<Effect>("Assets/Shaders/RainbowShader",
				AssetRequestMode.ImmediateLoad).Value), "RainbowScreen");
			rainbowScreenData.SwapProgram("RainbowScreen");
			Filters.Scene["MCRainbow"] = new Filter(rainbowScreenData, EffectPriority.Medium);
		}

		Logging.IgnoreExceptionSource("MoreCommands");
		Logging.IgnoreExceptionSource("Brigadier.NET");

		CommandKeybind = KeybindLoader.RegisterKeybind(this, "Chat Command", Keys.OemQuestion);
	}

	public override void PostAddRecipes()
	{
		IdHelper.Init();
		
		Dictionary<LocalizedText, ChatCommandId> localizedCommands = GetLocalizedCommands(ChatManager.Commands);
		foreach (LocalizedText name in from KeyValuePair<ChatCommandId, IChatCommand> pair in GetVanillaCommands(ChatManager.Commands)
		         select localizedCommands.Select(pair0 => (KeyValuePair<LocalizedText, ChatCommandId>?) pair0)
			         .FirstOrDefault(pair0 => pair0.Value.Value.Equals(pair.Key))?.Key into name where name is not null select name)
			LegacyCommandsBackend.Add(name.Value[1..]); // Name starts with a slash
		
		foreach ((string name, List<ModCommand> commands) in Util.GetCommands())
			if (!commands.Any(cmd => cmd is ModCommandWrapper)) 
				LegacyCommandsBackend.Add(name);
		
		Log.Debug("Legacy commands: " + Command.JoinNicely(LegacyCommandsBackend));
		
		Dispatcher.FindAmbiguities((parent, child, sibling, inputs) =>
		{
			if (AbsoluteParents.TryGetValue(parent, out CommandNode<CommandSource> absParent) && CommandOrigins[absParent].IgnoreAmbiguities) return;
			Log.Debug($"Found ambiguity on command {Dispatcher.GetPath(parent).First()} between children {child.Name} and {sibling.Name} with inputs {Command.JoinNicely(inputs)}.");
		});
	}

	public override void Unload()
	{
		Instance = null;
		Dispatcher = null;
		DefColour = default;
		CommandKeybind = null;
		RequestedSuggestions = (0, null);
		
		foreach (Detour detour in Detours)
			detour.Dispose();
		Detours.Clear();
		ILManipulator.UnregisterEdits();
	}

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		byte id;
		try
		{
			id = reader.ReadByte();
		}
		catch (Exception)
		{
			Log.Warn($"Player {whoAmI} ({Main.player[whoAmI].name}) sent a packet without id.");
			return;
		}
		
		if (Main.netMode == NetmodeID.Server) switch (id)
		{
			// C2S PACKETS
			
			case 0: // Suggestions request packet
				uint requestId = reader.ReadUInt32();
				string text = reader.ReadString();
				CommandSource source = new(new ServerPlayerCommandCaller(Main.player[whoAmI]));
				
				Dispatcher.GetCompletionSuggestions(Dispatcher.Parse(text, source))
					.ContinueWith(task =>
					{
						Suggestions suggestions = task.Result;
						ModPacket packet = GetPacket();
						
						packet.Write((byte) 1);
						packet.Write(requestId);
						packet.Write(suggestions.Range.Start);
						packet.Write(suggestions.Range.End);
						packet.Write(suggestions.List.Count);
						
						foreach (Suggestion suggestion in suggestions.List)
						{
							packet.Write(suggestion.Text);
							packet.Write(suggestion.Tooltip is null ? "" : suggestion.Tooltip.String);
						}
						
						packet.Send(whoAmI);
					});
				break;
		}
		else switch (id)
		{
			// S2C PACKETS
			
			case 0: // Operator packet, only received when we're op(ped) or deopped.
				IsClientOp = reader.ReadBoolean();
				break;
			case 1: // Suggestions receive packet
				uint requestId = reader.ReadUInt32();
				StringRange range = StringRange.Between(reader.ReadInt32(), reader.ReadInt32());
				int count = reader.ReadInt32();

				List<Suggestion> list = new();
				for (int i = 0; i < count; i++)
				{
					string text = reader.ReadString();
					string tooltip = reader.ReadString();
					list.Add(new Suggestion(range, text, string.IsNullOrEmpty(tooltip) ? null : new LiteralMessage(tooltip)));
				}

				if (_clientSuggestions.requestId == requestId) list = _clientSuggestions.suggestions.List.Where(suggestion => !list.Contains(suggestion)).Concat(list).ToList();
				RequestedSuggestions = (requestId, new Suggestions(_clientSuggestions.requestId == requestId && _clientSuggestions.suggestions.Range.Start > range.Start ?
					_clientSuggestions.suggestions.Range : range, list));
				break;
		}
	}

	private static void RegisterCommands()
	{
		IDictionary<string, List<ModCommand>> commands = Util.GetCommands();

		IEnumerable<Command> commandsToRegister = Assembly.GetAssembly(typeof(MoreCommands))?.GetTypesIncludingNested()
			.Where(t => !t.IsAbstract && typeof(Command).IsAssignableFrom(t))
			.Select(type =>
			{
				try
				{
					return (Command) Activator.CreateInstance(type);
				}
				catch (Exception e)
				{
					Log.Error($"Could not load command of type {type.Name}.", e);
					return null;
				}
			})
			.Where(cmd => cmd is not null)
			.ToList();

		foreach (Command command in commandsToRegister!)
		{
			if (command.ServerOnly && !Main.dedServ || command.Type == CommandType.Chat && Main.dedServ) continue;
			command.Init();

			IList<CommandNode<CommandSource>> before = new List<CommandNode<CommandSource>>(Dispatcher.GetRoot().Children);
			command.Register(Dispatcher);
			CommandsBackend.Add(command);

			// Command might have registered multiple commands/aliases.
			Dispatcher.GetRoot().Children
				.Where(node => node is LiteralCommandNode<CommandSource> && !before.Contains(node))
				.ToList()
				.ForEach(node =>
				{
					CommandOrigins[node] = command;
					RegisterAbsoluteParents(node, node);
					
					commands.ComputeIfAbsent(node.Name, _ => new List<ModCommand>())
						.Add(new ModCommandWrapper(Dispatcher, node.Name, command));
				});
		}
		
		Log.Info($"Registered {commandsToRegister.Count()} commands.");
	}

	private static void RegisterAbsoluteParents(CommandNode<CommandSource> node, CommandNode<CommandSource> absoluteParent)
	{
		AbsoluteParents[node] = absoluteParent;
		
		foreach (CommandNode<CommandSource> child in node.Children)
			RegisterAbsoluteParents(child, absoluteParent);
	}

	private static void CreateDetour(MethodBase from, MethodBase to) => Detours.Add(new Detour(@from, to));

	public static bool IsOp(int playerId) => ModContent.GetInstance<SystemHooks>().Operators.Contains(Util.GetAddress(playerId).GetIdentifier());

	public static void RequestSuggestions(uint requestId)
	{
		Dispatcher.GetCompletionSuggestions(Dispatcher.Parse(Main.chatText[1..], new CommandSource(new ClientPlayerCommandCaller())))
			.ContinueWith(task =>
			{
				_clientSuggestions = (requestId, task.Result);
				
				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					ModPacket packet = Instance.GetPacket();
					packet.Write((byte) 0); // Suggestions request packet.
					packet.Write(requestId);
					packet.Write(Main.chatText[1..]);
					packet.Send();
				}
				else Dispatcher.GetCompletionSuggestions(Dispatcher.Parse(Main.chatText[1..], new CommandSource(new ServerPlayerCommandCaller(Main.LocalPlayer))))
					.ContinueWith(task0 => RequestedSuggestions = (requestId, new Suggestions(task0.Result.Range.Start > _clientSuggestions.suggestions.Range.Start ? 
							task0.Result.Range : _clientSuggestions.suggestions.Range, _clientSuggestions.suggestions.List
							.Where(suggestion => !task0.Result.List.Contains(suggestion)).Concat(task0.Result.List).ToList())));
			});
	}

	private static string GetConfigPath(string name) => Path.Combine(Util.ConfigDirPath, $"MoreCommands_{name}.json");
	public static bool ConfigExists(string name) => File.Exists(GetConfigPath(name));

	public static T ReadJson<T>(string name) where T : new() => !File.Exists(GetConfigPath(name)) ? new T() : JsonConvert.DeserializeObject<T>(File.ReadAllText(GetConfigPath(name)));
	
	public static void SaveJson(string name, object data) => File.WriteAllText(GetConfigPath(name), JsonConvert.SerializeObject(data, Formatting.Indented));
}