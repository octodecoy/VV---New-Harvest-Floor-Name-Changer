using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;

namespace FloorNameChanger
{
    [StaticConstructorOnStartup]
    public static class Initialize
    {
        static Initialize()
        {
            LongEventHandler.ExecuteWhenFinished(() => FloorReLabeler.ReLabelFloors());
        }

        public class FloorReLabeler
        {
            private static Dictionary<string, (string replacement, List<ThingDef> defs)> GetReplacementMap()
            {
                var defs = new List<ThingDef>();
                var mahoganyWood = DefDatabase<ThingDef>.GetNamed("VV_MahoganyWood");
                if (mahoganyWood != null) defs.Add(mahoganyWood);
                var mahoganyLumber = DefDatabase<ThingDef>.GetNamedSilentFail("VV_MahoganyLumber");
                if (mahoganyLumber != null) defs.Add(mahoganyLumber);

                if (defs.Count == 0)
                {
                    Log.Error("[New Harvest] No wood defs found.");
                    return null;
                }

                return new Dictionary<string, (string replacement, List<ThingDef> defs)>
                {
                    {
                        "mahogany",
                        (
                            "VV_MahoganyTranslation".Translate(),
                            defs
                        )
                    }
                    // More replacements if needed.
                };

            }


            private static Dictionary<string, (string replacement, List<TerrainDef> terrainList)> GetNewHarvestFloorsMap()
            {

                var map = GetReplacementMap();

                if (map.NullOrEmpty())
                    return null;

                var allDefs = new HashSet<ThingDef>(
                    map.Values.SelectMany(tuple => tuple.defs)
                );

                var terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading
                    .Where(def => !def.costList.NullOrEmpty() &&
                                  !string.IsNullOrWhiteSpace(def.label) &&
                                  def.costList.Any(cost => allDefs.Contains(cost.thingDef))
                    );

                var result = new Dictionary<string, (string replacement, List<TerrainDef> defs)>();

                foreach (var kvp in map)
                {
                    var matchingTerrains = new List<TerrainDef>();
                    foreach (var def in terrainDefs)
                    {
                        if (def.label.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            matchingTerrains.Add(def);
                    }

                    if (matchingTerrains.Count > 0)
                    {
                        result[kvp.Key] = (kvp.Value.replacement, matchingTerrains);
                    }
                }

                return result;
            }

            // Case-insensitive string replacement using Regex.
            private static string ReplaceIgnoreCase(string input, string target, string replacement)
            {
                return Regex.Replace(input, Regex.Escape(target), replacement, RegexOptions.IgnoreCase);
            }

            public static void ReLabelFloors()
            {
                var floorsMap = GetNewHarvestFloorsMap();
                if (floorsMap.NullOrEmpty())
                    return;

                foreach (var kvp in floorsMap)
                {
                    var strToReplace = kvp.Key;
                    var replacementStr = kvp.Value.replacement;
                    var floorDefs = kvp.Value.terrainList;

                    if (string.IsNullOrWhiteSpace(strToReplace) || string.IsNullOrWhiteSpace(replacementStr))
                        continue;

                    foreach (var def in floorDefs)
                    {
                        def.label = ReplaceIgnoreCase(def.label, strToReplace, replacementStr);
                    }
                }
            }
        }
    }
}