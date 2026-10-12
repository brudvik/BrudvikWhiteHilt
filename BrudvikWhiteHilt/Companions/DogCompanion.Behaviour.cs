using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public sealed partial class DogCompanion
{
    // Once a game day for each kind of trouble, wherever the owner is.
    private void WarnOwner(float hunger, float day)
    {
        string key = hunger >= (DogSettings.StarveDays.Value - 3f) * day ? "whitehilt_dog_warn_starving"
            : Zdo.GetFloat(neglectKey) >= day ? "whitehilt_dog_warn_homeless"
            : hunger >= HungryWarningDays * day ? "whitehilt_dog_warn_hungry"
            : null;
        int today = EnvMan.instance.GetDay();
        if (key == null || Zdo.GetInt(warnedKey, -1) == today)
        {
            return;
        }

        Zdo.Set(warnedKey, today);
        DogRegistry.SendNotice(OwnerId, key, DogName, transform.position, DogRegistry.Anywhere);
    }

    private void Whine(float hunger, float day)
    {
        if (Time.time < nextWhine || (hunger < day && character.GetHealthPercentage() > 0.3f && !care.IsMiserable))
        {
            return;
        }

        nextWhine = Time.time + WhineInterval;
        PlaySound("dogwhine");
    }

    // Barks and warns the owner when an enemy comes near home, at most once in a while.
    private void Guard(Vector3 home)
    {
        if (Time.time < nextGuard)
        {
            return;
        }

        bool intruder = Character.GetAllCharacters().Any(other => other != character && !other.IsDead() && !other.IsPlayer() && !other.IsTamed()
            && BaseAI.IsEnemy(character, other) && Vector3.Distance(other.transform.position, home) <= GuardRadius);
        if (!intruder)
        {
            return;
        }

        nextGuard = Time.time + GuardCooldown;
        Bark();
        DogRegistry.SendNotice(OwnerId, "whitehilt_dog_guard", DogName, home, GuardNoticeRange);
    }

    // A grown dog that follows sits down when its master has stood still for a moment, and gets up when they walk on.
    // On a ship it boards with its master and sits on deck until they go ashore.
    private void UpdateSitting()
    {
        GameObject master = ai.GetFollowTarget();
        if (master == null || IsPuppy || activities.IsBusy)
        {
            masterStill = 0f;
            return;
        }

        Ship masterShip = FindShipUnder(master.transform.position);
        Ship ownShip = character.GetStandingOnShip();
        if (masterShip != null)
        {
            if (ownShip != masterShip && masterShip.GetSpeed() < BoardMaxShipSpeed
                && Vector3.Distance(transform.position, master.transform.position) <= BoardRange)
            {
                TeleportTo(master.transform.position - master.transform.forward * 1.2f + Vector3.up * 0.3f);
            }

            if (rest.Current != RestPose.Pose.Sit)
            {
                rest.SetPose(RestPose.Pose.Sit);
            }

            return;
        }

        if (ownShip != null && rest.Current == RestPose.Pose.Sit)
        {
            rest.SetPose(RestPose.Pose.None);
            return;
        }

        Vector3 position = master.transform.position;
        if ((position - masterPosition).sqrMagnitude > 0.04f)
        {
            masterPosition = position;
            masterStill = 0f;
        }
        else
        {
            masterStill += SitCheckSeconds;
        }

        float distance = Vector3.Distance(transform.position, position);
        RestPose.Pose pose = rest.Current;
        if (pose == RestPose.Pose.Sit && (masterStill == 0f || distance > SitRange + 1f))
        {
            rest.SetPose(RestPose.Pose.None);
        }
        else if (pose == RestPose.Pose.None && masterStill >= SitAfterSeconds && distance <= SitRange && character.GetVelocity().magnitude < 0.3f)
        {
            rest.SetPose(RestPose.Pose.Sit);
        }
    }

    // A dog house placed near home becomes the new home.
    private Vector3 UpdateHome(ZDO zdo)
    {
        Vector3 home = zdo.GetVec3(homeKey, transform.position);
        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, home, HomeRadius);
        if (house != null && house.transform.position != home)
        {
            home = house.transform.position;
            zdo.Set(homeKey, home);
        }

        return home;
    }

    // Picks where the dog rests at home: beside its master's bed at night, in its own bed under a roof, or in its house
    // at night and in rain.
    private void UpdateRestGoal(bool atHome, Vector3 home, DogHomePiece house, DogHomePiece bed)
    {
        RestSpot spot = null;
        RestPose.Pose pose = RestPose.Pose.None;
        if (atHome && character.GetHealth() > 0f)
        {
            bool night = EnvMan.IsNight();
            Player master = Player.GetPlayer(OwnerId);

            // Only a bed within the home area, or the dog would stop counting as home once it lay down there.
            if (night && master != null && master.InBed() && Vector3.Distance(master.transform.position, home) <= HomeRadius)
            {
                spot = RestSpot.Beside(master);
                pose = RestPose.Pose.Sleep;
            }
            else if (night && bed != null)
            {
                spot = RestSpot.In(bed);
                pose = RestPose.Pose.Sleep;
            }
            else if ((night || EnvMan.IsWet()) && house != null)
            {
                spot = RestSpot.In(house);
                pose = night ? RestPose.Pose.Sleep : RestPose.Pose.Lie;
            }
        }

        if ((spot?.Key == goal?.Key) && pose == goalPose)
        {
            return;
        }

        // Only get up from a rest this care started; the test command's rest is left alone.
        if (rest.IsResting && Zdo.GetBool(goalRestKey))
        {
            rest.SetPose(RestPose.Pose.None);
        }

        Zdo.Set(goalRestKey, false);
        goal = spot;
        goalPose = pose;
        goalWalkTime = 0f;
    }

    // Feeds the dog. Bone broth does more: fed for two days, half its health back, no poison and faster growth for a
    // day.
    private void OnAte(ItemDrop food)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        Happy();
        if (food == null || Utils.GetPrefabName(food.gameObject) != DogRegistry.DogFoodPrefabName)
        {
            Zdo.Set(hungerKey, 0f);
            return;
        }

        // Bone broth: fed for two days, half the health back, no poison and double growth for a day.
        float day = DaySeconds;
        Zdo.Set(hungerKey, -day);
        Zdo.Set(boostKey, ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(day).Ticks);
        character.Heal(character.GetMaxHealth() * 0.5f);
        care.CurePoison();
    }

    private static Ship FindShipUnder(Vector3 position)
    {
        return Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 3f, shipLayers)
            ? hit.collider.GetComponentInParent<Ship>()
            : null;
    }

    // Where the dog lies down: in its bed or house, or on the floor beside its sleeping master.
    private sealed class RestSpot
    {
        private const float BesideDistance = 1.3f;

        private static readonly int floorLayers = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");

        public Object Key { get; private set; }

        public Vector3 Approach { get; private set; }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; }

        public static RestSpot In(DogHomePiece piece)
        {
            return new RestSpot { Key = piece, Approach = piece.ApproachPosition, Position = piece.RestPosition, Rotation = piece.RestRotation };
        }

        public static RestSpot Beside(Player master)
        {
            Vector3 origin = master.transform.position;
            Vector3 side = master.transform.right * BesideDistance;
            Vector3 floor = FindFloor(origin + side) ?? FindFloor(origin - side) ?? origin;
            return new RestSpot { Key = master, Approach = floor, Position = floor, Rotation = master.transform.rotation };
        }

        private static Vector3? FindFloor(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 1.2f, Vector3.down, out RaycastHit hit, 3f, floorLayers)
                && hit.collider.GetComponentInParent<Bed>() == null)
            {
                return hit.point;
            }

            return null;
        }
    }
}
