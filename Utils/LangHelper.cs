using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Newtonsoft.Json;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace MoreCommands.Utils;

// Ensures that no matter what language you've got selected,
// names used in commands will always be in English.
// Code largely taken from Terraria.Localization.LanguageManager
public static class LangHelper
{
    private static readonly IDictionary<string, string> Texts = new Dictionary<string, string>();
    private static readonly IDictionary<string, List<string>> CategoryGroupedKeys = new Dictionary<string, List<string>>();
    
    internal static void LoadEnglishLanguage()
    {
        string[] languageFiles = Array.FindAll(typeof(Program).Assembly.GetManifestResourceNames(),
            element => element.StartsWith("Terraria.Localization.Content." + GameCulture.FromCultureName(GameCulture.CultureName.English)
                .CultureInfo.Name.Replace('-', '_')) && element.EndsWith(".json"));

        foreach (string path in languageFiles)
            try
            {
                string fileText = Terraria.Utils.ReadEmbeddedResource(path);
                if (fileText is null || fileText.Length < 2)
                    throw new FormatException();
                
                foreach ((string category, Dictionary<string, string> entries) in JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileText))
                    foreach ((string key, string value) in entries)
                    {
                        string langKey = category + "." + key;

                        if (Texts.ContainsKey(langKey)) Texts[langKey] = value;
                        else
                        {
                            Texts.Add(langKey, value);

                            if (!CategoryGroupedKeys.ContainsKey(category))
                                CategoryGroupedKeys.Add(category, new List<string>());
                            CategoryGroupedKeys[category].Add(key);
                        }
                    }
            }
            catch (Exception ex)
            {
                MoreCommands.Log.Error("Failed to load language file: " + path, ex);
                break;
            }
        
        MoreCommands.Log.Debug($"Loaded {Texts.Count} texts.");
    }

    public static IEnumerable<string> GetKeys() => Texts.Keys.ToImmutableList();

    public static IEnumerable<string> GetTexts() => Texts.Values.ToImmutableList();

    public static string GetText(string key) => Texts[key];

    public static string GetEnglish(LocalizedText text) => Texts.ContainsKey(text.Key) ? GetText(text.Key) : text.Value;
    
    public static LocalizedText GetTileName(int tile)
    {

        LocalizedText tileName;
        try
        {
            tileName = Lang._mapLegendCache[MapHelper.tileLookup[tile]];
        }
        catch (Exception)
        {
            tileName = LocalizedText.Empty;
        }
        
        return tileName is not null && tileName.Value != "" ? tileName : Util.NewLocalizedText("MoreCommands.Synthetic", 
            Util.Beautify(tile < TileID.Search.Count ? TileID.Search.GetName(tile) : TileLoader.GetTile(tile)?.Name ?? "UNKNOWN"));
    }
}