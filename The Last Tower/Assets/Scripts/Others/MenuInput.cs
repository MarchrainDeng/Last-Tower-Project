using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// メニュー共通の入力判定
///
/// 【決定】ゲームパッドA（South） / Enter
/// 【戻る】ゲームパッドB（East） / マウス右クリック / Backspace
/// 【マウス】左クリック、ホバー判定（UIのRectTransform上にカーソルがあるか）
/// </summary>
public static class MenuInput
{
    // ─── 決定 / 戻る ─────────────────────────────────────────────
    public static bool ConfirmPressed()
    {
        var pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true;

        var kb = Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
    }

    public static bool CancelPressed()
    {
        var pad = Gamepad.current;
        if (pad != null && pad.buttonEast.wasPressedThisFrame) return true;

        if (RightClicked()) return true;

        var kb = Keyboard.current;
        return kb != null && kb.backspaceKey.wasPressedThisFrame;
    }

    // ─── マウス ─────────────────────────────────────────────────
    public static bool LeftClicked()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    public static bool LeftHeld()
    {
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    public static bool RightClicked()
    {
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
    }

    /// <summary>
    /// マウスが動いたか（止まっているカーソルがパッド操作の選択を上書きしないように使う）
    /// </summary>
    public static bool PointerMoved()
    {
        return Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
    }

    public static Vector2 PointerPosition()
    {
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    /// <summary>
    /// マウスカーソルが対象のUI上にあるか
    /// </summary>
    public static bool IsPointerOver(RectTransform rect)
    {
        if (Mouse.current == null || rect == null || !rect.gameObject.activeInHierarchy) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, PointerPosition(), GetCamera(rect));
    }

    /// <summary>
    /// カーソルが乗っている要素の番号。どれにも乗っていなければ -1
    /// </summary>
    public static int PointerOverIndex(RectTransform[] rects)
    {
        if (rects == null) return -1;
        for (int i = 0; i < rects.Length; i++)
            if (IsPointerOver(rects[i])) return i;
        return -1;
    }

    /// <summary>
    /// 対象のRect内でのカーソルの横位置（左端0〜右端1）
    /// </summary>
    public static float PointerNormalizedX(RectTransform rect)
    {
        if (rect == null) return 0f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, PointerPosition(), GetCamera(rect), out var local);
        return Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x);
    }

    static Camera GetCamera(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}
