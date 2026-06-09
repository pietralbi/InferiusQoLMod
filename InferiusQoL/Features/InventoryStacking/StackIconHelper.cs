#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
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

	private const int IconCacheVerificationIntervalFrames = 30;

	private const int MaxIconCacheEntries = 512;

	private static readonly FieldInfo NotificationLabelTextField = AccessTools.Field(typeof(uGUI_NotificationLabel), "text");

	private static readonly Dictionary<int, IconState> s_iconStates = new Dictionary<int, IconState>();

	private static readonly Dictionary<Type, PropertyInfo> s_colorProperties = new Dictionary<Type, PropertyInfo>();

	private static Font s_fallbackFont;

	private struct IconState
	{
		public int PickupId;

		public bool CanStack;

		public int Count;

		public int FullUpdateFrame;
	}

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
		if ((Object)(object)badgeRoot == (Object)null)
		{
			return;
		}
		string text = count.ToString();
		uGUI_NotificationLabel component = ((Component)badgeRoot).GetComponent<uGUI_NotificationLabel>();
		if ((Object)(object)component != (Object)null)
		{
			SetNotificationTextColor(component, Color.white);
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

	public static void UpdateForPickup(uGUI_ItemIcon icon, Pickupable p, bool force = false)
	{
		if ((Object)(object)icon == (Object)null || (Object)(object)p == (Object)null)
		{
			return;
		}

		bool canStack = StackRules.CanStack(p);
		int count = canStack ? Stack.CountOf(p) : 1;
		if (!force && IsIconStateCurrent(icon, p, canStack, count))
		{
			return;
		}

		RememberIconState(icon, p, canStack, count);
		DestroyLegacyChildren(icon);
		if (!canStack)
		{
			DestroyBadge(icon);
			return;
		}
		if (count <= 1)
		{
			DestroyBadge(icon);
			return;
		}
		Transform val = FindChildByName(((Component)icon).transform, "StackCountBadge");
		if ((Object)(object)val != (Object)null)
		{
			ApplyCountToBadge(val, count);
			((Component)val).gameObject.SetActive(true);
			val.localScale = Vector3.one;
			BringStackBadgeToFront(icon);
			LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
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
				SetNotificationTextColor(val2, Color.white);
				val2.SetText(count.ToString());
				val2.SetAlpha(1f);
				((Component)val2).gameObject.SetActive(true);
				((Component)val2).transform.localScale = Vector3.one;
				BringStackBadgeToFront(icon);
				LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
			}
			else
			{
				CreateFallbackTextBadge(icon, count);
				BringStackBadgeToFront(icon);
				LayoutRebuilder.MarkLayoutForRebuild(((Graphic)icon).rectTransform);
			}
		}
	}

	private static bool IsIconStateCurrent(uGUI_ItemIcon icon, Pickupable pickupable, bool canStack, int count)
	{
		int iconId = GetObjectId(icon);
		if (iconId == 0 || !s_iconStates.TryGetValue(iconId, out var state))
		{
			return false;
		}

		if (Time.frameCount - state.FullUpdateFrame >= IconCacheVerificationIntervalFrames)
		{
			return false;
		}

		return state.PickupId == GetObjectId(pickupable)
			&& state.CanStack == canStack
			&& state.Count == count;
	}

	private static void RememberIconState(uGUI_ItemIcon icon, Pickupable pickupable, bool canStack, int count)
	{
		if (s_iconStates.Count > MaxIconCacheEntries)
		{
			s_iconStates.Clear();
		}

		int iconId = GetObjectId(icon);
		if (iconId == 0)
		{
			return;
		}

		s_iconStates[iconId] = new IconState
		{
			PickupId = GetObjectId(pickupable),
			CanStack = canStack,
			Count = count,
			FullUpdateFrame = Time.frameCount
		};
	}

	private static int GetObjectId(Component component)
	{
		return ((Object)(object)component == (Object)null)
			? 0
			: ((Object)((Component)component).gameObject).GetInstanceID();
	}

	private static void SetNotificationTextColor(uGUI_NotificationLabel label, Color color)
	{
		object text = NotificationLabelTextField?.GetValue(label);
		if (text != null)
		{
			SetColorProperty(text, color);
		}
	}

	private static void SetColorProperty(object target, Color color)
	{
		Type type = target.GetType();
		if (!s_colorProperties.TryGetValue(type, out var property))
		{
			property = type.GetProperty("color", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			s_colorProperties[type] = property;
		}

		if (property != null && property.CanWrite)
		{
			property.SetValue(target, color, null);
		}
	}
}
