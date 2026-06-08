#nullable disable
namespace InferiusQoL.UI;

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static class HotkeyFocusGuard
{
	public static bool ShouldIgnoreHotkey()
	{
		try
		{
			if (GameInput.IsRebinding)
			{
				return true;
			}
			if (GUIUtility.keyboardControl != 0)
			{
				return true;
			}

			GameObject selected = EventSystem.current?.currentSelectedGameObject;
			if (selected != null && HasTextInputInParents(selected, allowActiveInput: true))
			{
				return true;
			}

			var group = FPSInputModule.current != null ? FPSInputModule.current.lastGroup : null;
			if (group != null && group.focused && HasActiveTextInputInChildren(group.gameObject))
			{
				return true;
			}
		}
		catch
		{
			// Focus probing should never break gameplay input.
		}

		return false;
	}

	private static bool HasTextInputInParents(GameObject selected, bool allowActiveInput)
	{
		InputField unityInput = selected.GetComponentInParent<InputField>();
		if (unityInput != null && (unityInput.isFocused || (allowActiveInput && unityInput.isActiveAndEnabled)))
		{
			return true;
		}

		foreach (MonoBehaviour behaviour in selected.GetComponentsInParent<MonoBehaviour>(includeInactive: true))
		{
			if (IsTextMeshProInputField(behaviour, allowActiveInput))
			{
				return true;
			}
		}

		return false;
	}

	private static bool HasActiveTextInputInChildren(GameObject root)
	{
		foreach (InputField unityInput in root.GetComponentsInChildren<InputField>(includeInactive: true))
		{
			if (unityInput != null && unityInput.isActiveAndEnabled)
			{
				return true;
			}
		}

		foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
		{
			if (IsTextMeshProInputField(behaviour, allowActiveInput: true))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsTextMeshProInputField(MonoBehaviour behaviour, bool allowActiveInput)
	{
		if (behaviour == null)
		{
			return false;
		}

		Type type = behaviour.GetType();
		if (!IsTextMeshProInputFieldType(type))
		{
			return false;
		}

		return GetBoolProperty(type, behaviour, "isFocused")
			|| GetBoolProperty(type, behaviour, "isSelected")
			|| GetBoolField(type, behaviour, "m_AllowInput")
			|| (allowActiveInput && behaviour.isActiveAndEnabled);
	}

	private static bool IsTextMeshProInputFieldType(Type type)
	{
		while (type != null)
		{
			if (type.Name == "TMP_InputField" || type.FullName == "TMPro.TMP_InputField")
			{
				return true;
			}
			type = type.BaseType;
		}

		return false;
	}

	private static bool GetBoolProperty(Type type, object instance, string name)
	{
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		PropertyInfo property = type.GetProperty(name, Flags);
		if (property == null || property.PropertyType != typeof(bool))
		{
			return false;
		}

		return property.GetValue(instance, null) is true;
	}

	private static bool GetBoolField(Type type, object instance, string name)
	{
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		FieldInfo field = type.GetField(name, Flags);
		if (field == null || field.FieldType != typeof(bool))
		{
			return false;
		}

		return field.GetValue(instance) is true;
	}
}
