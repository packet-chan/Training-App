using Mediapipe.Tasks.Components.Containers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HandRaiseCounter : MonoBehaviour
{
    public TextMeshProUGUI repCountText;

    public TMP_Dropdown weightDropdown; // ���C���X�y�N�^�[�ŏd���I���h���b�v�_�E����ݒ�
    public Button finishButton;       // ���C���X�y�N�^�[�ŏI���{�^����ݒ�

    private enum HandState { DOWN, UP }
    private HandState currentState = HandState.DOWN;

    private int repCount = 0;

    // �������y�ύX�_�@�z���ʂ��X���b�h�ԂŎ󂯓n�����߂̕ϐ���ǉ� ������
    private volatile int latestRepCount = 0;
    private volatile bool isCountUpdated = false;

    void Start()
    {
        // �I���{�^�����N���b�N���ꂽ��AFinishWorkout���\�b�h���Ăяo���悤�ɐݒ�
        finishButton.onClick.AddListener(FinishWorkout);
    }


    // Update���\�b�h��ǉ����܂��B�����Unity�̃��C���X���b�h�Ŗ��t���[�����s����܂��B
    private void Update()
    {
        // �������y�ύX�_�A�zUI�̍X�V��Update���\�b�h���ōs���܂� ������
        if (isCountUpdated)
        {
            repCountText.text = latestRepCount + " ��";
            isCountUpdated = false; // �X�V������t���O��߂�
        }
    }

    // ���̃��\�b�h�̓T�u�X���b�h����Ă΂�܂�
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

                // �������y�ύX�_�B�zUI�𒼐ڍX�V�����A�ϐ��Ɍ��ʂ�ۑ����A�t���O�𗧂Ă� ������
                latestRepCount = repCount;
                isCountUpdated = true;

                // Debug.Log�͂ǂ̃X���b�h����ł��Ăׂ�̂ŁA�����ł�OK
                Debug.Log("Rep counted on sub-thread: " + repCount);
            }
        }
    }

    /// <summary>
    /// �g���[�j���O�I���{�^���������ꂽ���̏���
    /// </summary>
    public void FinishWorkout()
    {
        // --- 1. ���ʂ��܂Ƃ߂� ---
        WorkoutResult result = new WorkoutResult();

        result.date = DateTime.Now.ToString("yyyy/MM/dd");

        // �h���b�v�_�E������I�����ꂽ�e�L�X�g�i��: "5.0 kg"�j���擾
        string selectedWeightText = weightDropdown.options[weightDropdown.value].text;
        // " kg"�̕������폜���āA���l�ɕϊ�
        result.weight = float.Parse(selectedWeightText.Replace(" kg", ""));

        result.totalReps = latestRepCount; // �J�E���g�����ŏI�񐔂��Z�b�g

        // --- 2. ���ʂ��u�f�[�^�Ǘ��l�v�ɗa���� ---
        DataManager.latestResult = result;

        // --- 3. ���ʊm�F�V�[���ֈړ� ---
        Debug.Log("�g���[�j���O�I���I���ʂ�ۑ����A���ʃV�[���ֈړ����܂��B");
        SceneManager.LoadScene("TrainingResultScene"); // "TrainingResultScene"�͂����g�̃V�[������
    }
}