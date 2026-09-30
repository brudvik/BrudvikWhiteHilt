using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Sits on a Kraken tentacle. It dies with its Kraken, and when the Kraken sinks back into the deep.
/// </summary>
public class KrakenTentacle : MonoBehaviour
{
    /// <summary>ZDO key of the Kraken the tentacle belongs to.</summary>
    public const string BodyKey = "whitehilt_kraken";

    // A tentacle without a Kraken (e.g. spawned by hand) lets go after this long.
    private const float LoneSeconds = 120f;

    private ZNetView nview;
    private Character character;
    private float nextTick;
    private float born;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        born = Time.time;
        nextTick = Time.time + 2f;
    }

    private void Update()
    {
        if (Time.time < nextTick || !nview.IsValid() || !nview.IsOwner() || character.IsDead())
        {
            return;
        }

        nextTick = Time.time + 1f;
        ZDOID body = nview.GetZDO().GetZDOID(BodyKey);
        ZDO bodyZdo = body.IsNone() ? null : ZDOMan.instance.GetZDO(body);
        bool gone = body.IsNone() ? Time.time - born > LoneSeconds : bodyZdo == null || bodyZdo.GetLong(KrakenBody.RetreatKey) != 0L;
        if (gone)
        {
            character.ApplyDamage(new HitData { m_damage = { m_damage = 99999f }, m_point = transform.position }, showDamageText: false, triggerEffects: true);
        }
    }
}
