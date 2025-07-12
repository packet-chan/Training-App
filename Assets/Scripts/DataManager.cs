using System.Collections.Generic; // Listを使うために必要
using System; // DateTimeを使うために必要

// 1回のトレーニング結果を格納するための「設計図」
[Serializable]
public class WorkoutResult
{
    public string date;       // 実施日 (例: "2025/07/12")
    public float weight;      // 重量
    public int totalReps;   // 総回数
}

// ゲーム全体でデータを保持するための「静的な」クラス
public static class DataManager
{
    // 最新のトレーニング結果を一時的に保持する場所
    public static WorkoutResult latestResult;

    // 過去全てのトレーニング履歴を保存するリスト
    public static List<WorkoutResult> history = new List<WorkoutResult>();
}