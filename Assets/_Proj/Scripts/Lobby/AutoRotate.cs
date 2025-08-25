using UnityEngine;

//로비에서 플레이어 차량 회전
public class AutoRotate : MonoBehaviour
{
    public float rotationSpeed = 20;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
