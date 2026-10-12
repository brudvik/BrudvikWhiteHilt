using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public sealed partial class DogCompanion
{
    private int GreyStep
    {
        get
        {
            float life = LifeDays > 0f ? LifeDays : DefaultLifeDays;
            float greying = (DaysLived / life - GreyStartLife) / (GreyFullLife - GreyStartLife);
            return Mathf.Clamp(Mathf.CeilToInt(greying * DogRegistry.GreySteps), 0, DogRegistry.GreySteps);
        }
    }

    // Shows the collar from the network data on the neck bone, made again only when it changes.
    private void ApplyCollar()
    {
        int collar = Zdo.GetInt(collarKey);
        if (collar == shownCollar || VisualHelper.IsHeadless || neck == null)
        {
            return;
        }

        shownCollar = collar;
        if (collarModel != null)
        {
            Destroy(collarModel);
        }

        if (collar <= 0 || collar >= collarScales.Length || bodies.Length == 0)
        {
            return;
        }

        try
        {
            DogRegistry.GetCollarModel(collar, out Mesh mesh, out Texture2D texture);
            collarModel = VisualHelper.CreateModel(neck, mesh, texture, bodies[0], collarPosition, collarRotation, 1f);
            collarModel.transform.localScale = collarScales[collar];
        }
        catch (System.Exception ex)
        {
            Jotunn.Logger.LogWarning($"Dog: no collar model: {ex.Message}");
        }
    }

    // Shows the coat colour, its greying with age and a worn coat, as a material made from the original one; checked
    // now and then and only changed when one of them did.
    private void ApplyColor()
    {
        if (VisualHelper.IsHeadless || Time.time < nextColorCheck)
        {
            return;
        }

        nextColorCheck = Time.time + ColorCheckSeconds;
        int color = Zdo.GetInt(colorKey);
        int grey = GreyStep;
        int coat = care != null && care.HasCoat ? 1 : 0;
        if (color == shownColor && grey == shownGrey && coat == shownCoat)
        {
            return;
        }

        shownColor = color;
        shownGrey = grey;
        shownCoat = coat;
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] != null && originalMaterials[i] != null && originalMaterials[i].mainTexture != null)
            {
                bodies[i].sharedMaterial = DogRegistry.GetCoatMaterial(color, grey, coat == 1, originalMaterials[i]);
            }
        }
    }

    // Size follows age on every client. A puppy cannot be commanded and does not look for fights.
    private void ApplyStage()
    {
        float grown = Grown;
        float scale = Mathf.Lerp(PuppyScale, AdultScale, grown);
        if (!Mathf.Approximately(scale, shownScale))
        {
            shownScale = scale;
            transform.localScale = Vector3.one * scale;
        }

        bool puppy = grown < 1f;
        tameable.m_commandable = !puppy;
        ai.m_viewRange = puppy ? 0f : viewRange;
        ai.m_hearRange = puppy ? 0f : hearRange;
        ai.m_alertRange = puppy ? 0f : alertRange;
    }
}
