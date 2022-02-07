using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Brigadier.NET;
using Microsoft.Xna.Framework;
using MoreCommands.Misc;
using MoreCommands.Misc.Styling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;

namespace MoreCommands.Commands.Client;

public class UrbanCommand : Command
{
	public override CommandType Type => CommandType.Chat;
    public override bool Console => true;
    public override string Description => "Lookup words on Urban Dictionary.";
    private static readonly IDictionary<string, JArray> Cache = new Dictionary<string, JArray>();
    private static readonly HttpClient Client = new();

    public override void Register(CommandDispatcher<CommandSource> dispatcher)
    {
        dispatcher.Register(RootLiteral("urban")
            .Then(Argument("query", Arguments.GreedyString())
                .Executes(ctx =>
                {
	                string query = ctx.GetArgument<string>("query");
	                if (int.TryParse(query.Split(" ")[0], out int result))
						query = query[(query.IndexOf(' ') + 1)..];
					
					(Cache.ContainsKey(query.ToLower()) ? Task.FromResult(Cache[query.ToLower()]) : Client.GetStringAsync("https://api.urbandictionary.com/v0/define?term=" + query.Replace(' ', '+'))
							.ContinueWith(resp => (JArray) JsonConvert.DeserializeObject<JObject>(resp.Result)["list"]))
						.ContinueWith(resp =>
						{
							JArray results = resp.Result;
							Cache[query.ToLower()] = results;
							
							if (result >= results.Count) Error(ctx, $"Only found {Coloured(results.Count, Color.DarkRed)} results for query {Coloured(query, Color.DarkRed)} while result " +
							                                        $"{Coloured(result, Color.DarkRed)} was requested.");
							else
							{
								JObject data = (JObject) results[result];
								
								Reply(ctx, $"Result for query {Coloured(query)}:");
								Reply(ctx, ParseText(CleanString(data["definition"]!.ToString())));
								
								string ex = string.IsNullOrEmpty(data["example"]?.ToString()) ? null : data["example"].ToString();
								
								if (ex is not null) {
									Reply(ctx, "Example:");
									Reply(ctx, ParseText(CleanString(ex)));
								}
								
								Reply(ctx, ""); // Empty line
								Reply(ctx, Coloured("Thumbs up: " + data["thumbs_up"], Color.Green));
								Reply(ctx, Coloured("Thumbs down: " + data["thumbs_down"], Color.Red));
								Reply(ctx, $"Click {Styled("here").WithClick(ClickAction.OpenUrl(data["permalink"]!.ToString())).WithColour(Color.DodgerBlue).WithUnderline()} to view this result in the browser.");
								Reply(ctx, "");
								
								DateTime date = DateTime.Parse(data["written_on"]!.ToString());
								Reply(ctx, $"Written by {Coloured(data["author"])} on {Coloured(date.ToString("MMMM d yyyy"))} at {Coloured(date.ToString("HH:mm:ss"))}.");
								
								Reply(ctx, $@"{Styled("<<<").WithClick(result > 0 ? ClickAction.RunCommand("/urban " + (result - 1)) : ClickAction.Empty)
									.WithColour(result > 0 ? Color.Green : Color.Gray).WithHover(result > 0 ? "Previous page" : null)}  " +
								           $@"{Styled(">>>").WithClick(result < results.Count - 1 ? ClickAction.RunCommand("/urban " + (result + 1)) : ClickAction.Empty)
									           .WithColour(result < results.Count - 1 ? Color.Green : Color.Gray).WithHover(result < results.Count - 1 ? "Next page" : null)}");
							}
						});

					return 1;
                })));
    }
    
    private static string CleanString(string s) {
	    s = s.Replace("\r", "");
	    while (s.Contains("\n\n")) s = s.Replace("\n\n", "\n");
	    return s.Trim();
    }
    
    private static string ParseText(string s) => Regex.Replace(s, @"\[([^\]]*)\]", m => Styled(m.Groups[1].Value)
	    .WithClick(ClickAction.RunCommand("/urban " + m.Groups[1].Value))
	    .WithHover("Click to look up " + m.Groups[1].Value).WithColour(SF)
	    .WithUnderline()
	    .BuildString());
}