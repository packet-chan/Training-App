// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// ▼▼▼【変更点①】Mediapipe.Tasks...のusing宣言は、メソッド引数でフルネーム指定するため、ここでは不要です ▼▼▼
// using Mediapipe.Tasks.Vision.PoseLandmarker;
// using PoseLandmarkerResult = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerResult;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    public class PoseLandmarkerRunner : VisionTaskApiRunner<Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarker>
    {
        [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;

        // ▼▼▼【変更点②】HandRaiseCounterへの参照を追加します ▼▼▼
        public HandRaiseCounter handRaiseCounter;

        private Experimental.TextureFramePool _textureFramePool;

        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        protected override IEnumerator Run()
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Model = {config.ModelName}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumPoses = {config.NumPoses}");
            Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
            Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
            Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetPoseLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);
            taskApi = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

            screen.Initialize(imageSource);

            SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
            _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally;
            var flipVertically = transformationOptions.flipVertically;

            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();

            // ▼▼▼【変更点③】result変数の型をフルネームで指定します ▼▼▼
            var result = Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage)
                        {
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;

                        if (req.hasError)
                        {
                            Debug.LogWarning($"Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        DisposeAllMasks(result);
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _poseLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        DisposeAllMasks(result);
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        // ▼▼▼【変更点④】メソッドの引数で使う型をフルネームで指定します ▼▼▼
        private void OnPoseLandmarkDetectionOutput(Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerResult result, Image image, long timestamp)
        {
            _poseLandmarkerResultAnnotationController.DrawLater(result);

            if (handRaiseCounter != null)
            {
                if (result.poseLandmarks != null && result.poseLandmarks.Count > 0)
                {
                    handRaiseCounter.OnPoseLandmarksOutput(result.poseLandmarks[0]);
                }
            }

            DisposeAllMasks(result);
        }

        // ▼▼▼【変更点⑤】こちらのメソッドの引数もフルネームで指定します ▼▼▼
        private void DisposeAllMasks(Mediapipe.Tasks.Vision.PoseLandmarker.PoseLandmarkerResult result)
        {
            if (result.segmentationMasks != null)
            {
                foreach (var mask in result.segmentationMasks)
                {
                    mask.Dispose();
                }
            }
        }
    }
}