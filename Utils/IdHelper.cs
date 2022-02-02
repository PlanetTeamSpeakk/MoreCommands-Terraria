using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Brigadier.NET.Context;
using MoreCommands.Misc;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreCommands.Utils;

public static class IdHelper
{
    private static readonly IDictionary<IdType, IDictionary<string, int>> Ids = new Dictionary<IdType, IDictionary<string, int>>();
    private static readonly IDictionary<IdType, IDictionary<int, string>> Names = new Dictionary<IdType, IDictionary<int, string>>();

    internal static void Init()
    {
        foreach (IdType type in Enum.GetValues<IdType>())
        {
            LocalizedText[] translations = GetTranslationsFor(type);
            Dictionary<int, string> names = GetIdsFor(type).ToDictionary(id => id, id => LangHelper.GetEnglish(translations[id + (type == IdType.Npc ? 65 : 0)]));

            Names[type] = names.ToImmutableDictionary();
            Ids[type] = names
                .GroupBy(pair => pair.Value)
                .ToImmutableDictionary(g => g.Key.ToLower(), g => g.First().Key);
            
            MoreCommands.Log.Debug($"Found {names.Count} entries for type {Enum.GetName(type)}.");
        }
    }

    private static IEnumerable<int> GetIdsFor(IdType type) => type switch
    { 
        IdType.Item => Enumerable.Range(0, ItemLoader.ItemCount),
        IdType.Buff => Enumerable.Range(0, BuffLoader.BuffCount),
        IdType.Npc => Enumerable.Range(-65, 65).Concat(Enumerable.Range(0, NPCLoader.NPCCount)),
        IdType.Tile => Enumerable.Range(0, TileLoader.TileCount),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static LocalizedText[] GetTranslationsFor(IdType type) => type switch
    {
        IdType.Item => GetTranslations("_itemNameCache"),
        IdType.Buff => GetTranslations("_buffNameCache"),
        IdType.Npc => GetTranslations("_negativeNpcNameCache").Reverse().Concat(GetTranslations("_npcNameCache")).ToArray(),
        IdType.Tile => Enumerable.Range(0, TileLoader.TileCount).Select(LangHelper.GetTileName).ToArray(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static LocalizedText[] GetTranslations(string fieldName) => (LocalizedText[])
        typeof(Lang).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);

    public static string GetName(IdType type, int id) => Names[type][id];

    public static int GetId(IdType type, string name) => Ids[type][name.ToLower()];

    public static IEnumerable<string> GetNames(IdType type) => Names[type].Values.ToImmutableList();

    public static IEnumerable<string> GetNamesLower(IdType type) => Ids[type].Keys
        .Distinct()
        .Where(name => name.Trim().Length > 0)
        .ToImmutableList();

    public static IEnumerable<int> GetIds(IdType type) => Ids[type].Values.ToImmutableList();

    public static IDictionary<int, string> Search(IdType type, string query)
    {
        if (query is null)
            return ImmutableDictionary<int, string>.Empty;
        
        query = query.ToLower();
        return Ids[type]
            .Where(pair => pair.Key.Contains(query))
            .ToImmutableDictionary(pair => pair.Value, pair => Names[type][pair.Value]);
    }

    public static int SearchCommand(CommandContext<CommandSource> ctx, string argName, IdType type)
    {
        string query = ctx.GetArgument<string>(argName).ToLower();
        IDictionary<int, string> results = Search(type, query);

        if (!results.Any()) ctx.Source.Error($"No {Enum.GetName(type)?.ToLower()}s were found with the given query.");
        else if (results.Count == 1)
            ctx.Source.Reply($"Found {Enum.GetName(type)?.ToLower()} {results.Values.First()} with id {results.Keys.First()}.");
        else
        {
            ctx.Source.Reply($"A total of {results.Count} {Enum.GetName(type)?.ToLower()}s were found:");

            int index = 1;
            foreach ((int key, string value) in results)
                ctx.Source.Reply($"{index++}. {value}: {key}");
        }

        return results.Count;
    }
}

public enum IdType
{
    Item,
    Buff,
    Npc,
    Tile
}