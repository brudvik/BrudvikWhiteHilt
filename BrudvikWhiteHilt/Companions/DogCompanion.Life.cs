using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public sealed partial class DogCompanion
{
    // Once a night, with another player's dog nearby. The dog of the lower player ID decides, so a pair is only tried once.
    private void TryLitter(Vector3 home, bool fed)
    {
        int today = EnvMan.instance.GetDay();
        if (!DogSettings.Litters.Value || !EnvMan.IsNight() || !fed || !CanHaveLitter || Zdo.GetInt(litterTryKey, -1) == today
            || today - Zdo.GetInt(litterDayKey, -LitterCooldownDays) < LitterCooldownDays)
        {
            return;
        }

        long ownerId = OwnerId;
        DogCompanion partner = dogs.FirstOrDefault(other => other != null && other != this && other.OwnerId > ownerId && other.CanHaveLitter
            && Vector3.Distance(other.transform.position, transform.position) <= LitterRange);
        if (partner == null)
        {
            return;
        }

        Zdo.Set(litterTryKey, today);
        if (Random.value >= LitterChance)
        {
            return;
        }

        Zdo.Set(litterDayKey, today);
        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, home, HomeRadius);
        Vector3 nest = house != null ? house.ApproachPosition : transform.position;
        int color = Random.value < 0.5f ? Zdo.GetInt(colorKey) : partner.Zdo.GetInt(colorKey);
        DogRegistry.DropItem(DogRegistry.GetPuppyPrefab(color), 1, nest);
        string names = DogName + " & " + partner.DogName;
        DogRegistry.SendNotice(ownerId, "whitehilt_dog_litter", names, nest, DogRegistry.Anywhere);
        DogRegistry.SendNotice(partner.OwnerId, "whitehilt_dog_litter", names, nest, DogRegistry.Anywhere);
        Happy();
        partner.Happy();
    }

    private void Starve()
    {
        Zdo.Set(hungerKey, float.MaxValue);
        Die();
    }

    private void Die()
    {
        HitData hit = new() { m_point = transform.position, m_dir = Vector3.down };
        hit.m_damage.m_damage = 1E+07f;
        character.Damage(hit);
    }

    // Tells the owner once that the dog has grown old, and ends its life when its time has come. True when it died.
    private bool UpdateOldAge(bool atHome)
    {
        float life = LifeDays;
        if (life <= 0f || !IsOld)
        {
            return false;
        }

        if (!Zdo.GetBool(oldNoticeKey))
        {
            Zdo.Set(oldNoticeKey, true);
            DogRegistry.SendNotice(OwnerId, "whitehilt_dog_warn_old", DogName, transform.position, DogRegistry.Anywhere);
        }

        float lived = DaysLived;
        if (lived < life || (lived < life + OldAgeGraceDays && !(atHome && rest.IsResting)))
        {
            return false;
        }

        Zdo.Set(oldAgeKey, true);
        Die();
        return true;
    }

    private void RunAway()
    {
        DogRegistry.SendGone(OwnerId, "whitehilt_dog_ranaway", DogName);
        ZNetScene.instance.Destroy(gameObject);
    }

    // When the dog dies: drops its remains with its name and age for a gravestone, and its collar and coat. A dog that
    // dies of old age leaves a puppy in its house.
    private void OnDeath()
    {
        if (!nview.IsOwner())
        {
            return;
        }

        string dogName = DogName;
        bool starved = Zdo.GetFloat(hungerKey) >= DogSettings.StarveDays.Value * DaySeconds;
        ItemDrop remains = PrefabManager.Instance.GetPrefab(DogRegistry.RemainsPrefabName)?.GetComponent<ItemDrop>();
        if (remains != null)
        {
            // Days in the world since it was let out, for the gravestone.
            long now = ZNet.instance.GetTime().Ticks;
            double lived = System.TimeSpan.FromTicks(now - Zdo.GetLong(bornKey, now)).TotalSeconds / DaySeconds;
            ItemDrop.ItemData data = remains.m_itemData.Clone();
            data.m_dropPrefab = remains.gameObject;
            data.m_stack = 1;
            DogRegistry.SetInscription(data, dogName, Mathf.Max(0, (int)lived));
            ItemDrop.DropItem(data, 1, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // The collar and coat are not lost with the dog.
        string collar = DogRegistry.GetCollarPrefab(Zdo.GetInt(collarKey));
        GameObject collarPrefab = collar != null ? PrefabManager.Instance.GetPrefab(collar) : null;
        if (collarPrefab != null)
        {
            Instantiate(collarPrefab, transform.position + Vector3.up * 0.7f, Quaternion.identity);
        }

        if (care.HasCoat)
        {
            DogRegistry.DropItem(DogRegistry.CoatPrefabName, 1, transform.position + Vector3.up * 0.4f);
        }

        bool oldAge = Zdo.GetBool(oldAgeKey);
        DogRegistry.SendGone(OwnerId, starved ? "whitehilt_dog_starved" : oldAge ? "whitehilt_dog_old_age" : "whitehilt_dog_died", dogName);

        // A dog that lived out its life leaves a puppy of its own colour in the dog house.
        if (oldAge)
        {
            DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, Home, HomeRadius);
            Vector3 nest = house != null ? house.RestPosition : transform.position;
            DogRegistry.DropItem(DogRegistry.GetPuppyPrefab(Zdo.GetInt(colorKey)), 1, nest);
            DogRegistry.SendNotice(OwnerId, "whitehilt_dog_legacy", dogName, nest, DogRegistry.Anywhere);
        }
    }
}
