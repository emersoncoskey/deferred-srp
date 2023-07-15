using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Overrides {
    [Serializable, VolumeComponentMenuForRenderPipeline("PostProcessing/Bloom", typeof(Retrolight))]
    public class Bloom : VolumeComponent {
        [Serializable]
        public enum BloomMode { Additive, Scattering } //todo: does scattering make sense for SDR? should it be a "prefer" style of option?

        [Serializable]
        public class BloomModeParameter : VolumeParameter<BloomMode> {
            public BloomModeParameter(BloomMode value, bool overrideState = false) : base(value, overrideState) { }
        }

        [Header("Bloom")] 
        public BloomModeParameter mode = new BloomModeParameter(BloomMode.Additive);
        public BoolParameter highQuality = new BoolParameter(false);
        public MinFloatParameter intensity = new MinFloatParameter(0, 0);
        public MinFloatParameter threshold = new MinFloatParameter(0.8f, 0);
        public ClampedFloatParameter knee = new ClampedFloatParameter(0, 0, 1);
        public ClampedIntParameter maxIterations = new ClampedIntParameter(3, 1, 16);
        public MinIntParameter downscaleLimit = new MinIntParameter(1, 1);
    }
} 