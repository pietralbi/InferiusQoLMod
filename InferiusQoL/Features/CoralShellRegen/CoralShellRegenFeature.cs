#nullable disable
namespace InferiusQoL.Features.CoralShellRegen;

using System.Reflection;
using HarmonyLib;
using InferiusQoL.Config;
using InferiusQoL.Logging;
using ProtoBuf;
using UWE;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class CoralShellRegenFeature
{
    private const int CoralShellPlateTechId = 3025;
    private const float SecondsPerGameDay = 1200f;

    private static readonly FieldInfo CoralKilledField = AccessTools.Field(typeof(CoralBlendWhite), "killed");
    private static readonly FieldInfo CoralDoneField = AccessTools.Field(typeof(CoralBlendWhite), "done");
    private static readonly FieldInfo CoralTimesDiedField = AccessTools.Field(typeof(CoralBlendWhite), "timesDied");
    private static readonly FieldInfo CoralUpdatingDeathFadeField = AccessTools.Field(typeof(CoralBlendWhite), "updatingDeathFade");
    private static readonly FieldInfo CoralTimeOfDeathField = AccessTools.Field(typeof(CoralBlendWhite), "timeOfDeath");

    public static void ApplyRuntime()
    {
        foreach (var data in Object.FindObjectsOfType<CoralShellRegenData>())
        {
            data.ApplyProgress();
        }
    }

    public static CoralShellRegenData Ensure(LiveMixin liveMixin)
    {
        if ((Object)(object)liveMixin == (Object)null || !IsCoralShellPlate(liveMixin.gameObject))
        {
            return null;
        }

        var data = liveMixin.GetComponent<CoralShellRegenData>();
        if ((Object)(object)data == (Object)null)
        {
            data = liveMixin.gameObject.AddComponent<CoralShellRegenData>();
        }

        data.Bind(liveMixin);
        data.StartIfDamaged(liveMixin);
        return data;
    }

    public static bool IsCoralShellPlate(GameObject gameObject)
    {
        if ((Object)(object)gameObject == (Object)null)
        {
            return false;
        }

        var techType = CraftData.GetTechType(gameObject);
        if ((int)techType == CoralShellPlateTechId)
        {
            return true;
        }

        var techTag = gameObject.GetComponent<TechTag>();
        return (Object)(object)techTag != (Object)null && (int)techTag.type == CoralShellPlateTechId;
    }

    public static float GetRegrowthDurationSeconds()
    {
        return Mathf.Clamp(InferiusConfig.Instance.CoralShellPlateRegrowthDays, 1, 10) * SecondsPerGameDay;
    }

    public static bool TryGetTime(out float time)
    {
        if ((Object)(object)DayNightCycle.main == (Object)null)
        {
            time = 0f;
            return false;
        }

        time = DayNightCycle.main.timePassedAsFloat;
        return true;
    }

    public static void ResetBleachState(CoralBlendWhite coral)
    {
        if ((Object)(object)coral == (Object)null)
        {
            return;
        }

        CoralKilledField?.SetValue(coral, false);
        CoralDoneField?.SetValue(coral, false);
        CoralTimesDiedField?.SetValue(coral, 0);
        CoralUpdatingDeathFadeField?.SetValue(coral, false);
        CoralTimeOfDeathField?.SetValue(coral, 0f);
        BehaviourUpdateUtils.Deregister(coral);

        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetFloat(ShaderPropertyID._Brightness, 0f);
        propertyBlock.SetFloat(ShaderPropertyID._Gray, 0f);

        var renderers = coral.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    public static bool BlocksHarvest(GameObject target)
    {
        var liveMixin = ResolveCoralLiveMixin(target);
        var data = Ensure(liveMixin);
        return (Object)(object)data != (Object)null && data.BlocksHarvest;
    }

    public static bool BlocksDamage(LiveMixin liveMixin)
    {
        var data = Ensure(liveMixin);
        return (Object)(object)data != (Object)null && data.BlocksHarvest;
    }

    private static LiveMixin ResolveCoralLiveMixin(GameObject target)
    {
        if ((Object)(object)target == (Object)null)
        {
            return null;
        }

        var liveMixin = target.GetComponentInParent<LiveMixin>();
        if ((Object)(object)liveMixin != (Object)null && IsCoralShellPlate(liveMixin.gameObject))
        {
            return liveMixin;
        }

        var ancestor = target.FindAncestor<LiveMixin>();
        if ((Object)(object)ancestor != (Object)null && IsCoralShellPlate(ancestor.gameObject))
        {
            return ancestor;
        }

        return null;
    }
}

[ProtoContract]
public sealed class CoralShellRegenData : MonoBehaviour, IProtoEventListener
{
    [ProtoMember(1)]
    public bool isRegenerating;

    [ProtoMember(2)]
    public float startedAt;

    [ProtoMember(3)]
    public float healthAtStart;

    private LiveMixin liveMixin;

    public bool BlocksHarvest => isRegenerating;

    public void Bind(LiveMixin mixin)
    {
        liveMixin = mixin;
        ApplyProgress();
    }

    public void MarkHarvested(LiveMixin mixin)
    {
        liveMixin = mixin;
        if (!CoralShellRegenFeature.TryGetTime(out float now))
        {
            return;
        }

        isRegenerating = true;
        startedAt = now;
        healthAtStart = Mathf.Clamp(mixin.health, 0f, mixin.maxHealth);
        QoLLog.Debug(Category.Coral, $"Coral shell plate regrowth started at health={healthAtStart:0.##}/{mixin.maxHealth:0.##}");
    }

    public void StartIfDamaged(LiveMixin mixin)
    {
        if (isRegenerating || (Object)(object)mixin == (Object)null || mixin.IsFullHealth())
        {
            return;
        }

        MarkHarvested(mixin);
    }

    public void OnProtoSerialize(ProtobufSerializer serializer)
    {
        Normalize();
    }

    public void OnProtoDeserialize(ProtobufSerializer serializer)
    {
        Normalize();
    }

    private void Start()
    {
        liveMixin = GetComponent<LiveMixin>();
        ApplyProgress();
    }

    private void Update()
    {
        ApplyProgress();
    }

    public void ApplyProgress()
    {
        if (!isRegenerating)
        {
            return;
        }

        if ((Object)(object)liveMixin == (Object)null)
        {
            liveMixin = GetComponent<LiveMixin>();
        }

        if ((Object)(object)liveMixin == (Object)null || !CoralShellRegenFeature.TryGetTime(out float now))
        {
            return;
        }

        float duration = CoralShellRegenFeature.GetRegrowthDurationSeconds();
        float elapsed = Mathf.Max(0f, now - startedAt);
        float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        float targetHealth = Mathf.Lerp(healthAtStart, liveMixin.maxHealth, progress);

        if (targetHealth > liveMixin.health)
        {
            liveMixin.health = Mathf.Min(targetHealth, liveMixin.maxHealth);
        }

        if (progress >= 1f || liveMixin.IsFullHealth())
        {
            liveMixin.health = liveMixin.maxHealth;
            isRegenerating = false;
            startedAt = 0f;
            healthAtStart = 0f;
            CoralShellRegenFeature.ResetBleachState(GetComponent<CoralBlendWhite>());
            QoLLog.Debug(Category.Coral, "Coral shell plate fully regrown and unbleached.");
        }
    }

    private void Normalize()
    {
        if (!isRegenerating)
        {
            startedAt = 0f;
            healthAtStart = 0f;
        }
    }
}

[HarmonyPatch(typeof(LiveMixin), "Start")]
internal static class LiveMixin_Start_CoralShellRegen_Patch
{
    [HarmonyPostfix]
    private static void Postfix(LiveMixin __instance)
    {
        CoralShellRegenFeature.Ensure(__instance);
    }
}

[HarmonyPatch(typeof(CoralBlendWhite), "OnKill")]
internal static class CoralBlendWhite_OnKill_CoralShellRegen_Patch
{
    [HarmonyPostfix]
    private static void Postfix(CoralBlendWhite __instance)
    {
        var liveMixin = __instance != null ? __instance.GetComponent<LiveMixin>() : null;
        var data = CoralShellRegenFeature.Ensure(liveMixin);
        if ((Object)(object)data != (Object)null)
        {
            data.MarkHarvested(liveMixin);
        }
    }
}

[HarmonyPatch(typeof(LiveMixin), nameof(LiveMixin.TakeDamage))]
internal static class LiveMixin_TakeDamage_CoralShellRegen_Patch
{
    [HarmonyPrefix]
    private static bool Prefix(LiveMixin __instance, ref bool __result)
    {
        if (CoralShellRegenFeature.BlocksDamage(__instance))
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Knife), "GiveResourceOnDamage")]
internal static class Knife_GiveResourceOnDamage_CoralShellRegen_Patch
{
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool Prefix(GameObject target)
    {
        return !CoralShellRegenFeature.BlocksHarvest(target);
    }
}
