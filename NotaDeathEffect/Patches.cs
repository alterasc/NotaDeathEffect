using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using System.Linq;

namespace NotaDeathEffect;

[HarmonyPatch]
internal static class Patches
{

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    [HarmonyPostfix]
    public static void BlueprintChanges()
    {
        var constructType = Utils.GetBlueprintReference<BlueprintUnitFactReference>("fd389783027d63343b4a5634bd81645f");
        var undeadType = Utils.GetBlueprintReference<BlueprintUnitFactReference>("734a29b693e9ec346ba2951b27987e33");

        // Rogue
        {
            var masterStrikeBuff = Utils.GetBlueprint<BlueprintBuff>("eab680abdb0194343af169af393c2603");
            var comp = masterStrikeBuff.GetComponent<AddInitiatorAttackWithWeaponTrigger>();
            if (comp != null && comp.Action != null && comp.Action.Actions != null
                && comp.Action.Actions.Length > 0 && comp.Action.Actions[0] is Conditional cond)
            {
                cond.ConditionsChecker.Conditions =
                    cond.ConditionsChecker.Conditions.AppendToArray(new ContextConditionHasFact
                    {
                        Not = true,
                        m_Fact = constructType
                    });
            }

            var masterStrikeDamageBuff = Utils.GetBlueprint<BlueprintBuff>("b431dac34884ca64cadc45cc1efacaee");
            masterStrikeDamageBuff.RemoveComponents<SpellDescriptorComponent>();
        }

        // Ranger
        {
            var masterHunterAbility = Utils.GetBlueprint<BlueprintAbility>("8a57e1072da4f6f4faaa55b7b7dc633c");
            var variants = masterHunterAbility.GetComponent<AbilityVariants>();
            if (variants != null)
            {
                foreach (var variant in variants.m_Variants)
                {
                    var variantBP = variant.Get();
                    var cc = variantBP.GetComponents<AbilityTargetHasFact>()
                        .Where(x => x.Inverted)
                        .FirstOrDefault();
                    if (cc != null)
                    {
                        cc.m_CheckedFacts = cc.m_CheckedFacts.AppendToArray(constructType, undeadType);
                    }
                }
            }

            var masterHunterDamageBuff = Utils.GetBlueprint<BlueprintBuff>("af58cd45b9dc61e4fbfa9f46d4bffd00");
            masterHunterDamageBuff.RemoveComponents<SpellDescriptorComponent>();
        }

        // Slayer
        {
            var masterSlayerAbility = Utils.GetBlueprint<BlueprintAbility>("856bc8a19ae72b9498d87935bc0e28bb");
            var noFactComp = masterSlayerAbility.GetComponents<AbilityTargetHasFact>()
                .Where(x => x.Inverted).FirstOrDefault();
            if (noFactComp != null)
            {
                noFactComp.m_CheckedFacts = noFactComp.m_CheckedFacts.AppendToArray(constructType, undeadType);
            }
        }

        // Slayer Executioner and Monk Quivering Palm
        {
            var executionerAssassinateAbility = Utils.GetBlueprint<BlueprintAbility>("3dad7f131aa884f4c972f2fb759d0df4");
            var comp = executionerAssassinateAbility.GetComponent<AbilityTargetHasFact>();
            comp.m_CheckedFacts = comp.m_CheckedFacts.AppendToArray(undeadType, constructType);

            var kiQuiveringPalm = Utils.GetBlueprint<BlueprintAbility>("4de518e69f9b8094fb996b1599d00314");
            kiQuiveringPalm.RemoveComponents<SpellDescriptorComponent>();
            kiQuiveringPalm.AddComponent(comp);

            var scaledFistQuiveringPalm = Utils.GetBlueprint<BlueprintAbility>("749e77f7014cb4e4487400e508e70a59");
            scaledFistQuiveringPalm.RemoveComponents<SpellDescriptorComponent>();
            scaledFistQuiveringPalm.AddComponent(comp);
        }

        // Assassin
        {
            var assassinDeathAttackAbilityKill = Utils.GetBlueprint<BlueprintAbility>("ca5575accdf8ee64cb32608a77aaf989");
            assassinDeathAttackAbilityKill.RemoveComponents<SpellDescriptorComponent>();

            var assassinDeathAttackAbilityKillStandard = Utils.GetBlueprint<BlueprintAbility>("02d129b799da92d40b6377bac27d843f");
            assassinDeathAttackAbilityKillStandard.RemoveComponents<SpellDescriptorComponent>();

            var assassinDeathAttackAbilityKillBuffEffect = Utils.GetBlueprint<BlueprintBuff>("fa7cf97ea4dfc4d4aa600e18fd7d419b");

            var contextAction = assassinDeathAttackAbilityKillBuffEffect.GetComponent<AddFactContextActions>();
            var rootAction = contextAction.Activated.Actions[0];

            var ctions = (rootAction as ContextActionSavingThrow).Actions;
            var saveAction = ctions.Actions[0] as ContextActionConditionalSaved;

            var newRoot = new Conditional()
            {
                ConditionsChecker = new()
                {
                    Conditions = new[]
                    {
                        new ContextConditionHasFact()
                        {
                            m_Fact = undeadType
                        },
                        new ContextConditionHasFact()
                        {
                            m_Fact = constructType
                        },
                    },
                    Operation = Kingmaker.ElementsSystem.Operation.Or
                },
                IfTrue = saveAction.Succeed,
                IfFalse = new()
                {
                    Actions = contextAction.Activated.Actions
                }
            };
            contextAction.Activated.Actions = [newRoot];
        }

        // Colluding Scoundrel
        {
            var masterBackstabberAbility = Utils.GetBlueprint<BlueprintAbility>("e7b1fdd788579404e9eca606f3998110");
            var comp = masterBackstabberAbility.GetComponent<AbilityEffectRunAction>();
            if (comp.Actions.Actions.Length > 0 && comp.Actions.Actions[0] is Conditional cond)
            {
                cond.ConditionsChecker.Conditions = cond.ConditionsChecker.Conditions.AppendToArray(
                    new ContextConditionHasFact()
                    {
                        m_Fact = undeadType,
                        Not = true
                    },
                    new ContextConditionHasFact()
                    {
                        m_Fact = constructType,
                        Not = true
                    }
                );
            }
        }
    }
}
