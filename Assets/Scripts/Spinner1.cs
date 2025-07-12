using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 200f;

    void Update()
    {
        // Z軸を中心に毎フレーム回転させる
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}