using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

// NativeGalleryはモバイル専用機能のため、using宣言も囲みます
#if UNITY_ANDROID || UNITY_IOS
#endif

public class MealLogger : MonoBehaviour
{
    // --- Unityエディタから設定する項目 ---
    [Header("UIパーツ")]
    [SerializeField] private Button selectImageButton;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject loadingIndicator;

    // --- 内部で使う変数 ---
    private Texture2D selectedImageTexture;

    // --- ダミーデータ用のクラス定義 ---
    [System.Serializable]
    private class FoodData
    {
        public string food_name;
        public int calories;
        public int protein;
        public int fat;
        public int carbs;
    }

    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        selectImageButton.onClick.AddListener(PickImageFromGallery);
#else
        selectImageButton.interactable = false;
        resultText.text = "この機能はAndroidまたはiOSデバイスでのみ利用可能です。";
#endif
        loadingIndicator.SetActive(false);
    }

#if UNITY_ANDROID || UNITY_IOS
    private void PickImageFromGallery()
    {
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

            selectedImageTexture = NativeGallery.LoadImageAtPath(path, 1024, false);
            if (selectedImageTexture == null)
            {
                resultText.text = "画像の読み込みに失敗しました。";
                return;
            }

            photoPreview.texture = selectedImageTexture;
            photoPreview.color = Color.white;

            // ▼▼▼【変更点】API通信の代わりに、ダミーデータを表示するコルーチンを呼び出します ▼▼▼
            StartCoroutine(ShowDummyData());

        }, "食事の写真を選択");
    }
#endif

    // ▼▼▼【変更点】API通信の代わりに、ロード画面を少し表示してから定型文を出す処理 ▼▼▼
    private IEnumerator ShowDummyData()
    {
        // 1. 解析中の表示を出す
        resultText.text = "AIが解析中です...";
        loadingIndicator.SetActive(true);

        // 2. 解析しているように見せるため、1.5秒待つ
        yield return new WaitForSeconds(1.5f);

        // 3. ローディングを終了し、ダミーデータを作成
        loadingIndicator.SetActive(false);
        FoodData dummyFoodData = new FoodData
        {
            food_name = "豚の生姜焼き",
            calories = 580,
            protein = 25,
            fat = 35,
            carbs = 40
        };

        // 4. ダミーデータを画面に表示
        resultText.text = $"料理名: {dummyFoodData.food_name}\n" +
                          $"カロリー: {dummyFoodData.calories} kcal\n" +
                          $"タンパク質: {dummyFoodData.protein} g\n" +
                          $"脂質: {dummyFoodData.fat} g\n" +
                          $"炭水化物: {dummyFoodData.carbs} g";
    }
}