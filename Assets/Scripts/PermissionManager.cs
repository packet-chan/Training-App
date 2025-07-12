using UnityEngine;
using UnityEngine.Android; // Androidパーミッションのために必要
using System.Collections;

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        // アプリ起動時にパーミッションを要求するコルーチンを開始
        StartCoroutine(RequestCameraPermission());
    }

    IEnumerator RequestCameraPermission()
    {
        // すでにカメラ使用が許可されているか確認
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            // 許可されていない場合、ユーザーに許可を求めるダイアログを表示
            Permission.RequestUserPermission(Permission.Camera);
        }

        // ユーザーが応答するまで少し待つ（念のため）
        yield return new WaitForSeconds(0.5f);

        // 再度、許可されたか確認
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("Camera permission granted.");
            // ここで、カメラを初期化する処理を呼び出す
            //例：FindObjectOfType<YourMediaPipeScript>().InitializeCamera();
        }
        else
        {
            Debug.LogError("Camera permission was denied.");
            // ここで、許可されなかった場合の処理（警告表示など）を行う
        }
    }
}