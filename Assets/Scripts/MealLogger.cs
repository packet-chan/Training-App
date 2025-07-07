using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

// Native Gallery�𐳂����g�����߂ɁA����using�f�B���N�e�B�u���K�{�ł�
#if UNITY_ANDROID || UNITY_IOS
using NativeGallery;
#endif

public class MealLogger : MonoBehaviour
{
    // --- Unity�G�f�B�^����ݒ肷�鍀�� ---
    [Header("UI�p�[�c")]
    [SerializeField] private Button selectImageButton;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("API�ݒ�")]
    [SerializeField] private string geminiApiKey = "YOUR_GEMINI_API_KEY"; // �����Ɏ�����API�L�[�����

    // --- �����Ŏg���ϐ� ---
    private Texture2D selectedImageTexture;

    // --- API���X�|���X���i�[���邽�߂̃N���X��` ---
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
    /// �M�������[����摜��I�����鏈�� (�����T���v��������)
    /// </summary>
    private void PickImageFromGallery()
    {
        // GetImageFromGallery�������v�������s���Ă����
        NativeGallery.GetImageFromGallery((path) =>
        {
            // ���[�U�[���摜��I�������ɕ����ꍇ�́Apath��null�ɂȂ�
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("�摜���I������܂���ł����B");
                return;
            }

            Debug.Log("�I�����ꂽ�摜�̃p�X: " + path);

            // �O��̃e�N�X�`��������Δj��
            if (selectedImageTexture != null)
            {
                Destroy(selectedImageTexture);
            }

            // �I�����ꂽ�摜���e�N�X�`���Ƃ��ēǂݍ��݁A�v���r���[�ɕ\��
            selectedImageTexture = NativeGallery.LoadImageAtPath(path, 1024, false); // �ő�T�C�Y��1024px�ɐ���
            if (selectedImageTexture == null)
            {
                resultText.text = "�摜�̓ǂݍ��݂Ɏ��s���܂����B";
                return;
            }

            photoPreview.texture = selectedImageTexture;
            photoPreview.color = Color.white; // �摜��\�����邽�߂�RawImage��s�����ɂ���

            resultText.text = "AI����͒��ł�...";
            loadingIndicator.SetActive(true);

            // AI�ɉ摜�𑗐M����R���[�`�����J�n
            StartCoroutine(UploadToGemini(selectedImageTexture));

        }, "�H���̎ʐ^��I��");
    }

    private IEnumerator UploadToGemini(Texture2D image)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + geminiApiKey;
        byte[] imageData = image.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageData);
        string prompt = "���̉摜�Ɏʂ��Ă���H�ו��𕪐͂��A�������A���J�����[�APFC�o�����X�i�^���p�N���A�����A�Y�������j����{��ŋ����Ă��������B�񓚂͕K���ȉ���JSON�`���ł��肢���܂�: {\\\"food_name\\\": \\\"������\\\", \\\"calories\\\": �J�����[��, \\\"protein\\\": �^���p�N���O������, \\\"fat\\\": �����O������, \\\"carbs\\\": �Y�������O������}";
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
                Debug.LogError("API�G���[: " + webRequest.error + "\n" + webRequest.downloadHandler.text);
                resultText.text = "��̓G���[���������܂����B";
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

            resultText.text = $"������: {foodData.food_name}\n" +
                              $"�J�����[: {foodData.calories} kcal\n" +
                              $"�^���p�N��: {foodData.protein} g\n" +
                              $"����: {foodData.fat} g\n" +
                              $"�Y������: {foodData.carbs} g";
        }
        catch (System.Exception e)
        {
            Debug.LogError("JSON�̃p�[�X�Ɏ��s���܂���: " + e.Message + "\nRaw Response: " + jsonResponse);
            resultText.text = "���ʂ̉�͂Ɏ��s���܂����B";
        }
    }
}