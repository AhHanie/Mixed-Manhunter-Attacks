using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Mixed_Manhunter_Attacks.Patches
{
    [HarmonyPatch(typeof(IncidentWorker_AggressiveAnimals), "TryExecuteWorker")]
    internal static class IncidentWorker_AggressiveAnimals_TryExecuteWorker_Patch
    {
        private const int AnimalsStayDurationMin = 60000;
        private const int AnimalsStayDurationMax = 120000;

        private static bool Prefix(IncidentWorker_AggressiveAnimals __instance, IncidentParms parms, ref bool __result)
        {
            if (__instance.def != IncidentDefOf.ManhunterPack || ModSettings.MixedAttackChance <= 0f || !Rand.Chance(ModSettings.MixedAttackChance / 100f))
            {
                return true;
            }

            if (IsQuestIncident(parms) && !ModSettings.AllowQuestOverrides)
            {
                return true;
            }

            Map map = parms.target as Map;
            if (map == null)
            {
                return true;
            }

            if (!TryExecuteMixed(__instance, parms, map, out bool result))
            {
                return true;
            }

            __result = result;
            return false;
        }

        private static bool IsQuestIncident(IncidentParms parms)
        {
            return parms.quest != null || !parms.questTag.NullOrEmpty();
        }

        private static bool TryExecuteMixed(IncidentWorker_AggressiveAnimals worker, IncidentParms parms, Map map, out bool result)
        {
            result = false;

            if (!MixedManhunterPackUtility.TryGenerateAnimals(map, parms.points, parms.pawnCount, out List<Pawn> pawns, out PawnKindDef primaryKind))
            {
                return false;
            }

            IntVec3 entryCell = parms.spawnCenter;
            if (!entryCell.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out entryCell, map, CellFinder.EdgeRoadChance_Animal))
            {
                return true;
            }

            Rot4 rot = Rot4.FromAngleFlat((map.Center - entryCell).AngleFlat);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(entryCell, map, 10);
                QuestUtility.AddQuestTag(GenSpawn.Spawn(pawn, loc, map, rot), parms.questTag);
                pawn.health.AddHediff(HediffDefOf.Scaria);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(AnimalsStayDurationMin, AnimalsStayDurationMax);
            }

            IncidentWorker.SendIncidentLetter(
                "LetterLabelManhunterPackArrived".Translate(),
                "MixedHunterAttacks.ManhunterPackArrivedMixed".Translate(primaryKind.GetLabelPlural()),
                LetterDefOf.ThreatBig,
                parms,
                pawns[0],
                worker.def);

            Find.TickManager.slower.SignalForceNormalSpeedShort();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Important);
            result = true;
            return true;
        }
    }
}
