using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// マウスを動かしたらカーソルを表示し、ゲームパッドを触ったら隠す
/// 起動時に自動で生成されるので、シーンに置く必要はない
/// </summary>
public class CursorAutoVisibility : MonoBehaviour
{
    const float StickThreshold = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject(nameof(CursorAutoVisibility));
        go.AddComponent<CursorAutoVisibility>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse != null &&
            (MenuInput.PointerMoved() || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
        {
            Cursor.visible = true;
            return;
        }

        var pad = Gamepad.current;
        if (pad != null && Cursor.visible &&
            (pad.wasUpdatedThisFrame && IsPadActive(pad)))
        {
            Cursor.visible = false;
        }
    }

    static bool IsPadActive(Gamepad pad)
    {
        if (pad.leftStick.ReadValue().magnitude > StickThreshold) return true;
        if (pad.rightStick.ReadValue().magnitude > StickThreshold) return true;
        if (pad.dpad.ReadValue() != Vector2.zero) return true;
        return pad.buttonSouth.isPressed || pad.buttonEast.isPressed ||
               pad.buttonWest.isPressed || pad.buttonNorth.isPressed ||
               pad.startButton.isPressed;
    }
}
