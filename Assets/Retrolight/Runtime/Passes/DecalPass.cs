using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace Retrolight.Runtime.Passes {
    public class DecalPass : RenderPass<DecalPass.DecalPassData> {
        public class DecalPassData {
            public RendererListHandle DecalRendererList;
        }

        public DecalPass(Retrolight pipeline) : base(pipeline) { }

        protected override string PassName => "Decal Pass";

        public void Run(GBuffer gBuffer) {
            using var builder = CreatePass(out var passData);

            gBuffer.ReadAll(builder);
            builder.UseColorBuffer(gBuffer.Albedo, 0);
            builder.UseDepthBuffer(gBuffer.Depth, DepthAccess.Read);
            builder.UseColorBuffer(gBuffer.Normal, 1);
            builder.UseColorBuffer(gBuffer.Attributes, 2);

            var decalRendererDesc = new RendererListDesc(Constants.DecalPassId, cull, camera) {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque
            };
            var decalRendererHandle = renderGraph.CreateRendererList(decalRendererDesc);
            passData.DecalRendererList = builder.UseRendererList(decalRendererHandle);
        }

        protected override void Render(DecalPassData passData, RenderGraphContext context) {
            context.cmd.DrawRendererList(passData.DecalRendererList);
        }
    }
}