using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LastTower.ColorRestore
{
    /// <summary>
    /// Renderer2D に1回だけ追加しておく描画機能。
    /// シーンに MonochromeWorld があるときだけ動き、画面を白黒にして ColorRestoreArea の所だけ色を戻す。
    /// スプライトが何枚重なっていても、カメラに映った最終的な絵に対して処理するので関係ない。
    /// </summary>
    public class ColorRestoreFeature : ScriptableRendererFeature
    {
        public Shader compositeShader;
        public Shader stampShader;
        [Tooltip("白黒処理を入れるタイミング。BeforeRenderingPostProcessing ならブルーム等はカラーのまま後から掛かる")]
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Tooltip("マスクの解像度（画面に対する倍率）。0.5 にすると軽くなる")]
        [Range(0.25f, 1f)] public float maskResolutionScale = 1f;

        Material _compositeMat;
        Material _stampMat;
        ColorRestorePass _pass;

        public override void Create()
        {
            _pass = new ColorRestorePass { renderPassEvent = passEvent, requiresIntermediateTexture = true };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var world = MonochromeWorld.Active;
            if (world == null || !world.isActiveAndEnabled) return;

            CameraType type = renderingData.cameraData.cameraType;
            if (type != CameraType.Game && type != CameraType.SceneView) return;

            if (!EnsureMaterials()) return;

            _pass.renderPassEvent = passEvent;
            _pass.Setup(_compositeMat, _stampMat, world, maskResolutionScale);
            renderer.EnqueuePass(_pass);
        }

        bool EnsureMaterials()
        {
            if (compositeShader == null) compositeShader = Shader.Find("Hidden/LastTower/ColorRestoreComposite");
            if (stampShader == null) stampShader = Shader.Find("Hidden/LastTower/ColorRestoreStamp");
            if (compositeShader == null || stampShader == null) return false;
            if (_compositeMat == null) _compositeMat = CoreUtils.CreateEngineMaterial(compositeShader);
            if (_stampMat == null) _stampMat = CoreUtils.CreateEngineMaterial(stampShader);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_compositeMat);
            CoreUtils.Destroy(_stampMat);
            _compositeMat = null;
            _stampMat = null;
        }

        class ColorRestorePass : ScriptableRenderPass
        {
            Material _compositeMat;
            Material _stampMat;
            MonochromeWorld _world;
            float _maskScale;
            Mesh _quad;
            readonly List<MaterialPropertyBlock> _mpbPool = new List<MaterialPropertyBlock>();

            class MaskPassData
            {
                public Matrix4x4 viewProj;
                public Mesh quad;
                public Material stampMat;
                public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
                public readonly List<MaterialPropertyBlock> props = new List<MaterialPropertyBlock>();
            }

            class CompositePassData
            {
                public TextureHandle source;
                public Material material;
            }

            public void Setup(Material composite, Material stamp, MonochromeWorld world, float maskScale)
            {
                _compositeMat = composite;
                _stampMat = stamp;
                _world = world;
                _maskScale = maskScale;
                if (_quad == null) _quad = CreateQuad();
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (resourceData.isActiveTargetBackBuffer) return; // 中間テクスチャが無いと処理できない

                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc srcDesc = renderGraph.GetTextureDesc(source);

                // ---- 1. マスク（色を戻す所 = 白）を描く ----
                var maskDesc = new TextureDesc(
                    Mathf.Max(1, Mathf.RoundToInt(srcDesc.width * _maskScale)),
                    Mathf.Max(1, Mathf.RoundToInt(srcDesc.height * _maskScale)))
                {
                    format = MaskFormat(),
                    clearBuffer = true,
                    clearColor = Color.black,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "_ColorRestoreMask",
                };
                TextureHandle mask = renderGraph.CreateTexture(maskDesc);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("ColorRestore Mask", out var data))
                {
                    data.viewProj = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true) * cameraData.GetViewMatrix();
                    data.quad = _quad;
                    data.stampMat = _stampMat;
                    data.matrices.Clear();
                    data.props.Clear();

                    var areas = ColorRestoreArea.ActiveAreas;
                    for (int i = 0; i < areas.Count; i++)
                    {
                        var area = areas[i];
                        if (area == null || !area.ShouldDraw) continue;
                        if (_mpbPool.Count <= data.props.Count) _mpbPool.Add(new MaterialPropertyBlock());
                        var mpb = _mpbPool[data.props.Count];
                        mpb.Clear();
                        area.FillProperties(mpb);
                        data.matrices.Add(area.GetQuadMatrix());
                        data.props.Add(mpb);
                    }

                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(mask, ShaderIds.MaskTex); // 合成パス（や自作シェーダー）から _CR_MaskTex で読める
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((MaskPassData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalMatrix(ShaderIds.MatrixVP, d.viewProj);
                        for (int i = 0; i < d.matrices.Count; i++)
                            ctx.cmd.DrawMesh(d.quad, d.matrices[i], d.stampMat, 0, 0, d.props[i]);
                    });
                }

                // ---- 2. 画面を白黒にして、マスクの所だけ元の色で合成 ----
                var w = _world;
                _compositeMat.SetFloat(ShaderIds.GrayAmount, w.grayAmount);
                _compositeMat.SetFloat(ShaderIds.GlobalRestore, w.globalRestore);
                _compositeMat.SetColor(ShaderIds.GrayTint, w.grayTint);
                _compositeMat.SetFloat(ShaderIds.Contrast, w.contrast);
                _compositeMat.SetFloat(ShaderIds.Brightness, w.brightness);
                _compositeMat.SetColor(ShaderIds.EdgeColor, w.edgeColor);
                _compositeMat.SetFloat(ShaderIds.EdgeIntensity, w.edgeIntensity);
                _compositeMat.SetFloat(ShaderIds.Invert, w.invert ? 1f : 0f);

                var dstDesc = srcDesc;
                dstDesc.name = "_ColorRestoreColor";
                dstDesc.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(dstDesc);

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("ColorRestore Composite", out var data))
                {
                    data.source = source;
                    data.material = _compositeMat;
                    builder.UseTexture(source);
                    builder.UseTexture(mask);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((CompositePassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, 0);
                    });
                }

                resourceData.cameraColor = destination;
            }

            static GraphicsFormat MaskFormat()
            {
                if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8_UNorm, GraphicsFormatUsage.Blend))
                    return GraphicsFormat.R8G8_UNorm;   // R=色の戻り具合, G=広がっている先端
                return GraphicsFormat.R8G8B8A8_UNorm;
            }

            static Mesh CreateQuad()
            {
                var m = new Mesh { name = "ColorRestoreQuad", hideFlags = HideFlags.HideAndDontSave };
                m.SetVertices(new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) });
                m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
                m.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
                m.bounds = new Bounds(Vector3.zero, new Vector3(1, 1, 1));
                return m;
            }
        }
    }
}
