using UnityEngine;

public class DestroyDebugger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnDestroy()
    {
        Debug.LogError(
            $"[被销毁] {gameObject.name} | " +
            $"Frame: {Time.frameCount}",
            gameObject
        );
    }
}
