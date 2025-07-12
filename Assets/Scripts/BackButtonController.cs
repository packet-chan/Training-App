using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonController : MonoBehaviour
{
    /// <summary>
    /// ボタンのOnClickイベントから、このメソッドを呼び出します
    /// </summary>
    public void GoToHomeWithMessage()
    {
        // 1. 管理人に、表示したい定型文を預ける
        DataManager.messageToHome = "今日のトレーニングお疲れ様です！ダンベル運動後の豚の生姜焼きは、実は理想的な食事の一つです。";

        // 2. ホーム画面へ移動する
        SceneManager.LoadScene("HomeScene");
    }
}