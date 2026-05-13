using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Mixed_Manhunter_Attacks.Patches
{
    internal static class MixedManhunterPackUtility
    {
        private const int MaxSpecies = 8;

        public static bool TryGenerateAnimals(Map map, float points, int pawnCount, out List<Pawn> pawns, out PawnKindDef primaryKind)
        {
            pawns = null;
            primaryKind = null;

            if (!AggressiveAnimalIncidentUtility.TryFindAggressiveAnimalKind(points, map, out primaryKind))
            {
                return false;
            }

            int totalCount = pawnCount > 0 ? pawnCount : AggressiveAnimalIncidentUtility.GetAnimalsCount(primaryKind, points);
            if (totalCount < 2)
            {
                return false;
            }

            List<PawnKindDef> candidates = GetCandidateKinds(map, points, primaryKind);
            int speciesCount = CalculateSpeciesCount(totalCount, candidates.Count);
            if (speciesCount < 2)
            {
                return false;
            }

            List<PawnKindDef> selectedKinds = SelectKinds(candidates, primaryKind, points, speciesCount);
            if (selectedKinds.Count < 2)
            {
                return false;
            }

            List<int> counts = SplitCount(totalCount, selectedKinds.Count);
            pawns = new List<Pawn>(totalCount);
            for (int i = 0; i < selectedKinds.Count; i++)
            {
                for (int j = 0; j < counts[i]; j++)
                {
                    pawns.Add(PawnGenerator.GeneratePawn(new PawnGenerationRequest(selectedKinds[i], null, PawnGenerationContext.NonPlayer, map.Tile)));
                }
            }

            pawns.Shuffle();
            return pawns.Count > 0;
        }

        private static List<PawnKindDef> GetCandidateKinds(Map map, float points, PawnKindDef primaryKind)
        {
            bool polluted = ModsConfig.BiotechActive && map.Tile.Valid && Rand.Value < WildAnimalSpawner.PollutionAnimalSpawnChanceFromPollutionCurve.Evaluate(Find.WorldGrid[map.Tile].pollution);
            List<PawnKindDef> candidates = BiomeAnimalKinds(map, polluted).Where(k => CanArriveManhunter(k) && AggressiveAnimalIncidentUtility.AnimalWeight(k, points) > 0f).Distinct().ToList();

            if (polluted && candidates.Count == 0)
            {
                polluted = false;
                candidates = BiomeAnimalKinds(map, polluted).Where(k => CanArriveManhunter(k) && AggressiveAnimalIncidentUtility.AnimalWeight(k, points) > 0f).Distinct().ToList();
            }

            if (candidates.Count == 0)
            {
                candidates = DefDatabase<PawnKindDef>.AllDefs.Where(k => CanArriveManhunter(k)
                    && AggressiveAnimalIncidentUtility.AnimalWeight(k, points) > 0f
                    && (!map.Tile.Valid || Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(map.Tile, k.race))).ToList();
            }

            if (primaryKind != null && !candidates.Contains(primaryKind))
            {
                candidates.Add(primaryKind);
            }

            return candidates.Where(k => IsReasonableSecondaryKind(primaryKind, k)).ToList();
        }

        private static IEnumerable<PawnKindDef> BiomeAnimalKinds(Map map, bool polluted)
        {
            if (polluted)
            {
                return map.Biomes.SelectMany(b => b.AllWildAnimals).Where(k => map.Biomes.Any(b => b.CommonalityOfPollutionAnimal(k) > 0f));
            }

            if (map.TileInfo.IsCoastal)
            {
                return map.Biomes.SelectMany(b => b.AllWildAnimals).Where(k => map.Biomes.Any(b => b.CommonalityOfAnimal(k) > 0f || b.CommonalityOfCoastalAnimal(k) > 0f));
            }

            return map.Biomes.SelectMany(b => b.AllWildAnimals).Where(k => map.Biomes.Any(b => b.CommonalityOfAnimal(k) > 0f));
        }

        private static bool CanArriveManhunter(PawnKindDef kind)
        {
            return kind != null && kind.RaceProps.Animal && kind.canArriveManhunter && kind.RaceProps.CanPassFences;
        }

        private static bool IsReasonableSecondaryKind(PawnKindDef primaryKind, PawnKindDef candidate)
        {
            if (primaryKind == null || candidate == primaryKind)
            {
                return true;
            }

            float lower = primaryKind.combatPower * 0.35f;
            float upper = primaryKind.combatPower * 1.75f;
            return candidate.combatPower >= lower && candidate.combatPower <= upper;
        }

        private static int CalculateSpeciesCount(int totalCount, int candidateCount)
        {
            int max = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(totalCount) * 0.8f), 1, MaxSpecies);
            int min = Mathf.Clamp(Mathf.FloorToInt(totalCount / 18f) + 1, 1, max);
            if (totalCount >= 8)
            {
                min = Mathf.Max(min, 2);
            }

            int minPerSpecies = MinimumAnimalsPerSpecies(totalCount);
            max = Mathf.Min(max, Mathf.Max(1, totalCount / minPerSpecies), candidateCount);
            min = Mathf.Min(min, max);
            if (max <= 1)
            {
                return 1;
            }

            int result = Rand.RangeInclusive(min, max);
            float largeGroupTrend = Mathf.InverseLerp(25f, 100f, totalCount);
            if (result < max && Rand.Chance(largeGroupTrend * 0.35f))
            {
                result++;
            }
            return result;
        }

        private static List<PawnKindDef> SelectKinds(List<PawnKindDef> candidates, PawnKindDef primaryKind, float points, int speciesCount)
        {
            List<PawnKindDef> selected = new List<PawnKindDef>();
            if (primaryKind != null && candidates.Contains(primaryKind))
            {
                selected.Add(primaryKind);
            }

            List<PawnKindDef> pool = candidates.Where(k => k != primaryKind).ToList();
            while (selected.Count < speciesCount && pool.TryRandomElementByWeight(k => SelectionWeight(k, primaryKind, points), out PawnKindDef picked))
            {
                selected.Add(picked);
                pool.Remove(picked);
            }

            return selected;
        }

        private static float SelectionWeight(PawnKindDef candidate, PawnKindDef primaryKind, float points)
        {
            float weight = AggressiveAnimalIncidentUtility.AnimalWeight(candidate, points);
            if (primaryKind == null || weight <= 0f)
            {
                return weight;
            }

            float high = Mathf.Max(candidate.combatPower, primaryKind.combatPower);
            float low = Mathf.Max(1f, Mathf.Min(candidate.combatPower, primaryKind.combatPower));
            return weight / Mathf.Max(1f, high / low);
        }

        private static List<int> SplitCount(int totalCount, int speciesCount)
        {
            int minimum = MinimumAnimalsPerSpecies(totalCount);
            while (minimum > 1 && minimum * speciesCount > totalCount)
            {
                minimum--;
            }

            List<int> counts = Enumerable.Repeat(minimum, speciesCount).ToList();
            int remaining = totalCount - minimum * speciesCount;
            if (remaining <= 0)
            {
                return counts;
            }

            List<float> weights = new List<float>(speciesCount);
            float totalWeight = 0f;
            for (int i = 0; i < speciesCount; i++)
            {
                float weight = Rand.Range(0.7f, 1.5f);
                weights.Add(weight);
                totalWeight += weight;
            }

            int assigned = 0;
            for (int i = 0; i < speciesCount; i++)
            {
                int extra = Mathf.FloorToInt(remaining * (weights[i] / totalWeight));
                counts[i] += extra;
                assigned += extra;
            }

            while (assigned < remaining)
            {
                counts[Rand.Range(0, counts.Count)]++;
                assigned++;
            }

            counts.Shuffle();
            return counts;
        }

        private static int MinimumAnimalsPerSpecies(int totalCount)
        {
            if (totalCount >= 50)
            {
                return 4;
            }
            if (totalCount >= 25)
            {
                return 3;
            }
            return 2;
        }
    }
}
