using Retrolight.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using Retrolight.Runtime.Passes;
using Retrolight.Util;

namespace Retrolight.Runtime {
    public sealed class Retrolight : RenderPipeline {
        internal RenderGraph RenderGraph { get; private set; }
        internal readonly ShaderBundle ShaderBundle;
        internal readonly int PixelRatio;
        internal FrameData FrameData { get; private set; }

        //render passes
        private SetupPass SetupPass;
        private GBufferPass GBufferPass;
        private LightingPass LightingPass;
        private TransparentPass TransparentPass;
        private FinalPass FinalPass;

        public Retrolight(ShaderBundle shaderBundle, int pixelRatio) {
            //todo: enable SRP batcher, other graphics settings like linear light intensity
            RenderGraph = new RenderGraph("Retrolight Render Graph");
            ShaderBundle = shaderBundle;
            PixelRatio = pixelRatio;

            SetupPass = new SetupPass(this);
            GBufferPass = new GBufferPass(this);
            LightingPass = new LightingPass(this);
            TransparentPass = new TransparentPass(this);
            FinalPass = new FinalPass(this);

            Blitter.Initialize(shaderBundle.BlitShader, shaderBundle.BlitWithDepthShader);
            RTHandles.Initialize(Screen.width / PixelRatio, Screen.height / PixelRatio);
            RTHandles.ResetReferenceSize(Screen.width / PixelRatio, Screen.height / PixelRatio);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            BeginFrameRendering(context, cameras);
            foreach (var camera in cameras) {
                BeginCameraRendering(context, camera);
                RenderCamera(context, camera);
                EndCameraRendering(context, camera);
            }
            RenderGraph.EndFrame();
            EndFrameRendering(context, cameras);
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera) {
            if (!camera.TryGetCullingParameters(out var cullingParams)) return;
            CullingResults cull = context.Cull(ref cullingParams);
            
            RTHandles.SetReferenceSize(camera.pixelWidth / PixelRatio, camera.pixelHeight / PixelRatio);
            var viewportParams = new ViewportParams(RTHandles.rtHandleProperties);
            FrameData = new FrameData(camera, cull, viewportParams);
            
            using var snapContext = SnappingUtility.Snap(camera, camera.transform, viewportParams);

            context.SetupCameraProperties(camera);

            CommandBuffer cmd = CommandBufferPool.Get("Execute Retrolight Render Graph");
            var renderGraphParams = new RenderGraphParameters {
                scriptableRenderContext = context,
                commandBuffer = cmd,
                currentFrameIndex = Time.frameCount,
            };
            using (RenderGraph.RecordAndExecute(renderGraphParams)) {
                RenderPasses(snapContext.ViewportShift);
            }

            if (camera.clearFlags == CameraClearFlags.Skybox) {
                context.DrawSkybox(camera);
            }
            context.ExecuteCommandBuffer(cmd);/*
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);*/
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        private void RenderPasses(Vector2 viewportShift) {
            SetupPass.Run();
            var gBuffer = GBufferPass.Run();
            var finalColorTex = LightingPass.Run(gBuffer);
            TransparentPass.Run(gBuffer, finalColorTex);
            //PostProcessPass -> writes to final color buffer after all other shaders
            FinalPass.Run(finalColorTex, viewportShift);
        }

        protected override void Dispose(bool disposing) {
            if (!disposing) return;

            SetupPass.Dispose();
            GBufferPass.Dispose();
            LightingPass.Dispose();
            TransparentPass.Dispose();
            FinalPass.Dispose();

            SetupPass = null;
            GBufferPass = null;
            LightingPass = null;
            TransparentPass = null;
            FinalPass = null;
            

            Blitter.Cleanup();
            RenderGraph.Cleanup();
            RenderGraph = null;
        }
    }
}