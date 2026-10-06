using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMunin
{
    /// <summary>
    /// The Gift of Munin potion: brewed like a mead, from a mead base cooked at the Cauldron and fermented. Drinking it
    /// gives <see cref="GiftOfMuninEffect"/>, which makes every material known at once, and with them every recipe they
    /// unlock. That skips what linear progression is about, so the potion is <see cref="IFullModeOnly"/>: only in full
    /// mode, unless the [Tiers] config gives it a tier.
    /// </summary>
    public class GiftOfMunin : PotionBase, IFullModeOnly
    {
        /// <summary>
        /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game
        /// sees the result only once Add has run.
        /// </summary>
        /// <param name="instance">Jotunn's item manager, which the item is added to.</param>
        public GiftOfMunin(ItemManager instance) : base(instance) { }

        /// <inheritdoc/>
        protected override string BaseName => "GiftOfMunin";

        /// <inheritdoc/>
        protected override string FullName => "Gift of Munin";

        /// <inheritdoc/>
        protected override string Description => "Grants you all the knowledge";

        /// <inheritdoc/>
        protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfMunin.png";

        /// <inheritdoc/>
        protected override RequirementConfig[] MeadBaseRequirements => new[]
        {
            new RequirementConfig { Item = "NeckTail", Amount = 20, Recover = false },
            new RequirementConfig { Item = "Raspberry", Amount = 20, Recover = false },
            new RequirementConfig { Item = "Eitr", Amount = 1, Recover = false }
        };

        /// <inheritdoc/>
        public override bool Enabled => true;

        /// <inheritdoc/>
        public override ProgressionTier DefaultTier => ProgressionTier.Mistlands;

        /// <inheritdoc/>
        protected override SE_Stats CreateEffect()
        {
            var effect = ScriptableObject.CreateInstance<GiftOfMuninEffect>();
            effect.Initialize(FullName);
            effect.SetIcon(IconPath);
            return effect;
        }
    }

}
