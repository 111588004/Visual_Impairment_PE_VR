using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

#pragma warning disable 0672 // Suppress obsolete member overrides for URP 6 Compatibility

public class GlaucomaBlurFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(0, 5)] public int downsample = 2;
        [Range(0.0f, 10.0f)] public float blurSize = 2.0f;
        [Range(1, 4)] public int iterations = 3;
    }

    public Settings settings = new Settings();
    private GlaucomaBlurPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new GlaucomaBlurPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;
        m_ScriptablePass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class GlaucomaBlurPass : ScriptableRenderPass
    {
        private Settings settings;
        private Material blurMaterial;
        private RTHandle tempTexture1;
        private RTHandle tempTexture2;
        private int globalBlurTexId = Shader.PropertyToID("_GlaucomaBlurTex");

        private class PassData
        {
            public TextureHandle tempTex1;
            public int globalBlurTexId;
        }

        public GlaucomaBlurPass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            Shader shader = Shader.Find("Hidden/GlaucomaBlur");
            if (shader != null)
                blurMaterial = new Material(shader);
        }

        // ─── Render Graph Path (Unity 6 / URP 17+) ───────────────────────────
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType == CameraType.Preview) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.width  /= (1 << settings.downsample);
            desc.height /= (1 << settings.downsample);
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle tempTex1 = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, desc, "_TempBlur1", false, FilterMode.Bilinear);

            // Use AddUnsafePass so we can both SetRenderTarget AND SetGlobalTexture
            using (var builder = renderGraph.AddUnsafePass<PassData>("GlaucomaBlurPass", out var passData))
            {
                passData.tempTex1        = tempTex1;
                passData.globalBlurTexId = globalBlurTexId;

                builder.UseTexture(tempTex1, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    // Get the native CommandBuffer from the unsafe context
                    CommandBuffer natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    // Expose the texture globally so GlaucomaFieldGenerator shader can sample it.
                    // Never SetRenderTarget here: this runs inside an unsafe pass, and switching
                    // the target without restoring it desyncs RenderGraph from the camera's real
                    // color target, which silently blanks the camera output.
                    natCmd.SetGlobalTexture(data.globalBlurTexId, data.tempTex1);
                });
            }
        }

        // ─── Legacy / Compatibility Mode Path ────────────────────────────────
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var desc = cameraTextureDescriptor;
            desc.width  /= (1 << settings.downsample);
            desc.height /= (1 << settings.downsample);
            desc.depthBufferBits = 0;

            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTexture1, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempBlur1");
            RenderingUtils.ReAllocateHandleIfNeeded(ref tempTexture2, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempBlur2");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blurMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("GlaucomaBlurPass");

            cmd.SetGlobalTexture(globalBlurTexId, tempTexture1);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose()
        {
            tempTexture1?.Release();
            tempTexture2?.Release();
        }
    }
}
