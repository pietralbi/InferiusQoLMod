#nullable disable
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace InferiusQoL.Features.InventoryStacking;

internal static class StackIconHelper
{
	internal const string BadgeName = "StackCountBadge";

	private const string LegacyTmp = "StackAmountTMP";

	private const string LegacyUiTextChild = "StackCount";

	private static Font s_fallbackFont;

	private static Font FallbackFont
	{
		get
		{
			if ((Object)(object)s_fallbackFont == (Object)null)
			{
				s_fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				if ((Object)(object)s_fallbackFont == (Object)null)
				{
					s_fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
				}
			}
			return s_fallbackFont;
		}
	}

	private static void DestroyLegacyChildren(uGUI_ItemIcon icon)
	{
		if (!((Object)(object)icon == (Object)null))
		{
			TryDestroyChild(icon, "StackAmountTMP");
			TryDestroyChild(icon, "StackCount");
		}
	}

	private static void TryDestroyChild(uGUI_ItemIcon icon, string childName)
	{
		Transform val = FindChildByName(((Component)icon).transform, childName);
		if ((Object)(object)val != (Object)null)
		{
			Object.Destroy((Object)(object)((Component)val).gameObject);
		}
	}

	private static Transform FindChildByName(Transform root, string name)
	{
		if ((Object)(object)root == (Object)null)
		{
			return null;
		}
		for (int i = 0; i < root.childCount; i++)
		{
			Transform child = root.GetChild(i);
			if (((Object)child).name == name || ((Object)child).name.StartsWith(name + "(", StringComparison.Ordinal))
			{
				return child;
			}
		}
		return null;
	}

	private static void DestroyBadge(uGUI_ItemIcon icon)
	{
		TryDestroyChild(icon, "StackCountBadge");
	}

	public static void HideLabel(uGUI_ItemIcon icon)
	{
		Clear(icon);
	}

	private static void Clear(uGUI_ItemIcon icon)
	{
		if (!((Object)(object)icon == (Object)null))
		{
			DestroyLegacyChildren(icon);
			DestroyBadge(icon);
		}
	}

	private static void SyncVanillaStackLabel(uGUI_ItemIcon icon, Pickupable p)
	{
		if (!((Object)(object)icon == (Object)null) && !((Object)(object)p == (Object)null) && uGUI.isInitialized)
		{
			if (!StackRules.CanStack(p))
			{
				icon.SetNotificationAlpha(0f);
			}
			else if (MRStack.CountOf(p) > 1)
			{
				icon.SetNotificationAlpha(0f);
			}
		}
	}

	public static void BringStackBadgeToFront(uGUI_ItemIcon icon)
	{
		Transform val = (((Object)(object)icon == (Object)null) ? null : FindChildByName(((Component)icon).transform, "StackCountBadge"));
		if ((Object)(object)val != (Object)null)
		{
			val.SetAsLastSibling();
		}
	}

