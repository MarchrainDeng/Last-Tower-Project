using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// リールを巻く速さを読む
///
/// - マウス  : カーソルをぐるぐる回す。回した中心は直近のカーソル位置の平均から求めるので、画面のどこで回してもよい
/// - パッド  : スティックをぐるぐる回す
/// - キーボード: Space を押している間、一定の速さで巻く
/// 往復させただけでは回転が打ち消し合って巻けない。回す向きはどちらでもよい
/// </summary>
public class FishingReelInput
{
    // 1秒に1回転 = 2π rad/s を「ふつうの速さ」とする
    public const float NormalSpeed = Mathf.PI * 2f;

    const float CenterFollowTime = 0.35f;  // 回転の中心がカーソルに追いつく時間
    const float MinRadius = 20f;           // これより中心に近い動きは回転とみなさない（画面px）
    const float SmoothTime = 0.12f;
    const float StickDeadZone = 0.5f;

    Vector2 mouseCenter, mousePrev;
    bool mouseReady;
    Vector2 stickPrev;
    bool stickReady;
    float signedSpeed;

    /// <summary>
    /// 巻く速さ（rad/s）
    /// </summary>
    public float Speed { get; private set; }

    /// <summary>
    /// リールの見た目用の回転角（度）
    /// </summary>
    public float Angle { get; private set; }

    public void Reset()
    {
        mouseReady = stickReady = false;
        signedSpeed = 0f;
        Speed = 0f;
    }

    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        float delta = ReadMouse(deltaTime) + ReadStick();
        float target = delta / deltaTime;

        // 往復運動は符号が入れ替わって打ち消し合う
        signedSpeed = Mathf.Lerp(signedSpeed, target, 1f - Mathf.Exp(-deltaTime / SmoothTime));
        Speed = Mathf.Abs(signedSpeed);

        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.isPressed) Speed = Mathf.Max(Speed, NormalSpeed);

        Angle += Speed * Mathf.Rad2Deg * deltaTime;
    }

    float ReadMouse(float deltaTime)
    {
        var mouse = Mouse.current;
        if (mouse == null) return 0f;

        Vector2 pos = mouse.position.ReadValue();
        if (!mouseReady)
        {
            mouseCenter = mousePrev = pos;
            mouseReady = true;
            return 0f;
        }

        mouseCenter = Vector2.Lerp(mouseCenter, pos, 1f - Mathf.Exp(-deltaTime / CenterFollowTime));
        Vector2 a = mousePrev - mouseCenter;
        Vector2 b = pos - mouseCenter;
        mousePrev = pos;

        if (a.magnitude < MinRadius || b.magnitude < MinRadius) return 0f;
        return Vector2.SignedAngle(a, b) * Mathf.Deg2Rad;
    }

    float ReadStick()
    {
        var pad = Gamepad.current;
        if (pad == null) return 0f;

        // 左右どちらのスティックで回してもよい（大きく倒している方）
        Vector2 left = pad.leftStick.ReadValue();
        Vector2 right = pad.rightStick.ReadValue();
        Vector2 stick = right.sqrMagnitude > left.sqrMagnitude ? right : left;

        if (stick.magnitude < StickDeadZone)
        {
            stickReady = false;
            return 0f;
        }

        float result = stickReady ? Vector2.SignedAngle(stickPrev, stick) * Mathf.Deg2Rad : 0f;
        stickPrev = stick;
        stickReady = true;
        return result;
    }
}
