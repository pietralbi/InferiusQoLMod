#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InferiusQoL.Features.InventoryStacking.Patches;

[HarmonyPatch(typeof(uGUI_RecipeEntry), "UpdateIngredients")]
[HarmonyPriority(0)]
internal static class uGUI_RecipeEntry_UpdateIngredients_TotalUnits_Postfix
{
	[HarmonyPostfix]
	private static void Postfix(uGUI_RecipeEntry __instance, ItemsContainer container, bool ping)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)__instance == (Object)null || container == null)
		{
			return;
		}
		ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(__instance.techType);
		if (ingredients == null || ingredients.Count == 0)
		{
			return;
		}
		List<uGUI_RecipeItem> value = Traverse.Create((object)__instance).Field<List<uGUI_RecipeItem>>("items").Value;
		if (value == null || value.Count == 0)
		{
			return;
		}
		int num = -1;
		for (int i = 0; i < ingredients.Count && i < value.Count; i++)
		{
			Ingredient val = ingredients[i];
			int num2 = CraftingCounts.UnitsIn(container, val.techType);
			int amount = val.amount;
			int num3 = ((amount > 0) ? (num2 / amount) : 0);
			if (num < 0 || num3 < num)
			{
				num = num3;
			}
			uGUI_RecipeItem obj = value[i];
			object value2 = Traverse.Create((object)obj).Field("text").GetValue();
			if (value2 != null)
			{
				Color val2 = ((num2 >= amount) ? __instance.manager.colorGreen : __instance.manager.colorRed);
				Traverse.Create(value2).Property("color", (object[])null).SetValue((object)val2);
			}
			obj.Set(val.techType, num2, amount, ping);
		}
		int craftAmount = TechData.GetCraftAmount(__instance.techType);
		num *= craftAmount;
		object value3 = Traverse.Create((object)__instance).Field("text").GetValue();
		if (num > 0)
		{
			Traverse.Create((object)__instance).Field<int>("min").Value = num;
			if (value3 != null)
			{
				Traverse.Create(value3).Property("text", (object[])null).SetValue((object)("x" + IntStringCache.GetStringForInt(num)));
			}
		}
		else
		{
			Traverse.Create((object)__instance).Field<int>("min").Value = int.MinValue;
			if (value3 != null)
			{
				Traverse.Create(value3).Property("text", (object[])null).SetValue((object)string.Empty);
			}
		}
	}
}
