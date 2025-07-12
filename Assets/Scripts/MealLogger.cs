using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

// 'using NativeGallery;' �̓N���X���Ȃ̂ŕs�v�B�폜���܂����B

public class MealLogger : MonoBehaviour
{
    // --- Unity�G�f�B�^����ݒ肷�鍀�� ---
    [Header("UI�p�[�c")]
    [SerializeField] private Button selectImageButton;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("API�ݒ�")]
    [SerializeField] private string geminiApiKey; // YOUR_GEMINI_API_KEY

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
        // NativeGallery�̓��o�C����p�@�\�Ȃ̂ŁAPC�G�f�B�^�ȂǂŃG���[���o�Ȃ��悤��
        // #if�f�B���N�e�B�u�ŁAAndroid��iOS�̎������{�^�����@�\����悤�ɐݒ肵�܂��B
#if UNITY_ANDROID || UNITY_IOS
        selectImageButton.onClick.AddListener(PickImageFromGallery);
#else
        // ���o�C���ȊO�ł̓{�^���𖳌������A���b�Z�[�W��\��
        selectImageButton.interactable = false;
        resultText.text = "���̋@�\��Android�܂���iOS�f�o�C�X�ł̂ݗ��p�\�ł��B";
#endif
        loadingIndicator.SetActive(false);
    }

    // ���̃��\�b�h�S�̂��A���o�C���v���b�g�t�H�[���ł̂݃R���p�C�������悤�Ɉ݂͂܂��B
#if UNITY_ANDROID || UNITY_IOS
    /// <summary>
    /// �M�������[����摜��I�����鏈��
    /// </summary>
    private void PickImageFromGallery()
    {
        // GetImageFromGallery�������v�������s���Ă���܂�
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

            // CPU����A�N�Z�X�ł���悤�ɁAmarkAsNonReadable��false�ɐݒ�
            selectedImageTexture = NativeGallery.LoadImageAtPath(path, 1024, false);
            if (selectedImageTexture == null)
            {
                resultText.text = "�摜�̓ǂݍ��݂Ɏ��s���܂����B";
                return;
            }

            photoPreview.texture = selectedImageTexture;
            photoPreview.color = Color.white;

            resultText.text = "AI����͒��ł�...";
            loadingIndicator.SetActive(true);

            StartCoroutine(UploadToGemini(selectedImageTexture));

        }, "�H���̎ʐ^��I��");
    }
#endif

    private IEnumerator UploadToGemini(Texture2D image)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + geminiApiKey;
        byte[] imageData = image.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageData);
        string prompt = "���̉摜�Ɏʂ��Ă���H�ו��𕪐͂��A�������A���J�����[�APFC�o�����X�i�^���p�N���A�����A�Y�������j����{��ŋ����Ă��������B�񓚂͕K���ȉ���JSON�`���ł��肢���܂�: {\\\"food_name\\\": \\\"������\\\", \\\"calories\\\": �J�����[��, \\\"protein\\\": �^���p�N���O������, \\\"fat\\\": �����O������, \\\"carbs\\\": �Y�������O������}";
        string jsonPayload = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{prompt}\"}},{{\"inline_data\":{{\"mime_type\":\"image/jpeg\",\"data\":\"{base64Image}\"}}}}]}}]}}";

        // --- ������ ���g���C�����̒ǉ� ������ ---
        int maxRetries = 3; // �ő�3��܂Ŏ��s
        float retryDelay = 2.0f; // ���s������2�b�҂�

        for (int i = 0; i < maxRetries; i++)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                // 503�G���[�ȊO�Ŏ��s�������A�܂��͐��������ꍇ
                if (webRequest.responseCode != 503 || webRequest.result == UnityWebRequest.Result.Success)
                {
                    // �����Ń��[�v�𔲂��āA�ʏ�̌��ʏ����ɐi��
                    HandleApiResponse(webRequest);
                    yield break;
                }

                // 503�G���[�ŁA�܂����g���C�񐔂��c���Ă���ꍇ
                Debug.LogWarning($"API is overloaded. Retrying in {retryDelay} seconds... (Attempt {i + 1}/{maxRetries})");
                yield return new WaitForSeconds(retryDelay);
            }
        }
        // --- ������ ���g���C���������܂� ������ ---

        // ���ׂẴ��g���C�����s�����ꍇ
        Debug.LogError("API failed after all retries.");
        resultText.text = "�T�[�o�[�����ݍ����Ă��܂��B���΂炭���Ă��炨�������������B";
        loadingIndicator.SetActive(false);
    }

    // ���X�|���X������ʂ̃��\�b�h�ɕ���
    private void HandleApiResponse(UnityWebRequest webRequest)
    {
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
