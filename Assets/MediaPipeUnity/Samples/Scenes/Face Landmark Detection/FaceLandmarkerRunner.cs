// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
  public class FaceLandmarkerRunner : VisionTaskApiRunner<FaceLandmarker>
  {
    [SerializeField] private FaceLandmarkerResultAnnotationController _faceLandmarkerResultAnnotationController;
    [SerializeField] private FaceSync _faceSync;

    private Experimental.TextureFramePool _textureFramePool;

    public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
     /*
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumFaces = {config.NumFaces}");
      Debug.Log($"MinFaceDetectionConfidence = {config.MinFaceDetectionConfidence}");
      Debug.Log($"MinFacePresenceConfidence = {config.MinFacePresenceConfidence}");
      Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
      Debug.Log($"OutputFaceBlendshapes = {config.OutputFaceBlendshapes}");
      Debug.Log($"OutputFacialTransformationMatrixes = {config.OutputFacialTransformationMatrixes}");
      */

      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      var options = config.GetFaceLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnFaceLandmarkDetectionOutput : null);
      taskApi = FaceLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
      var imageSource = ImageSourceProvider.ImageSource;

      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Debug.LogError("Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);

      SetupAnnotationController(_faceLandmarkerResultAnnotationController, imageSource);

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();
      var result = FaceLandmarkerResult.Alloc(options.numFaces);

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
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
          yield return null;
          continue;
        }

        // Build the input Image
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
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
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
              _faceLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _faceLandmarkerResultAnnotationController.DrawNow(default);
            }
            break;
          case Tasks.Vision.Core.RunningMode.VIDEO:
            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
            {
              _faceLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _faceLandmarkerResultAnnotationController.DrawNow(default);
            }
            break;
          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }
      }
    }

    private void OnFaceLandmarkDetectionOutput(FaceLandmarkerResult result, Image image, long timestamp)
    {
      _faceLandmarkerResultAnnotationController.DrawLater(result);

      // ガード節
      if (_faceSync == null || result.faceBlendshapes == null || result.faceBlendshapes.Count == 0) return;

      var categories = result.faceBlendshapes[0].categories;

      // 生の数値を初期化
      float rawLeft = 0, rawRight = 0;
      float rawSmileLeft = 0, rawSmileRight = 0;
      float rawJawOpen = 0, rawBrowInnerUp = 0;
      float rawBrowDownLeft = 0, rawBrowDownRight = 0;
      float rawEyeWideLeft = 0, rawEyeWideRight = 0;

      // MediaPipeのデータから数値を抽出
      foreach (var category in categories)
      {
        switch (category.categoryName)
        {
          case "eyeBlinkLeft":    rawLeft = category.score; break;
          case "eyeBlinkRight":   rawRight = category.score; break;
          case "mouthSmileLeft":  rawSmileLeft = category.score; break;
          case "mouthSmileRight": rawSmileRight = category.score; break;
          case "jawOpen":         rawJawOpen = category.score; break;
          case "browInnerUp":     rawBrowInnerUp = category.score; break;
          case "browDownLeft":    rawBrowDownLeft = category.score; break;
          case "browDownRight":   rawBrowDownRight = category.score; break;
          case "eyeWideLeft":     rawEyeWideLeft = category.score; break;
          case "eyeWideRight":    rawEyeWideRight = category.score; break;
        }
      }

      // まばたき計算（デッドゾーン 0.25f, ウインク補正 0.15f, 感度 1.8倍）
      float left = rawLeft < 0.25f ? 0 : rawLeft;
      float right = rawRight < 0.25f ? 0 : rawRight;

      if (Mathf.Abs(left - right) > 0.15f)
      {
        if (left > right) right = 0; else left = 0;
      }
      left = Mathf.Clamp01(left * 1.8f);
      right = Mathf.Clamp01(right * 1.8f);

      // 各種表情の計算
      float smile = Mathf.Clamp01((rawSmileLeft + rawSmileRight) / 2f * 4.0f);
      float browUp = rawBrowInnerUp < 0.7f ? 0 : rawBrowInnerUp;
      float surprised = Mathf.Clamp01((browUp + (rawEyeWideLeft + rawEyeWideRight) / 2f) / 2f * 2.0f);
      float angry = Mathf.Clamp01((rawBrowDownLeft + rawBrowDownRight) / 2f * 4.0f);
      float mouth = rawJawOpen < 0.2f ? 0 : Mathf.Clamp01(rawJawOpen * 1.5f);

      // 笑顔による抑制を適用
      float blinkSuppression = smile > 0.5f ? 0f : 1.0f;

      // 頭の回転計算
      Quaternion targetRotation = Quaternion.identity; // デフォルトは正面
      if (result.facialTransformationMatrixes != null && result.facialTransformationMatrixes.Count > 0)
      {
          var matrix = result.facialTransformationMatrixes[0];
          Vector3 forward = new Vector3(matrix.m02, matrix.m12, matrix.m22);
          Vector3 up = new Vector3(matrix.m01, matrix.m11, matrix.m21);
          if (forward != Vector3.zero && up != Vector3.zero)
          {
              targetRotation = Quaternion.LookRotation(forward, up);
          }
          //使いやすくミラー状態に
          Vector3 euler = targetRotation.eulerAngles;
              targetRotation = Quaternion.Euler(-euler.x, -euler.y, -euler.z);
      }
      // アバターへ反映
      _faceSync.UpdateMouth(mouth);
      _faceSync.UpdateBlink(left * blinkSuppression, right * blinkSuppression);
      _faceSync.UpdateExpression(smile, surprised, angry);
      _faceSync.UpdateRotation(targetRotation);

      // デバッグログ（100ms毎）
      if (timestamp % 100 == 0)
      {
        Debug.Log($"[FaceSync] Smile:{smile:F2} Surprised:{surprised:F2} Jaw:{rawJawOpen:F2}");
      }
    }
  }
}
