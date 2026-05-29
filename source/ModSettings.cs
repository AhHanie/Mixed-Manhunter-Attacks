using Verse;

namespace Mixed_Manhunter_Attacks
{
    public class ModSettings : Verse.ModSettings
    {
        public static float MixedAttackChance = 100f;
        public static bool AllowQuestOverrides = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref MixedAttackChance, "MixedAttackChance", 100f);
            Scribe_Values.Look(ref AllowQuestOverrides, "AllowQuestOverrides", false);
            MixedAttackChance = UnityEngine.Mathf.Clamp(MixedAttackChance, 0f, 100f);
        }
    }
}
