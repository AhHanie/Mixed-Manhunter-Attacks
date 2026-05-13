using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Mixed_Manhunter_Attacks
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "MixedHunterAttacks.LoadingLabel", doAsynchronously: true, null);
        }
        private void Init()
        {
            GetSettings<ModSettings>();
            new Harmony("rimworld.sk.mixedhunterattacks").PatchAll();
        }
        public override string SettingsCategory()
        {
            return "MixedHunterAttacks.SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModSettingsWindow.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
