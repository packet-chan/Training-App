using System.Collections.Generic; // List���g�����߂ɕK�v
using System; // DateTime���g�����߂ɕK�v

// 1��̃g���[�j���O���ʂ��i�[���邽�߂́u�݌v�}�v
[Serializable]
public class WorkoutResult
{
    public string date;       // ���{�� (��: "2025/07/12")
    public float weight;      // �d��
    public int totalReps;   // ����
}

// �Q�[���S�̂Ńf�[�^��ێ����邽�߂́u�ÓI�ȁv�N���X
public static class DataManager
{
    // �ŐV�̃g���[�j���O���ʂ��ꎞ�I�ɕێ�����ꏊ
    public static WorkoutResult latestResult;

    // �ߋ��S�Ẵg���[�j���O������ۑ����郊�X�g
    public static List<WorkoutResult> history = new List<WorkoutResult>();
}