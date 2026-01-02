using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMunin
{
    public class GiftOfMunin : PotionBase
    {
        public GiftOfMunin(ItemManager instance) : base(instance) { }

        /// <summary>
        /// The base name of the potion.
        /// </summary>
        protected override string BaseName => "GiftOfMunin";

        /// <summary>
        /// The full name of the potion.
        /// </summary>
        protected override string FullName => "Gift of Munin";

        /// <summary>
        /// The description of the potion.
        /// </summary>
        protected override string Description => "Grants you all the knowledge";

        /// <summary>
        /// The path to the icon of the potion.
        /// </summary>
        protected override string IconPath => "BrudvikWhiteHilt.Assets.GiftOfMunin.png";

        /// <summary>
        /// The requirements for crafting the potion.
        /// </summary>
        protected override RequirementConfig[] MeadBaseRequirements => new[]
        {
            new RequirementConfig { Item = "NeckTail", Amount = 20, Recover = false },
            new RequirementConfig { Item = "Raspberry", Amount = 20, Recover = false },
            new RequirementConfig { Item = "Eitr", Amount = 1, Recover = false }
        };

        /// <summary>
        /// Indicates whether the potion is enabled.
        /// </summary>
        public override bool Enabled => true;

        /// <summary>
        /// Creates the effect for the potion.
        /// </summary>
        /// <returns></returns>
        protected override SE_Stats CreateEffect()
        {
            var effect = ScriptableObject.CreateInstance<GiftOfMuninEffect>();
            effect.Initialize(FullName);
            effect.SetIcon(IconPath);
            return effect;
        }
    }

}
