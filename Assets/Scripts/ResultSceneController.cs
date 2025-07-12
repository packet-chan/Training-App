using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultSceneController : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI repsText;
    public Button completeButton;

    void Start()
    {
        // --- 1. データ管理人から最新の結果を受け取る ---
        WorkoutResult result = DataManager.latestResult;

        // --- 2. 受け取った結果をUIに表示 ---
        dateText.text = result.date;
        weightText.text = result.weight.ToString("F1") + " kg";
        repsText.text = result.totalReps + " 回";

        // --- 3. 完了ボタンに処理を登録 ---
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    /// <summary>
    /// 完了ボタンが押された時の処理
    /// </summary>
    void OnCompleteButtonClicked()
    {
        // --- 1. 最新の結果を「履歴」リストに追加 ---
        DataManager.history.Add(DataManager.latestResult);

        // --- 2. ホーム画面へ移動 ---
        Debug.Log("結果を履歴に追加し、ホーム画面へ移動します。");
        SceneManager.LoadScene("HomeScene"); // "HomeScreen"はご自身のシーン名に
    }
}