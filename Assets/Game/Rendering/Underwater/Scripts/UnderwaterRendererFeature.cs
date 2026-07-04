using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

#if URP_COMPATIBILITY_MODE
using UnityEngine.Experimental.Rendering;
#endif

namespace GameJam.Rendering.Underwater
{
    public sealed class UnderwaterRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        Material underwaterMaterial;

        [SerializeField]
        RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField]
        bool gameCamerasOnly = true;

        [SerializeField]
        [Tooltip("Si no hay UnderwaterEffectController activo en la escena, el pass ni se encola. Como apagar la alberca cuando nadie se mete.")]
        bool requireActiveController = true;

        UnderwaterRenderPass pass;

        public override void Create()
        {
            pass = new UnderwaterRenderPass();
            pass.renderPassEvent = injectionPoint;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (underwaterMaterial == null)
                return;

            if (requireActiveController && !UnderwaterEffectController.HasActiveController)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (gameCamerasOnly && cameraType != CameraType.Game)
                return;

            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
                return;

            pass.renderPassEvent = injectionPoint;
            pass.Setup(underwaterMaterial);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
        }

        sealed class UnderwaterRenderPass : ScriptableRenderPass
        {
            const string PassName = "Toon Underwater Fullscreen";

            Material material;

#if URP_COMPATIBILITY_MODE
            RTHandle temporaryColor;
#endif

            public UnderwaterRenderPass()
            {
                profilingSampler = new ProfilingSampler(PassName);

                // Necesitamos leer el color actual. URP crea intermedio cuando hace falta; barato, ordenado y sin magia negra.
                requiresIntermediateTexture = true;
            }

            public void Setup(Material passMaterial)
            {
                material = passMaterial;
            }

            public void Dispose()
            {
#if URP_COMPATIBILITY_MODE
                temporaryColor?.Release();
#endif
            }

#if URP_COMPATIBILITY_MODE
#pragma warning disable 618, 672
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.depthStencilFormat = GraphicsFormat.None;

                RenderingUtils.ReAllocateHandleIfNeeded(
                    ref temporaryColor,
                    descriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_ToonUnderwaterTempColor");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                    return;

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                CommandBuffer cmd = CommandBufferPool.Get(PassName);

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    Blitter.BlitCameraTexture(cmd, source, temporaryColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 0);
                    Blitter.BlitCameraTexture(cmd, temporaryColor, source);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore 618, 672
#endif

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (cameraData.camera.cameraType != CameraType.Game && cameraData.camera.cameraType != CameraType.SceneView)
                    return;

                if (resourceData.isActiveTargetBackBuffer)
                {
                    Debug.LogWarning("UnderwaterRendererFeature skipped because the active target is the backbuffer. Move the injection point earlier.");
                    return;
                }

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                    return;

                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "_ToonUnderwaterColor";
                destinationDescriptor.clearBuffer = false;

                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);
                RenderGraphUtils.BlitMaterialParameters blitParameters = new(source, destination, material, 0);

                renderGraph.AddBlitPass(blitParameters, PassName);

                // Dejamos el color procesado como camara principal; asi no hacemos el blit de regreso porque la GPU ya tuvo suficiente drama.
                resourceData.cameraColor = destination;
            }
        }
    }
}
