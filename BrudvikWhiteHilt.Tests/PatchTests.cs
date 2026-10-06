using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace BrudvikWhiteHilt.Tests;

/// <summary>
/// Checks that Harmony will apply every patch the mod declares.
/// </summary>
public class PatchTests
{
    /// <summary>
    /// Harmony's PatchAll skips a class without a [HarmonyPatch] of its own, so the patch methods in it never run and
    /// nothing says so. The chest handoff was lost this way for many versions.
    /// </summary>
    [Fact]
    public void EveryClassWithPatchMethodsIsAnnotated()
    {
        List<string> skipped = ModTypes()
            .Where(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Any(method => method.GetCustomAttributesData().Any(IsPatchAttribute)))
            .Where(type => !type.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
            .Select(type => type.FullName)
            .ToList();

        Assert.True(skipped.Count == 0, "Classes Harmony will skip: " + string.Join(", ", skipped));
    }

    private static bool IsPatchAttribute(CustomAttributeData attribute)
    {
        return attribute.AttributeType.FullName is "HarmonyLib.HarmonyPatch" or "HarmonyLib.HarmonyPrefix"
            or "HarmonyLib.HarmonyPostfix" or "HarmonyLib.HarmonyTranspiler" or "HarmonyLib.HarmonyFinalizer";
    }

    private static IEnumerable<Type> ModTypes()
    {
        Assembly mod = typeof(global::BrudvikWhiteHilt.BrudvikWhiteHilt).Assembly;
        try
        {
            return mod.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null);
        }
    }
}
