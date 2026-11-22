using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LastTower.ColorRestore.EditorTools
{
    /// <summary>
    /// 白黒→色が戻る演出のセットアップ。
    /// メニュー「Tools/Color Restore/...」から誰でも実行できる。
    /// </summary>
    public static class ColorRestoreSetup
    {
        const string ShaderDir = "Assets/Scripts/ColorRestore/Shaders/";
        const string MaskDir = "Assets/Sprites/ColorRestoreMasks/";
        const string ScenePath = "Assets/Scenes/Test/ColorRestoreTest.unity";

        [MenuItem("Tools/Color Restore/1. 描画機能をレンダラーに追加")]
        public static void SetupRendererMenu()
        {
            var log = new StringBuilder();
            SetupRenderers(log);
            ImportMasks(log);
            Debug.Log("[ColorRestore]\n" + log);
        }

        [MenuItem("Tools/Color Restore/2. 検証シーンを作り直す")]
        public static void CreateSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var log = new StringBuilder();
            SetupRenderers(log);
            ImportMasks(log);
            BuildTestScene(log, true);
            Debug.Log("[ColorRestore]\n" + log);
        }

        /// <summary>開いている検証シーンを、いくつかの段階で Temp/ColorRestore_*.png に撮影する（動作確認用）。</summary>
        [MenuItem("Tools/Color Restore/3. 検証シーンの段階スクショを撮る")]
        public static void CaptureStatesMenu()
        {
            var log = new StringBuilder();
            var cam = Camera.main;
            var world = Object.FindAnyObjectByType<MonochromeWorld>();
            var demo = Object.FindAnyObjectByType<ColorRestoreDemo>();
            if (cam == null || world == null || demo == null || demo.areas == null || demo.areas.Length < 3)
            {
                File.WriteAllText("Temp/ColorRestoreReport.txt", "capture skipped: 検証シーンを開いてください");
                return;
            }
            var a = demo.areas;
            void Set(float p1, float p2, float p3, float global, string name)
            {
                a[0].progress = p1; a[1].progress = p2; a[2].progress = p3; world.globalRestore = global;
                Capture(cam, "Temp/ColorRestore_" + name + ".png");
                log.AppendLine("captured " + name);
            }
            world.enabled = false;
            Capture(cam, "Temp/ColorRestore_0_original.png");
            world.enabled = true;
            Set(0f, 0f, 0f, 0f, "1_gray");
            Set(0.45f, 0.5f, 0.4f, 0f, "2_spreading");
            Set(1f, 1f, 1f, 0f, "3_all_areas");
            Set(0f, 0f, 0f, 0.5f, "4_global_half");
            Set(0f, 0f, 0f, 0f, "1_gray");
            log.AppendLine("DONE");
            File.WriteAllText("Temp/ColorRestoreReport.txt", log.ToString());
        }

        [MenuItem("GameObject/Last Tower/Color Restore Area (色を戻すエリア)", false, 10)]
        static void CreateAreaMenu(MenuCommand cmd)
        {
            var go = new GameObject("ColorRestoreArea");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            var area = go.AddComponent<ColorRestoreArea>();
            area.maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MaskDir + "Mask_Splash.png");
            area.progress = 1f;
            if (Object.FindAnyObjectByType<MonochromeWorld>() == null)
                new GameObject("MonochromeWorld").AddComponent<MonochromeWorld>();
            Undo.RegisterCreatedObjectUndo(go, "Create Color Restore Area");
            Selection.activeObject = go;
        }

        // ------------------------------------------------------------------

        public static void SetupRenderers(StringBuilder log)
        {
            var composite = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "ColorRestoreComposite.shader");
            var stamp = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "ColorRestoreStamp.shader");

            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableRendererData", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (data == null) continue;

                var existing = data.rendererFeatures.OfType<ColorRestoreFeature>().FirstOrDefault();
                if (existing != null)
                {
                    existing.compositeShader = composite;
                    existing.stampShader = stamp;
                    EditorUtility.SetDirty(existing);
                    log.AppendLine($"既に追加済み: {path}");
                    continue;
                }

                var feature = ScriptableObject.CreateInstance<ColorRestoreFeature>();
                feature.name = "ColorRestore";
                feature.compositeShader = composite;
                feature.stampShader = stamp;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedProperties();
                data.SetDirty();
                EditorUtility.SetDirty(data);
                log.AppendLine($"描画機能を追加: {path}");
            }
            AssetDatabase.SaveAssets();
        }

        public static void ImportMasks(StringBuilder log)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { MaskDir.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti) continue;
                bool changed = ti.textureType != TextureImporterType.Sprite || ti.sRGBTexture || ti.mipmapEnabled
                               || ti.spriteImportMode != SpriteImportMode.Single || ti.textureCompression != TextureImporterCompression.Uncompressed;
                if (!changed) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.sRGBTexture = false;           // マスクは「量」なのでリニアで読む
                ti.alphaIsTransparency = false;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.spritePixelsPerUnit = 100;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
                log.AppendLine($"マスク画像の設定: {path}");
            }
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

        static SpriteRenderer AddSprite(string name, string path, Vector2 pos, float height, int order)
        {
            var sprite = LoadSprite(path);
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return sr;
        }

        static ColorRestoreArea AddArea(string name, string mask, Vector2 pos, float rotZ, Vector2 scale, float duration)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 0, rotZ));
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var a = go.AddComponent<ColorRestoreArea>();
            a.maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MaskDir + mask + ".png");
            a.duration = duration;
            return a;
        }

        public static Scene BuildTestScene(StringBuilder log, bool single)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, single ? NewSceneMode.Single : NewSceneMode.Additive);
            if (!single) SceneManager.SetActiveScene(scene);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            light.intensity = 1f;

            // いろんなイラストを重ねたコラージュ（奥→手前）
            const string T = "Assets/Sprites/ColorRestoreTest/";
            var art = new GameObject("Illustrations").transform;
            AddSprite("空", T + "CR_Sky.png", new Vector2(0, 0), 10.8f, 0).transform.SetParent(art);
            AddSprite("丘と木", T + "CR_Hills.png", new Vector2(0, -2.2f), 6.4f, 1).transform.SetParent(art);
            AddSprite("GameSample (額縁のように右上)", "Assets/Sasaki/Image/Title/FT7_GameSample.png", new Vector2(5.0f, 2.7f), 3.8f, 2).transform.SetParent(art);
            AddSprite("風船", T + "CR_Balloons.png", new Vector2(-6.4f, 1.4f), 6f, 3).transform.SetParent(art);
            AddSprite("花畑", T + "CR_Flowers.png", new Vector2(0, -3.3f), 4.2f, 4).transform.SetParent(art);
            AddSprite("味方キャラ (中央手前)", "Assets/Sprites/Images/味方キャラ_正面.png", new Vector2(-0.6f, -1.3f), 6f, 5).transform.SetParent(art);

            var world = new GameObject("MonochromeWorld").AddComponent<MonochromeWorld>();

            var areasRoot = new GameObject("ColorRestoreAreas").transform;
            var a1 = AddArea("Area1 キャラから広がる", "Mask_Splash", new Vector2(-0.6f, -1.2f), 0f, new Vector2(1.3f, 1.3f), 1.8f);
            var a2 = AddArea("Area2 筆でなぞる", "Mask_Stroke", new Vector2(-3.6f, 2.9f), -8f, new Vector2(1.4f, 1.6f), 2.0f);
            a2.strokeStretch = 6f; a2.noiseScale = 0.4f;
            var a3 = AddArea("Area3 右下が咲く", "Mask_Bloom", new Vector2(5.0f, -2.2f), 0f, new Vector2(0.6f, 0.6f), 2.2f);
            var tmpl = AddArea("ClickTemplate (クリック用の雛形)", "Mask_Splash", Vector2.zero, 0f, new Vector2(0.7f, 0.7f), 1.6f);
            tmpl.gameObject.SetActive(false);
            foreach (var a in new[] { a1, a2, a3, tmpl }) a.transform.SetParent(areasRoot, true);

            var demo = new GameObject("ColorRestoreDemo").AddComponent<ColorRestoreDemo>();
            demo.world = world;
            demo.areas = new[] { a1, a2, a3 };
            demo.clickTemplate = tmpl;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            log.AppendLine($"検証シーンを作成: {ScenePath}");
            return scene;
        }

        /// <summary>カメラを RenderTexture に描画して PNG に保存（動作確認用）。</summary>
        public static void Capture(Camera cam, string file)
        {
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
