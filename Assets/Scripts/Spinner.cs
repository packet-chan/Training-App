using UnityEngine;

public class Spinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 200f;

    void Update()
    {
        // Z²‚ğ’†S‚É–ˆƒtƒŒ[ƒ€‰ñ“]‚³‚¹‚é
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}