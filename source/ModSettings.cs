using Verse;

namespace Mixed_Manhunter_Attacks
{
    public class ModSettings : Verse.ModSettings
    {
        public static float MixedAttackChance = 100f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref MixedAttackChance, "MixedAttackChance", 100f);
            MixedAttackChance = UnityEngine.Mathf.Clamp(MixedAttackChance, 0f, 100f);
        }
    }
}
