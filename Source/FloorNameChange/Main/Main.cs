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
            private static Dictionary<string, string> GetReplacementMap()
            {
                return new Dictionary<string, string>
                {
                    { "mahogany", "red mahogany" }
                    // Add more replacements as needed.
                };
            }

            private static Dictionary<string, List<TerrainDef>> GetNewHarvestFloorsMap(IEnumerable<string> substrings)
            {
                const string newHarvestPackageId = "vvenchov.vvnewharvest";

                var terrainDefs = DefDatabase<TerrainDef>.AllDefsListForReading
                    .Where(def =>
                        def.modContentPack?.PackageIdPlayerFacing.Equals(newHarvestPackageId, System.StringComparison.OrdinalIgnoreCase) == true
                        && !string.IsNullOrWhiteSpace(def.label)
                        && def.costList != null
                    )
                    .ToList();

                var result = new Dictionary<string, List<TerrainDef>>();

                foreach (var substr in substrings)
                {
                    var matchingTerrains = terrainDefs
                        .Where(def =>
                            def.costList.Any(thingCount =>
                                thingCount.thingDef.defName.IndexOf(substr, System.StringComparison.OrdinalIgnoreCase) >= 0
                            ) &&
                            def.label.IndexOf(substr, System.StringComparison.OrdinalIgnoreCase) >= 0
                        )
                        .ToList();

                    if (matchingTerrains.Any())
                    {
                        result[substr] = matchingTerrains;
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
                var replacementMap = GetReplacementMap();

                if (replacementMap.Count == 0)
                    return;

                var floorsMap = GetNewHarvestFloorsMap(replacementMap.Keys);

                if (floorsMap.NullOrEmpty())
                    return;

                foreach (var kvp in replacementMap)
                {
                    var strToReplace = kvp.Key;
                    var replacementStr = kvp.Value;

                    if (string.IsNullOrWhiteSpace(strToReplace) || string.IsNullOrWhiteSpace(replacementStr))
                        continue;

                    if (!floorsMap.TryGetValue(strToReplace, out var floorDefs) || floorDefs.NullOrEmpty())
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