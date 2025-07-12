using UnityEngine;
using UnityEngine.Android; // Android�p�[�~�b�V�����̂��߂ɕK�v
using System.Collections;

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        // �A�v���N�����Ƀp�[�~�b�V������v������R���[�`�����J�n
        StartCoroutine(RequestCameraPermission());
    }

    IEnumerator RequestCameraPermission()
    {
        // ���łɃJ�����g�p��������Ă��邩�m�F
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            // ������Ă��Ȃ��ꍇ�A���[�U�[�ɋ������߂�_�C�A���O��\��
            Permission.RequestUserPermission(Permission.Camera);
        }

        // ���[�U�[����������܂ŏ����҂i�O�̂��߁j
        yield return new WaitForSeconds(0.5f);

        // �ēx�A�����ꂽ���m�F
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("Camera permission granted.");
            // �����ŁA�J���������������鏈�����Ăяo��
            //��FFindObjectOfType<YourMediaPipeScript>().InitializeCamera();
        }
        else
        {
            Debug.LogError("Camera permission was denied.");
            // �����ŁA������Ȃ������ꍇ�̏����i�x���\���Ȃǁj���s��
        }
    }
}