using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

// Native Galleryを正しく使うために、このusingディレクティブが必須です
#if UNITY_ANDROID || UNITY_IOS
using NativeGallery;
#endif

public class MealLogger : MonoBehaviour
{
    // --- Unityエディタから設定する項目 ---
    [Header("UIパーツ")]
    [SerializeField] private Button selectImageButton;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("API設定")]
    [SerializeField] private string geminiApiKey = "YOUR_GEMINI_API_KEY"; // ここに自分のAPIキーを入力

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
        selectImageButton.onClick.AddListener(PickImageFromGallery);
        loadingIndicator.SetActive(false);
    }

    /// <summary>
    /// ギャラリーから画像を選択する処理 (公式サンプル準拠版)
    /// </summary>
    private void PickImageFromGallery()
    {
        // GetImageFromGalleryが権限要求も実行してくれる
        NativeGallery.GetImageFromGallery((path) =>
        {
            // ユーザーが画像を選択せずに閉じた場合は、pathはnullになる
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("画像が選択されませんでした。");
                return;
            }

            Debug.Log("選択された画像のパス: " + path);

            // 前回のテクスチャがあれば破棄
            if (selectedImageTexture != null)
            {
                Destroy(selectedImageTexture);
            }

            // 選択された画像をテクスチャとして読み込み、プレビューに表示
            selectedImageTexture = NativeGallery.LoadImageAtPath(path, 1024, false); // 最大サイズを1024pxに制限
            if (selectedImageTexture == null)
            {
                resultText.text = "画像の読み込みに失敗しました。";
                return;
            }

            photoPreview.texture = selectedImageTexture;
            photoPreview.color = Color.white; // 画像を表示するためにRawImageを不透明にする

            resultText.text = "AIが解析中です...";
            loadingIndicator.SetActive(true);

            // AIに画像を送信するコルーチンを開始
            StartCoroutine(UploadToGemini(selectedImageTexture));

        }, "食事の写真を選択");
    }

    private IEnumerator UploadToGemini(Texture2D image)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + geminiApiKey;
        byte[] imageData = image.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageData);
        string prompt = "この画像に写っている食べ物を分析し、料理名、総カロリー、PFCバランス（タンパク質、脂質、炭水化物）を日本語で教えてください。回答は必ず以下のJSON形式でお願いします: {\\\"food_name\\\": \\\"料理名\\\", \\\"calories\\\": カロリー数, \\\"protein\\\": タンパク質グラム数, \\\"fat\\\": 脂質グラム数, \\\"carbs\\\": 炭水化物グラム数}";
        string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{prompt}\"}},{{\"inline_data\":{{\"mime_type\":\"image/jpeg\",\"data\":\"{base64Image}\"}}}}]}}]}}";

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

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