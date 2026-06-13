namespace InferiusQoL.Features.MobileResourceScanner;

using UWE;

internal sealed class MobileResourceScannerMenu : uGUI_InputGroup, uGUI_IButtonReceiver
{
    public override void Update()
    {
        if (focused && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
        {
            MobileResourceScannerFeature.CloseMenu();
            GameInput.ClearInput();
            return;
        }

        base.Update();
    }

    public bool OnButtonDown(GameInput.Button button)
    {
        if (button != GameInput.Button.UIMenu)
            return false;

        MobileResourceScannerFeature.CloseMenu();
        GameInput.ClearInput();
        return true;
    }

    private void OnEnable()
    {
        uGUI_LegendBar.ClearButtons();
        uGUI_LegendBar.ChangeButton(0, GameInput.FormatButton(GameInput.Button.UICancel, false), Language.main.GetFormat("Back"));
        uGUI_LegendBar.ChangeButton(1, GameInput.FormatButton(GameInput.Button.UISubmit, false), Language.main.GetFormat("ItemSelectorSelect"));
    }

    public override void OnDisable()
    {
        base.OnDisable();
        uGUI_LegendBar.ClearButtons();
        FreezeTime.End(FreezeTime.Id.IngameMenu);
    }

    public override void OnSelect(bool lockMovement)
    {
        base.OnSelect(lockMovement);
        gameObject.SetActive(true);
        FreezeTime.Begin(FreezeTime.Id.IngameMenu);
        Utils.lockCursor = false;
    }

    public override void OnDeselect()
    {
        base.OnDeselect();
        MobileResourceScannerFeature.OnMenuDeselected();
    }
}
