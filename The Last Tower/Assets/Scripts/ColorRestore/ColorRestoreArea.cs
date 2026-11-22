using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LastTower.ColorRestore
{
    /// <summary>
    /// マスク画像の形で、白黒の世界に色を戻すエリア。
    /// 好きな場所に置いて、拡大・回転・移動できる（Transform がそのまま効く）。
    /// マスク画像の白い所ほど早く色が戻るので、白→黒のグラデーションで「広がる順番」を描ける。
    /// progress を 0→1 に動かすと色が広がる（Animator / Timeline / スクリプトどれでも可）。
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Last Tower/Color Restore/Color Restore Area")]
    public class ColorRestoreArea : MonoBehaviour
    {
        static readonly List<ColorRestoreArea> s_Active = new List<ColorRestoreArea>();
        public static IReadOnlyList<ColorRestoreArea> ActiveAreas => s_Active;

        [Tooltip("色を戻す範囲を描いたマスク画像。白=先に戻る / 黒・透明=戻らない")]
        public Sprite maskSprite;

        [Tooltip("色の戻り具合。0=白黒のまま、1=マスク全体がカラー")]
        [Range(0f, 1f)] public float progress = 0f;
        [Tooltip("このエリアで戻せる色の最大量（半分だけ色を戻す、などに使う）")]
        [Range(0f, 1f)] public float strength = 1f;

        [Header("筆っぽさ")]
        [Tooltip("境目のなめらかさ。小さいほどくっきり")]
        [Range(0.01f, 1f)] public float softness = 0.12f;
        [Tooltip("境目のギザギザの強さ")]
        [Range(0f, 1f)] public float noiseStrength = 0.35f;
        [Tooltip("ギザギザの大きさ（ワールド単位）")]
        [Range(0.02f, 5f)] public float noiseScale = 0.6f;
        [Tooltip("筆の向き（度）。オブジェクトの回転にも足される")]
        public float strokeAngle = 0f;
        [Tooltip("筆の向きにギザギザを伸ばす量。大きいほど刷毛で掃いたような筋になる")]
        [Range(1f, 10f)] public float strokeStretch = 3f;
        public float seed = 0f;

        [Header("再生")]
        [Tooltip("シーン開始時に自動で色を戻す")]
        public bool playOnStart = false;
        public float playDelay = 0f;
        public float duration = 2f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public UnityEvent onRestored;

        Coroutine _routine;

        public bool IsPlaying => _routine != null;

        void OnEnable()
        {
            if (!s_Active.Contains(this)) s_Active.Add(this);
        }

        void OnDisable()
        {
            s_Active.Remove(this);
            _routine = null;
        }

        void Start()
        {
            if (Application.isPlaying && playOnStart) Play();
        }

        /// <summary>今の状態から色を戻し始める（duration 秒）。</summary>
        public void Play() => PlayTo(1f, duration, playDelay);

        /// <summary>指定秒数で色を戻す。</summary>
        public void Play(float seconds) => PlayTo(1f, seconds, 0f);

        /// <summary>白黒に戻す。</summary>
        public void Rewind(float seconds = 0.5f) => PlayTo(0f, seconds, 0f);

        public void SetProgress(float value)
        {
            StopRoutine();
            progress = Mathf.Clamp01(value);
        }

        public void PlayTo(float target, float seconds, float delay)
        {
            StopRoutine();
            if (!Application.isPlaying || (seconds <= 0f && delay <= 0f)) { progress = target; return; }
            _routine = StartCoroutine(PlayRoutine(target, seconds, delay));
        }

        void StopRoutine()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
        }

        IEnumerator PlayRoutine(float target, float seconds, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float from = progress;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                progress = Mathf.Lerp(from, target, curve.Evaluate(t / seconds));
                yield return null;
            }
            progress = target;
            _routine = null;
            if (target >= 1f) onRestored?.Invoke();
        }

        // ---- ColorRestoreFeature から使う ----

        internal bool ShouldDraw => maskSprite != null && progress > 0f && strength > 0f && isActiveAndEnabled;

        /// <summary>単位四角形 (-0.5..0.5) をスプライトの大きさ・位置に合わせる行列。</summary>
        internal Matrix4x4 GetQuadMatrix()
        {
            Bounds b = maskSprite.bounds;
            return transform.localToWorldMatrix * Matrix4x4.TRS(b.center, Quaternion.identity, new Vector3(b.size.x, b.size.y, 1f));
        }

        internal void FillProperties(MaterialPropertyBlock mpb)
        {
            Texture2D tex = maskSprite.texture;
            Rect r = maskSprite.textureRect;
            var st = new Vector4(r.width / tex.width, r.height / tex.height, r.x / tex.width, r.y / tex.height);

            mpb.SetTexture(ShaderIds.MainTex, tex);
            mpb.SetVector(ShaderIds.MainTexST, st);
            mpb.SetFloat(ShaderIds.Progress, progress);
            mpb.SetFloat(ShaderIds.Strength, strength);
            mpb.SetFloat(ShaderIds.Softness, softness);
            mpb.SetFloat(ShaderIds.NoiseStrength, noiseStrength);
            mpb.SetFloat(ShaderIds.NoiseScale, noiseScale);
            mpb.SetFloat(ShaderIds.StrokeAngle, (strokeAngle + transform.eulerAngles.z) * Mathf.Deg2Rad);
            mpb.SetFloat(ShaderIds.StrokeStretch, strokeStretch);
            mpb.SetFloat(ShaderIds.Seed, seed);
        }

        void OnDrawGizmos()
        {
            if (maskSprite == null) return;
            Gizmos.matrix = GetQuadMatrix();
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }

    internal static class ShaderIds
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");
        public static readonly int Progress = Shader.PropertyToID("_Progress");
        public static readonly int Strength = Shader.PropertyToID("_Strength");
        public static readonly int Softness = Shader.PropertyToID("_Softness");
        public static readonly int NoiseStrength = Shader.PropertyToID("_NoiseStrength");
        public static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
        public static readonly int StrokeAngle = Shader.PropertyToID("_StrokeAngle");
        public static readonly int StrokeStretch = Shader.PropertyToID("_StrokeStretch");
        public static readonly int Seed = Shader.PropertyToID("_Seed");

        public static readonly int MatrixVP = Shader.PropertyToID("_CR_MatrixVP");
        public static readonly int MaskTex = Shader.PropertyToID("_CR_MaskTex");
        public static readonly int GrayAmount = Shader.PropertyToID("_CR_GrayAmount");
        public static readonly int GlobalRestore = Shader.PropertyToID("_CR_GlobalRestore");
        public static readonly int GrayTint = Shader.PropertyToID("_CR_GrayTint");
        public static readonly int Contrast = Shader.PropertyToID("_CR_Contrast");
        public static readonly int Brightness = Shader.PropertyToID("_CR_Brightness");
        public static readonly int EdgeColor = Shader.PropertyToID("_CR_EdgeColor");
        public static readonly int EdgeIntensity = Shader.PropertyToID("_CR_EdgeIntensity");
        public static readonly int Invert = Shader.PropertyToID("_CR_Invert");
    }
}
