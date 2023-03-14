using Retrolight.Data;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace Retrolight.Runtime.Passes {
    public class TransparentPass : RenderPass<TransparentPass.TransparentPassData> {
        private static readonly ShaderTagId transparentPass = new ShaderTagId("RetrolightTransparent");

        public class TransparentPassData {
            public RendererListHandle TransparentRendererList;
        }

        public TransparentPass(Retrolight pipeline) : base(pipeline) { }

        protected override string PassName => "Transparent Pass";

        public void Run(GBuffer gBuffer, LightInfo lightInfo, LightingData lightingData) {
            using var builder = CreatePass(out var passData);

            gBuffer.ReadAll(builder);
            lightInfo.ReadAll(builder);
            lightingData.ReadAll(builder);

            RendererListDesc transparentRendererDesc = new RendererListDesc(transparentPass, cull, camera) {
                sortingCriteria = SortingCriteria.CommonTransparent,
                renderQueueRange = RenderQueueRange.transparent
            };
            passData.TransparentRendererList = renderGraph.CreateRendererList(transparentRendererDesc);
        }

        protected override void Render(TransparentPassData passData, RenderGraphContext context) {
            context.cmd.DrawRendererList(passData.TransparentRendererList);
        }
    }
}