#nullable disable
namespace InferiusQoL.Features.InventoryStacking;

using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

internal sealed class StackedPrefab<TComponent>
    where TComponent : Component
{
    public GameObject GameObject { get; private set; }

    public TComponent Component { get; private set; }

    public Pickupable Pickupable { get; private set; }

    public void Set(GameObject gameObject, TComponent component, Pickupable pickupable)
    {
        GameObject = gameObject;
        Component = component;
        Pickupable = pickupable;
    }
}

internal static class StackedPrefabFactory
{
    public static IEnumerator InstantiatePickup(TechType tech, int stackCount, StackedPrefab<Pickupable> result)
    {
        return Instantiate(tech, stackCount, result);
    }

    public static IEnumerator Instantiate<TComponent>(TechType tech, int stackCount, StackedPrefab<TComponent> result)
        where TComponent : Component
    {
        var prefabResult = new TaskResult<GameObject>();
        yield return CraftData.InstantiateFromPrefabAsync(tech, (IOut<GameObject>)(object)prefabResult, false);

        var gameObject = prefabResult.Get();
        if ((Object)(object)gameObject == (Object)null)
        {
            yield break;
        }

        var component = gameObject.GetComponent<TComponent>();
        var pickupable = ResolvePickupable(gameObject, component);
        if ((Object)(object)component == (Object)null || (Object)(object)pickupable == (Object)null)
        {
            Object.Destroy((Object)(object)gameObject);
            yield break;
        }

        CrafterLogic.NotifyCraftEnd(gameObject, tech);
        Stack.Ensure(pickupable, stackCount);
        result.Set(gameObject, component, pickupable);
    }

    private static Pickupable ResolvePickupable<TComponent>(GameObject gameObject, TComponent component)
        where TComponent : Component
    {
        if (component is PlayerTool tool && (Object)(object)tool.pickupable != (Object)null)
        {
            return tool.pickupable;
        }

        return gameObject.GetComponent<Pickupable>();
    }
}
