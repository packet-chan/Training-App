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
        // --- 1. �f�[�^�Ǘ��l����ŐV�̌��ʂ��󂯎�� ---
        WorkoutResult result = DataManager.latestResult;

        // --- 2. �󂯎�������ʂ�UI�ɕ\�� ---
        dateText.text = result.date;
        weightText.text = result.weight.ToString("F1") + " kg";
        repsText.text = result.totalReps + " ��";

        // --- 3. �����{�^���ɏ�����o�^ ---
        completeButton.onClick.AddListener(OnCompleteButtonClicked);
    }

    /// <summary>
    /// �����{�^���������ꂽ���̏���
    /// </summary>
    void OnCompleteButtonClicked()
    {
        // --- 1. �ŐV�̌��ʂ��u�����v���X�g�ɒǉ� ---
        DataManager.history.Add(DataManager.latestResult);

        // --- 2. �z�[����ʂֈړ� ---
        Debug.Log("���ʂ𗚗��ɒǉ����A�z�[����ʂֈړ����܂��B");
        SceneManager.LoadScene("HomeScene"); // "HomeScreen"�͂����g�̃V�[������
    }
}