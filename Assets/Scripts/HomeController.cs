using UnityEngine;
using TMPro;

public class HomeController : MonoBehaviour
{
    // インスペクターで、メッセージを表示したいTextMeshProのUIを設定
    public TextMeshProUGUI notificationText;

    void Start()
    {
        // 1. 管理人にメッセージが預けられているか確認
        if (!string.IsNullOrEmpty(DataManager.messageToHome))
        {
            // 2. メッセージがあれば、TMPのテキストを更新
            notificationText.text = DataManager.messageToHome;

            // 3. 表示したら、メッセージを空に戻しておく
            DataManager.messageToHome = "";
        }
    }
}