using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastTower.ColorRestore
{
    /// <summary>
    /// 検証シーン用の操作スクリプト（本番では不要）。
    /// 1〜3: 各エリアの色を戻す / 4: 画面全体の色を戻す / R: 全部白黒に戻す
    /// クリック: クリックした場所から色が広がる / Space: 自動デモをもう一度
    /// </summary>
    public class ColorRestoreDemo : MonoBehaviour
    {
        public MonochromeWorld world;
        public ColorRestoreArea[] areas;
        [Tooltip("クリックした場所に複製するエリア")]
        public ColorRestoreArea clickTemplate;
        public bool autoPlay = true;

        readonly List<ColorRestoreArea> _spawned = new List<ColorRestoreArea>();
        Coroutine _auto;

        void Start()
        {
            if (autoPlay) _auto = StartCoroutine(AutoDemo());
        }

        IEnumerator AutoDemo()
        {
            ResetAll();
            yield return new WaitForSeconds(1.0f);
            foreach (var a in areas)
            {
                if (a == null) continue;
                a.Play();
                yield return new WaitForSeconds(a.duration * 0.8f + 0.4f);
            }
            yield return new WaitForSeconds(1.0f);
            world.RestoreAll(2.5f);
            _auto = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < areas.Length && i < 3; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame && areas[i] != null) { StopAuto(); areas[i].Play(); }
                if (kb.digit4Key.wasPressedThisFrame) { StopAuto(); world.RestoreAll(2.5f); }
                if (kb.rKey.wasPressedThisFrame) { StopAuto(); ResetAll(); }
                if (kb.spaceKey.wasPressedThisFrame) { StopAuto(); _auto = StartCoroutine(AutoDemo()); }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && clickTemplate != null)
            {
                Vector3 p = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                p.z = 0f;
                var a = Instantiate(clickTemplate, p, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
                a.gameObject.SetActive(true);
                a.seed = Random.value * 100f;
                a.progress = 0f;
                a.Play(1.6f);
                _spawned.Add(a);
            }
        }

        void StopAuto()
        {
            if (_auto != null) StopCoroutine(_auto);
            _auto = null;
        }

        void ResetAll()
        {
            foreach (var a in areas) if (a != null) a.SetProgress(0f);
            foreach (var a in _spawned) if (a != null) Destroy(a.gameObject);
            _spawned.Clear();
            world.FadeGlobal(0f, 0f);
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 600, 120),
                "1〜3: エリアごとに色を戻す   4: 画面全体の色を戻す\n" +
                "クリック: その場所から色が広がる   R: 白黒に戻す   Space: 自動デモ");
        }
    }
}
