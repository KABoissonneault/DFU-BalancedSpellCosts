using System;
using UnityEngine;

using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;

public class BalancedSpellCostsMod : MonoBehaviour
{
    #region Constants
    // The gold cost used for an effect that doesn't have any active components (Duration, Chance, Magnitude)
    public static int DefaultEffectGoldCost = 320;
    #endregion

    static Mod mod;

    [Invoke(StateManager.StateTypes.Start, 0)]
    public static void Init(InitParams initParams)
    {
        mod = initParams.Mod;
        new GameObject(mod.Title).AddComponent<BalancedSpellCostsMod>();
    }

    public void Awake()
    {
        Debug.Log("Begin mod init: Balanced Spell Costs");

        FormulaHelper.RegisterOverride<Func<IEntityEffect, EffectSettings, DaggerfallEntity, FormulaHelper.SpellCost>>(mod, "CalculateEffectCosts", CalculateEffectCosts);

        Debug.Log("Finished mod init: Balanced Spell Costs");
    }

    // Changes compared to Classic
    // 1) The average between minimum and maximum magnitude (both the base and scaling value) is not truncated. 1-2 is more expensive than 1-1
    // 2) The scaling cost is not truncated anymore. 1 per 2 levels is now higher than 0 per 2 levels
    private static FormulaHelper.SpellCost CalculateEffectCosts(IEntityEffect effect, EffectSettings settings, DaggerfallEntity caster)
    {
        if (caster == null)
            caster = GameManager.Instance.PlayerEntity;

        int skillValue = caster.Skills.GetLiveSkillValue((DFCareer.Skills)effect.Properties.MagicSkill);

        bool hasActiveComponents = effect.Properties.SupportDuration || effect.Properties.SupportChance || effect.Properties.SupportMagnitude;
        if(!hasActiveComponents)
        {
            return new FormulaHelper.SpellCost { goldCost = DefaultEffectGoldCost, spellPointCost = DefaultEffectGoldCost * (110 - skillValue) / 400 };
        }

        int durationGoldCost = effect.Properties.SupportDuration
            ? GetEffectComponentCosts(effect.Properties.DurationCosts, settings.DurationBase, (float)settings.DurationPlus / settings.DurationPerLevel)
            : 0;

        int chanceGoldCost = effect.Properties.SupportChance
            ? GetEffectComponentCosts(effect.Properties.ChanceCosts, settings.ChanceBase, (float)settings.ChancePlus / settings.ChancePerLevel)
            : 0;

        float magnitudeBase = (settings.MagnitudeBaseMax + settings.MagnitudeBaseMin) / 2.0f;
        float magnitudePlus = (settings.MagnitudePlusMax + settings.MagnitudePlusMin) / 2.0f;
        int magnitudeGoldCost = effect.Properties.SupportMagnitude
            ? GetEffectComponentCosts(effect.Properties.MagnitudeCosts, magnitudeBase, magnitudePlus / settings.MagnitudePerLevel)
            : 0;

        int totalGoldCost = durationGoldCost + chanceGoldCost + magnitudeGoldCost;
        int spCost = totalGoldCost * (110 - skillValue) / 400;

        return new FormulaHelper.SpellCost { goldCost = totalGoldCost, spellPointCost = spCost };
    }

    private static int GetEffectComponentCosts(EffectCosts costs, float starting, float increasePerLevel)
    {
        return (int)Math.Truncate(costs.OffsetGold + costs.CostA * starting + costs.CostB * increasePerLevel);
    }
}
