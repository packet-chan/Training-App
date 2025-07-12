using Mediapipe.Tasks.Components.Containers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HandRaiseCounter : MonoBehaviour
{
    public TextMeshProUGUI repCountText;

    public TMP_Dropdown weightDropdown; // ★インスペクターで重さ選択ドロップダウンを設定
    public Button finishButton;       // ★インスペクターで終了ボタンを設定

    private enum HandState { DOWN, UP }
    private HandState currentState = HandState.DOWN;

    private int repCount = 0;

    // ▼▼▼【変更点①】結果をスレッド間で受け渡すための変数を追加 ▼▼▼
    private volatile int latestRepCount = 0;
    private volatile bool isCountUpdated = false;

    void Start()
    {
        // 終了ボタンがクリックされたら、FinishWorkoutメソッドを呼び出すように設定
        finishButton.onClick.AddListener(FinishWorkout);
    }


    // Updateメソッドを追加します。これはUnityのメインスレッドで毎フレーム実行されます。
    private void Update()
    {
        // ▼▼▼【変更点②】UIの更新をUpdateメソッド内で行います ▼▼▼
        if (isCountUpdated)
        {
            repCountText.text = latestRepCount + " 回";
            isCountUpdated = false; // 更新したらフラグを戻す
        }
    }

    // このメソッドはサブスレッドから呼ばれます
    public void OnPoseLandmarksOutput(NormalizedLandmarks landmarks)
    {

        var rightShoulder = landmarks.landmarks[12];
        var rightWrist = landmarks.landmarks[16];

        float shoulderY = rightShoulder.y;
        float wristY = rightWrist.y;

        if (currentState == HandState.DOWN)
        {
            if (wristY < shoulderY)
            {
                currentState = HandState.UP;
            }
        }
        else
        {
            if (wristY > shoulderY)
            {
                currentState = HandState.DOWN;
                repCount++;

                // ▼▼▼【変更点③】UIを直接更新せず、変数に結果を保存し、フラグを立てる ▼▼▼
                latestRepCount = repCount;
                isCountUpdated = true;

                // Debug.Logはどのスレッドからでも呼べるので、ここではOK
                Debug.Log("Rep counted on sub-thread: " + repCount);
            }
        }
    }

    /// <summary>
    /// トレーニング終了ボタンが押された時の処理
    /// </summary>
    public void FinishWorkout()
    {
        // --- 1. 結果をまとめる ---
        WorkoutResult result = new WorkoutResult();

        result.date = DateTime.Now.ToString("yyyy/MM/dd");

        // ドロップダウンから選択されたテキスト（例: "5.0 kg"）を取得
        string selectedWeightText = weightDropdown.options[weightDropdown.value].text;
        // " kg"の部分を削除して、数値に変換
        result.weight = float.Parse(selectedWeightText.Replace(" kg", ""));

        result.totalReps = latestRepCount; // カウントした最終回数をセット

        // --- 2. 結果を「データ管理人」に預ける ---
        DataManager.latestResult = result;

        // --- 3. 結果確認シーンへ移動 ---
        Debug.Log("トレーニング終了！結果を保存し、結果シーンへ移動します。");
        SceneManager.LoadScene("TrainingResultScene"); // "TrainingResultScene"はご自身のシーン名に
    }
}