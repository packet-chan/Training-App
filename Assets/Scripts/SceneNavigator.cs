using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    /// <summary>
    /// �w�肳�ꂽ���O�̃V�[����ǂݍ��݂܂��B
    /// ���̃��\�b�h��Unity�̃{�^����OnClick�C�x���g����Ăяo���܂��B
    /// </summary>
    /// <param name="sceneName">�ǂݍ��݂����V�[���̖��O</param>
    public void LoadScene(string sceneName)
    {
        // sceneName����łȂ����Ƃ��m�F
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("�V�[�������w�肳��Ă��܂���I");
        }
    }
}