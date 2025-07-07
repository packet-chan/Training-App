using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    /// <summary>
    /// 指定された名前のシーンを読み込みます。
    /// このメソッドをUnityのボタンのOnClickイベントから呼び出します。
    /// </summary>
    /// <param name="sceneName">読み込みたいシーンの名前</param>
    public void LoadScene(string sceneName)
    {
        // sceneNameが空でないことを確認
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("シーン名が指定されていません！");
        }
    }
}