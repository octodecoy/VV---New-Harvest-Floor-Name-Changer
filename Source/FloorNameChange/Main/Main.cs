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
                return new Dictionary<string, (string replacement, List<ThingDef> defs)>
                {
                    {
                        "mahogany",
                        (
                            "VV_MahoganyTranslation".Translate(),
                            new List<ThingDef>
                            {
                                DefDatabase<ThingDef>.GetNamedSilentFail("VV_MahoganyWood"),
                                DefDatabase<ThingDef>.GetNamedSilentFail("VV_MahoganyLumber"),
                            }.Where(def => def != null).ToList()
                        )
                    }
                    // Add more replacements as needed.
                };
            }


            private static Dictionary<string, (string replacement, List<TerrainDef> terrainList)> GetNewHarvestFloorsMap()
            {

                var dict = GetReplacementMap();

                var allDefs = new HashSet<ThingDef>(
                    dict.Values.SelectMany(tuple => tuple.defs)
                );

                var terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading
                    .Where(def => def.costList != null &&
                                  !string.IsNullOrWhiteSpace(def.label) &&
                                  def.costList.Any(cost => allDefs.Contains(cost.thingDef))
                    )
                    .ToList();

                var result = new Dictionary<string, (string replacement, List<TerrainDef> defs)>();

                foreach (var kvp in dict)
                {
                    var matchingTerrains = terrainDefs
                        .Where(def => def.label.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0
                        )
                        .ToList();

                    if (matchingTerrains.Any())
                    {
                        result[kvp.Key] = (kvp.Value.replacement, matchingTerrains);
                    }
                }

                return result;
            }

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

                    var regex = new Regex(Regex.Escape(strToReplace), RegexOptions.IgnoreCase);

                    foreach (var def in floorDefs)
                    {
                        def.label = ReplaceIgnoreCase(def.label, strToReplace, replacementStr);
                    }
                }
            }
        }
    }
}