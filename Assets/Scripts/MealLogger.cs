using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

// 'using NativeGallery;' はクラス名なので不要。削除しました。

public class MealLogger : MonoBehaviour
{
    // --- Unityエディタから設定する項目 ---
    [Header("UIパーツ")]
    [SerializeField] private Button selectImageButton;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("API設定")]
    [SerializeField] private string geminiApiKey; // YOUR_GEMINI_API_KEY

    // --- 内部で使う変数 ---
    private Texture2D selectedImageTexture;

    // --- APIレスポンスを格納するためのクラス定義 ---
    [System.Serializable] private class GeminiResponse { public Candidate[] candidates; }
    [System.Serializable] private class Candidate { public Content content; }
    [System.Serializable] private class Content { public Part[] parts; }
    [System.Serializable] private class Part { public string text; }
    [System.Serializable] private class FoodData { public string food_name; public int calories; public int protein; public int fat; public int carbs; }

    void Start()
    {
        // NativeGalleryはモバイル専用機能なので、PCエディタなどでエラーが出ないように
        // #ifディレクティブで、AndroidかiOSの時だけボタンが機能するように設定します。
#if UNITY_ANDROID || UNITY_IOS
        selectImageButton.onClick.AddListener(PickImageFromGallery);
#else
        // モバイル以外ではボタンを無効化し、メッセージを表示
        selectImageButton.interactable = false;
        resultText.text = "この機能はAndroidまたはiOSデバイスでのみ利用可能です。";
#endif
        loadingIndicator.SetActive(false);
    }

    // このメソッド全体も、モバイルプラットフォームでのみコンパイルされるように囲みます。
#if UNITY_ANDROID || UNITY_IOS
    /// <summary>
    /// ギャラリーから画像を選択する処理
    /// </summary>
    private void PickImageFromGallery()
    {
        // GetImageFromGalleryが権限要求も実行してくれます
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (selectedImageTexture != null)
            {
                Destroy(selectedImageTexture);
            }

            // CPUからアクセスできるように、markAsNonReadableをfalseに設定
            selectedImageTexture = NativeGallery.LoadImageAtPath(path, 1024, false);
            if (selectedImageTexture == null)
            {
                resultText.text = "画像の読み込みに失敗しました。";
                return;
            }

            photoPreview.texture = selectedImageTexture;
            photoPreview.color = Color.white;

            resultText.text = "AIが解析中です...";
            loadingIndicator.SetActive(true);

            StartCoroutine(UploadToGemini(selectedImageTexture));

        }, "食事の写真を選択");
    }
#endif

    private IEnumerator UploadToGemini(Texture2D image)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + geminiApiKey;
        byte[] imageData = image.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageData);
        string prompt = "この画像に写っている食べ物を分析し、料理名、総カロリー、PFCバランス（タンパク質、脂質、炭水化物）を日本語で教えてください。回答は必ず以下のJSON形式でお願いします: {\\\"food_name\\\": \\\"料理名\\\", \\\"calories\\\": カロリー数, \\\"protein\\\": タンパク質グラム数, \\\"fat\\\": 脂質グラム数, \\\"carbs\\\": 炭水化物グラム数}";
        string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{prompt}\"}},{{\"inline_data\":{{\"mime_type\":\"image/jpeg\",\"data\":\"{base64Image}\"}}}}]}}]}}";

        // --- ▼▼▼ リトライ処理の追加 ▼▼▼ ---
        int maxRetries = 3; // 最大3回まで試行
        float retryDelay = 2.0f; // 失敗したら2秒待つ

        for (int i = 0; i < maxRetries; i++)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                // 503エラー以外で失敗したか、または成功した場合
                if (webRequest.responseCode != 503 || webRequest.result == UnityWebRequest.Result.Success)
                {
                    // ここでループを抜けて、通常の結果処理に進む
                    HandleApiResponse(webRequest);
                    yield break;
                }

                // 503エラーで、まだリトライ回数が残っている場合
                Debug.LogWarning($"API is overloaded. Retrying in {retryDelay} seconds... (Attempt {i + 1}/{maxRetries})");
                yield return new WaitForSeconds(retryDelay);
            }
        }
        // --- ▲▲▲ リトライ処理ここまで ▲▲▲ ---

        // すべてのリトライが失敗した場合
        Debug.LogError("API failed after all retries.");
        resultText.text = "サーバーが混み合っています。しばらくしてからお試しください。";
        loadingIndicator.SetActive(false);
    }

    // レスポンス処理を別のメソッドに分離
    private void HandleApiResponse(UnityWebRequest webRequest)
    {
        loadingIndicator.SetActive(false);
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("APIエラー: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
            resultText.text = "解析エラーが発生しました。";
        }
        else
        {
            ParseAndDisplayResponse(webRequest.downloadHandler.text);
        }
    }

    private void ParseAndDisplayResponse(string jsonResponse)
    {
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
            string foodJson = geminiResponse.candidates[0].content.parts[0].text;

            if (foodJson.StartsWith("```json"))
            {
                foodJson = foodJson.Substring(7, foodJson.Length - 7 - 3).Trim();
            }

            FoodData foodData = JsonUtility.FromJson<FoodData>(foodJson);

            resultText.text = $"料理名: {foodData.food_name}\n" +
                              $"カロリー: {foodData.calories} kcal\n" +
                              $"タンパク質: {foodData.protein} g\n" +
                              $"脂質: {foodData.fat} g\n" +
                              $"炭水化物: {foodData.carbs} g";
        }
        catch (System.Exception e)
        {
            Debug.LogError("JSONのパースに失敗しました: " + e.Message + "\nRaw Response: " + jsonResponse);
            resultText.text = "結果の解析に失敗しました。";
        }
    }
}
