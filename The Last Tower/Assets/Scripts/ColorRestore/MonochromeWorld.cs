using System.Collections;
using UnityEngine;

namespace LastTower.ColorRestore
{
    /// <summary>
    /// シーンに1つ置くと、そのシーンの画面が白黒になる。
    /// ColorRestoreArea を置いた所だけ色が戻る。
    /// globalRestore を 1 にすると画面全体の色が戻る（ステージクリア演出など）。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Last Tower/Color Restore/Monochrome World")]
    public class MonochromeWorld : MonoBehaviour
    {
        public static MonochromeWorld Active { get; private set; }

        [Header("白黒の見た目")]
        [Tooltip("白黒の強さ。0=元の色、1=完全な白黒")]
        [Range(0f, 1f)] public float grayAmount = 1f;
        [Tooltip("白黒部分の色味。白なら純粋なグレー、少し茶色にするとセピア調")]
        public Color grayTint = Color.white;
        [Range(0.2f, 2f)] public float contrast = 1.1f;
        [Range(0.2f, 2f)] public float brightness = 1f;

        [Header("色の戻り")]
        [Tooltip("画面全体の色の戻り具合。0=白黒のまま、1=全部カラー")]
        [Range(0f, 1f)] public float globalRestore = 0f;
        [Tooltip("色が戻りかけている境目をほんのり光らせる色")]
        public Color edgeColor = new Color(1f, 0.85f, 0.55f, 1f);
        [Range(0f, 1f)] public float edgeIntensity = 0.15f;

        [Header("その他")]
        [Tooltip("オンにすると逆になる（カラーの世界で、エリアの所だけ白黒）")]
        public bool invert = false;

        Coroutine _routine;

        void OnEnable()
        {
            Active = this;
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        /// <summary>画面全体の色を duration 秒かけて戻す。</summary>
        public void RestoreAll(float duration = 2f) => FadeGlobal(1f, duration);

        /// <summary>画面全体を duration 秒かけて白黒に戻す。</summary>
        public void MonochromeAll(float duration = 1f) => FadeGlobal(0f, duration);

        public void FadeGlobal(float target, float duration)
        {
            if (_routine != null) StopCoroutine(_routine);
            if (!Application.isPlaying || duration <= 0f) { globalRestore = target; return; }
            _routine = StartCoroutine(FadeRoutine(target, duration));
        }

        IEnumerator FadeRoutine(float target, float duration)
        {
            float from = globalRestore;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                globalRestore = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            globalRestore = target;
            _routine = null;
        }
    }
}