	private static void ApplyCountToBadge(Transform badgeRoot, int count)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)badgeRoot == (Object)null)
		{
			return;
		}
		string text = count.ToString();
		uGUI_NotificationLabel component = ((Component)badgeRoot).GetComponent<uGUI_NotificationLabel>();
		if ((Object)(object)component != (Object)null)
		{
			object value = Traverse.Create((object)component).Field("text").GetValue();
			if (value != null)
			{
				Traverse.Create(value).Property("color", (object[])null).SetValue((object)Color.white);
			}
			component.SetText(text);
			component.SetAlpha(1f);
		}
		else
		{
			Text componentInChildren = ((Component)badgeRoot).GetComponentInChildren<Text>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				componentInChildren.text = text;
				((Behaviour)componentInChildren).enabled = true;
			}
		}
	}

	private static uGUI_NotificationLabel TryCreateNotificationLabel(uGUI_ItemIcon icon)
	{
		if (uGUI.isInitialized && (Object)(object)uGUI.main != (Object)null && (Object)(object)uGUI.main.prefabNotificationLabel != (Object)null)
		{
			GameObject val = Object.Instantiate<GameObject>(uGUI.main.prefabNotificationLabel, (Transform)(object)((Graphic)icon).rectTransform, false);
			((Object)val).name = "StackCountBadge";
			uGUI_NotificationLabel component = val.GetComponent<uGUI_NotificationLabel>();
			if ((Object)(object)component != (Object)null)
			{
				return component;
			}
			Object.Destroy((Object)(object)val);
		}
		uGUI_NotificationLabel val2 = uGUI_NotificationLabel.CreateInstance(((Graphic)icon).rectTransform);
		if ((Object)(object)val2 != (Object)null)
		{
			((Object)((Component)val2).gameObject).name = "StackCountBadge";
		}
		return val2;
	}

	private static void CreateFallbackTextBadge(uGUI_ItemIcon icon, int count)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("StackCountBadge")
		{
			layer = ((Component)icon).gameObject.layer
		};
		RectTransform obj = val.AddComponent<RectTransform>();
		((Transform)obj).SetParent((Transform)(object)((Graphic)icon).rectTransform, false);
		obj.anchorMin = new Vector2(1f, 0f);
		obj.anchorMax = new Vector2(1f, 0f);
		obj.pivot = new Vector2(1f, 0f);
		obj.anchoredPosition = new Vector2(-2f, 2f);
		obj.sizeDelta = new Vector2(32f, 20f);
		Text obj2 = val.AddComponent<Text>();
		obj2.font = FallbackFont;
		obj2.fontSize = 14;
		obj2.fontStyle = (FontStyle)1;
		obj2.alignment = (TextAnchor)8;
		((Graphic)obj2).color = Color.white;
		((Graphic)obj2).raycastTarget = false;
		obj2.text = count.ToString();
		Outline obj3 = val.AddComponent<Outline>();
		((Shadow)obj3).effectColor = new Color(0f, 0f, 0f, 0.9f);
		((Shadow)obj3).effectDistance = new Vector2(1f, -1f);
	}

	public static void UpdateForPickup(uGUI_ItemIcon icon, Pickupable p)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)icon == (Object)null || (Object)(object)p == (Object)null)
		{
			return;
		}
		DestroyLegacyChildren(icon);
		if (!StackRules.CanStack(p))
		{
			Clear(icon);
			SyncVanillaStackLabel(icon, p);
			return;
		}
		int num = MRStack.CountOf(p);
		if (num <= 1)
		{
			DestroyBadge(icon);
			SyncVanillaStackLabel(icon, p);
			return;
		}
		Transform val = FindChildByName(((Component)icon).transform, "StackCountBadge");
		if ((Object)(object)val != (Object)null)
		{
			ApplyCountToBadge(val, num);
			((Component)val).gameObject.SetActive(true);
			val.localScale = Vector3.one;
			BringStackBadgeToFront(icon);
			LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
			SyncVanillaStackLabel(icon, p);
		}
		else
		{
			if (!uGUI.isInitialized)
			{
				return;
			}
			uGUI_NotificationLabel val2 = TryCreateNotificationLabel(icon);
			if ((Object)(object)val2 != (Object)null)
			{
				val2.SetAnchor((UIAnchor)2);
				val2.SetOffset(new Vector2(-4f, 4f));
				val2.SetBackgroundColor(new Color(0.06f, 0.06f, 0.06f, 0.92f));
				object value = Traverse.Create((object)val2).Field("text").GetValue();
				if (value != null)
				{
					Traverse.Create(value).Property("color", (object[])null).SetValue((object)Color.white);
				}
				val2.SetText(num.ToString());
				val2.SetAlpha(1f);
				((Component)val2).gameObject.SetActive(true);
				((Component)val2).transform.localScale = Vector3.one;
				BringStackBadgeToFront(icon);
				LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
				SyncVanillaStackLabel(icon, p);
			}
			else
			{
				CreateFallbackTextBadge(icon, num);
				BringStackBadgeToFront(icon);
				LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
				SyncVanillaStackLabel(icon, p);
			}
		}
	}
}
