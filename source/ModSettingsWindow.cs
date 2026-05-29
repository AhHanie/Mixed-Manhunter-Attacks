using UnityEngine;
using Verse;

namespace Mixed_Manhunter_Attacks
{
    public static class ModSettingsWindow
    {
        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);

            string percent = ModSettings.MixedAttackChance.ToString("F0");
            string label = "MixedHunterAttacks.MixedAttackChanceLabel".Translate(percent).ToString();
            string tooltip = "MixedHunterAttacks.MixedAttackChanceTooltip".Translate().ToString();
            ModSettings.MixedAttackChance = listing.SliderLabeled(label, ModSettings.MixedAttackChance, 0f, 100f, 0.65f, tooltip);
            ModSettings.MixedAttackChance = Mathf.Round(ModSettings.MixedAttackChance);

            listing.Gap(4f);
            listing.Label("MixedHunterAttacks.MixedAttackChanceDescription".Translate());
            listing.Gap(12f);

            listing.CheckboxLabeled(
                "MixedHunterAttacks.AllowQuestOverridesLabel".Translate().ToString(),
                ref ModSettings.AllowQuestOverrides,
                "MixedHunterAttacks.AllowQuestOverridesTooltip".Translate().ToString());

            listing.End();
        }
    }
}
