using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 200f;

    void Update()
    {
        // Z���𒆐S�ɖ��t���[����]������
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}