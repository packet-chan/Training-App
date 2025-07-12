// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Mediapipe.Unity
{
    public class StreamingAssetsResourceManager : IResourceManager
    {
        private static readonly string _TAG = nameof(StreamingAssetsResourceManager);

        private static string _RelativePath;
        private static string _AssetPathRoot;
        private static string _CachePathRoot;

        public StreamingAssetsResourceManager(string path)
        {
            ResourceUtil.EnableCustomResolver();
            _RelativePath = path;
            _AssetPathRoot = Path.Combine(Application.streamingAssetsPath, _RelativePath);
            _CachePathRoot = Path.Combine(Application.persistentDataPath, _RelativePath);

            // ★★★【デバッグログ①】初期化時にどのパスが設定されたかを確認
            Debug.Log($"[Grip Debug] ResourceManager initialized. AssetPathRoot is set to: {_AssetPathRoot}");
        }

        public StreamingAssetsResourceManager() : this("") { }

        IEnumerator IResourceManager.PrepareAssetAsync(string name, string uniqueKey, bool overwriteDestination)
        {
            var destFilePath = GetCachePathFor(uniqueKey);
            ResourceUtil.SetAssetPath(name, destFilePath);

            if (File.Exists(destFilePath) && !overwriteDestination)
            {
                Logger.LogInfo(_TAG, $"{name} will not be copied to {destFilePath} because it already exists");
                yield break;
            }

            var sourceFilePath = GetCachePathFor(name);
            if (!File.Exists(sourceFilePath))
            {
                // CreateCacheFileを呼び出す前に、どのファイルを準備しようとしているかログに出力
                Debug.Log($"[Grip Debug] Preparing to create cache for asset: {name}");
                yield return CreateCacheFile(name);
            }

            if (sourceFilePath == destFilePath)
            {
                yield break;
            }

            Logger.LogVerbose(_TAG, $"Copying {sourceFilePath} to {destFilePath}...");
            File.Copy(sourceFilePath, destFilePath, overwriteDestination);
            Logger.LogVerbose(_TAG, $"{sourceFilePath} is copied to {destFilePath}");
        }

        private IEnumerator CreateCacheFile(string assetName)
        {
            var cacheFilePath = GetCachePathFor(assetName);

            if (File.Exists(cacheFilePath))
            {
                yield break;
            }

#if !UNITY_ANDROID && !UNITY_WEBGL
      throw new FileNotFoundException($"{cacheFilePath} is not found");
#else
            var assetPath = GetAssetPathFor(assetName);

            // ★★★【デバッグログ②】これが最も重要！実際に読み込もうとしているパスをログに出力
            Debug.Log($"[Grip Debug] Attempting to load asset from path: {assetPath}");

            using (var webRequest = UnityWebRequest.Get(assetPath))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    if (!Directory.Exists(_CachePathRoot))
                    {
                        var _ = Directory.CreateDirectory(_CachePathRoot);
                    }
                    Logger.LogVerbose(_TAG, $"Writing {assetName} data to {cacheFilePath}...");
                    var bytes = webRequest.downloadHandler.data;
                    File.WriteAllBytes(cacheFilePath, bytes);
                    Logger.LogVerbose(_TAG, $"{assetName} is saved to {cacheFilePath} (length={bytes.Length})");

                    // ★★★【デバッグログ③】成功したことをログに出力
                    Debug.Log($"[Grip Debug] Successfully loaded and cached: {assetName}");
                }
                else
                {
                    // ★★★【デバッグログ④】失敗したことをエラーとしてログに出力
                    Debug.LogError($"[Grip Debug] Failed to load from path: {assetPath}. Error: {webRequest.error}");
                    throw new InternalException($"Failed to load {assetName}: {webRequest.error}");
                }
            }
#endif
        }

        private static string GetAssetPathFor(string assetName)
        {
            return Path.Combine(_AssetPathRoot, assetName);
        }

        private static string GetCachePathFor(string assetName)
        {
            var assetPath = GetAssetPathFor(assetName);
            return File.Exists(assetPath) ? assetPath : Path.Combine(_CachePathRoot, assetName);
        }
    }
}