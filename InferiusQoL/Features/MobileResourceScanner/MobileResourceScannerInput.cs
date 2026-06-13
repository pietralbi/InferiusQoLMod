namespace InferiusQoL.Features.MobileResourceScanner;

using System;
using System.Reflection;
using InferiusQoL.Logging;
using Nautilus.Handlers;
using UnityEngine.InputSystem;

internal static class MobileResourceScannerInput
{
    private const string ButtonId = "InferiusQoLOpenMobileResourceScanner";
    private const string InputCategory = "Inferius Quality of Life";
    private const string Language = "English";

    private static bool _registered;

    internal static GameInput.Button OpenMenu { get; private set; } = GameInput.Button.None;

    internal static void Register()
    {
        if (_registered)
            return;

        try
        {
            var builder = EnumHandler.AddEntry<GameInput.Button>(ButtonId, Assembly.GetExecutingAssembly());
            if (builder == null)
            {
                OpenMenu = (GameInput.Button)Enum.Parse(typeof(GameInput.Button), ButtonId);
            }
            else
            {
                OpenMenu = builder
                    .CreateInput("Open Mobile Resource Scanner", "Open the Mobile Resource Scanner resource menu.", Language, InputActionType.Button)
                    .WithKeyboardBinding("<Mouse>/leftButton", string.Empty)
                    .WithCategory(InputCategory)
                    .SetBindable(GameInput.Device.Keyboard)
                    .Value;
            }

            _registered = true;
            QoLLog.Info(Category.MobileScanner, $"Registered Mobile Resource Scanner input as {OpenMenu}");
        }
        catch (Exception ex)
        {
            OpenMenu = GameInput.Button.None;
            QoLLog.Error(Category.MobileScanner, "Failed to register Mobile Resource Scanner input", ex);
        }
    }
}
