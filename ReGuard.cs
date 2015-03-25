using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using ReBot.API;
using ReBot;
using Newtonsoft.Json.Converters;
using Geometry;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Rebot
{
    [Rotation("ReGuard", "", "", WoWClass.Druid, Specialization.DruidGuardian, 10, 30)]
    public class ReGuard : CombatRotation
    {
        [JsonProperty("OoC Heal")]
        public bool HSOC = true;
        [JsonProperty("OoC Travel")]
        public bool ATF = true;
        [JsonProperty("Interrupt")]
        public bool AI = true;
        [JsonProperty("Soothe")]
        public bool AS = true;
        [JsonProperty("Wild Charge")]
        public bool AWC = true;
        [JsonProperty("AoE Rotation")]
        public bool UAE = true;
        [JsonProperty("Lacerate in AoE")]
        public bool ULAE = true;
        [JsonProperty("Iron Trap")]
        public bool Trapper = true;
        [JsonProperty("Berserk")]
        public bool BK = true;
        [JsonProperty("Berserk no. of adds")]
        public double BKA = 3;
        [JsonProperty("Dream of Cenarious %HP")]
        public double DCHeal = 0.85;
        [JsonProperty("Barkskin %HP")]
        public double BS = 0;
        [JsonProperty("Cenarion Ward %HP")]
        public double CWHeal = 0.85;
        [JsonProperty("Survival Instincts %HP")]
        public double SIL = 0;
        [JsonProperty("Frenzied Regeneration %HP")]
        public double FRHeal = 0.85;
        [JsonProperty("Savage Defense %HP")]
        public double SD = 0;
        [JsonProperty("Use Potion on Pull")]
        public bool usePrepot = false;
        public double BeerTimer 
        { 
            get 
            { 
                return API.ExecuteLua<double>("return BeerTimer;"); 
            } 
        }
        public ReGuard()
        {
            BeerTimersInit();
        }
        public void BeerTimersInit()
        {
            if (API.ExecuteLua<int>("return BeerTimerInit;") != 1)
                API.ExecuteLua("local f = CreateFrame(\"Frame\");" +
                    "BeerTimer = 0;" +
                    "BeerTimerInit = 1;" +
                    "f:RegisterEvent(\"CHAT_MSG_ADDON\");" +
                    "f:SetScript(\"OnEvent\", function(self, event, prefix, msg, channel, sender) if prefix == \"D4\" then local dbmPrefix, arg1, arg2, arg3, arg4 = strsplit(\"\t\", msg); if dbmPrefix == \"PT\" then BeerTimer = arg1 end end end);" +
                    "f:SetScript(\"OnUpdate\", function(self, e) BeerTimer = BeerTimer - e; if BeerTimer < 0 then BeerTimer = 0 end end);");
        }
        public override bool OutOfCombat()
        {
            if (HSOC)
            {
                if (CastSelf("Rejuvenation", () => Me.HealthFraction <= 0.45 && !HasAura("Rejuvenation"))) return true;
                if (CastSelfPreventDouble("Healing Touch", () => Me.HealthFraction <= 0.3)) return true;
            }
            if (CastSelf("Mark of the Wild", () => !HasAura("Mark of the Wild") && !HasAura("Blessing of Kings"))) return true;
            if (ATF)
            {
                if (CastSelf("Travel Form", () => Me.IsSwimming && Me.MovementSpeed != 0 && !HasAura("Travel Form") || Me.IsOutdoors && Me.MovementSpeed != 0 && !HasAura("Travel Form") && Me.DistanceTo(API.GetNaviTarget()) > 5)) return true;
                if (CastSelf("Cat Form", () => !Me.IsOutdoors && Me.MovementSpeed != 0 && !HasAura("Cat Form") && Me.DistanceTo(API.GetNaviTarget()) > 5)) return true;
            }
            if (usePrepot && BeerTimer < 3 && BeerTimer != 0) 
            { 
                if (API.ItemCount(109217) > 0) API.UseItem(109217); 
            }
            return false;
        }
        public override void Combat()
        {
            CastSelfPreventDouble("Bear Form", () => !IsInShapeshiftForm("Bear Form"));
            if (API.HasItem(115010) && Target.HealthFraction < 0.5 && Trapper)
            {
                API.ExecuteMacro("/use Deadly Iron Trap");
            }
            else if (API.HasItem(115009) && Target.HealthFraction < 0.5 && Trapper)
            {
                API.ExecuteMacro("/use Improved Iron Trap");
            }
            else if (API.HasItem(113991) && Target.HealthFraction < 0.5 && Trapper)
            {
                API.ExecuteMacro("/use Iron Trap");
            }
            if (HasAura("Bear Form"))
            {
                if (AI)
                {
                    if (!Cast("Skull Bash", () => Target.IsCastingAndInterruptible()))
                        Cast("Mighty Bash", () => Target.IsCasting);
                }
                if (CastSelf("Frenzied Regeneration", () => Me.GetPower(WoWPowerType.Rage) >= 30 && Me.HealthFraction <= FRHeal)) return;
                if (CastSelfPreventDouble("Survival Instincts", () => !HasAura("Survival Instincts") && Me.HealthFraction <= SIL)) return;
                if (CastSelf("Barkskin", () => Me.HealthFraction < BS)) return;
                if (CastSelf("Savage Defense", () => !HasAura("Savage Defense") && Me.GetPower(WoWPowerType.Rage) >= 60 && Me.HealthFraction <= SD)) return;
                if (CastSelf("Cenarion Ward", () => !HasAura("Cenarion Ward") && Me.HealthFraction <= CWHeal)) return;
                if (CastSelf("Healing Touch", () => HasAura("Dream of Cenarius") && Me.HealthFraction <= DCHeal)) return;
                //if (CastSelf("Remove Corruption", () => Me.Auras.Any(x => x.IsDebuff && "Curse,Poison".Contains(x.DebuffType)))) return;
                if (CastSelf("Berserk", () => Adds.Count(x => x.DistanceSquared <= 12 * 12) >= BKA && BK && Target.IsElite())) return;
                if (CastPreventDouble("Soothe", () => Target.Auras.Any(x => x.IsStealable) && AS, 9000)) return;
                if (HasSpell("Wild Charge"))
                {
                    if (Cast("Wild Charge", () => AWC && Me.InCombat && Target.Distance >= 15)) return;
                }
                if (Target.IsInCombatRangeAndLoS)
                {
                    if (Me.HasAura("Berserk"))
                    {
                        if (Cast("Mangle")) return;
                    }
                    else
                    {
                        if (Adds.Count(x => x.DistanceSquared <= 12 * 12) >= 2 && UAE)
                        {
                            if (Cast(80313, () => Target.HasAura("Lacerate", true, 3))) return; // Pulverize
                            if (Cast("Mangle")) return;
                            if (Cast(77758)) return; // Thrash
                            if (Cast("Maul", () => (HasAura("Tooth and Claw", true, 2) || HasAura("Tooth and Claw") && !Target.HasAura("Tooth and Claw")) && Me.GetPower(WoWPowerType.Rage) >= 20)) return;
                            if (Cast("Lacerate", () => !Target.HasAura("Lacerate", true, 3) && ULAE)) return;
                        }
                        else
                        {
                            if (Cast(80313, () => Target.HasAura("Lacerate", true, 3))) return; // Pulverize
                            if (Cast("Mangle")) return;
                            if (Cast("Lacerate", () => !Target.HasAura("Lacerate", true, 3))) return;
                            if (Cast(77758, () => (!Target.HasAura("Thrash") || Target.AuraTimeRemaining("Thrash") <= 4))) return; // Thrash
                            if (Cast("Maul", () => (HasAura("Tooth and Claw", true, 2) || HasAura("Tooth and Claw") && !Target.HasAura("Tooth and Claw") || AuraTimeRemaining("Tooth and Claw") <= 1) && Me.GetPower(WoWPowerType.Rage) >= 20)) return;
                        }
                    }
                }
            }
        }
    }
}
