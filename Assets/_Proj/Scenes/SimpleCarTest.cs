using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleCarTest : MonoBehaviour
{
  public float acceleration = 10f;     // 가속력
  public float deceleration = 5f;      // 감속력
  public float maxSpeed = 20f;         // 최고 속도
  public float turnSpeed = 100f;       // 회전 속도

  private Rigidbody rb;

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
  }

  void FixedUpdate()
  {
    float moveInput = Input.GetAxis("Vertical");   // W/S 또는 ↑/↓
    float turnInput = Input.GetAxis("Horizontal"); // A/D 또는 ←/→

    Vector3 forwardForce = transform.forward * moveInput * acceleration;

    // 현재 속도
    float currSpeed = Vector3.Dot(rb.velocity, transform.forward);

    // 속도 제한
    if (Mathf.Abs(currSpeed) < maxSpeed || Mathf.Sign(currSpeed) != Mathf.Sign(moveInput))
    {
      rb.AddForce(forwardForce, ForceMode.Acceleration);
    }

    // 회전 (속도 있을 때만)
    if (Mathf.Abs(currSpeed) > 0.1f)
    {
      float turn = turnInput * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(currSpeed);
      Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
      rb.MoveRotation(rb.rotation * turnRotation);
    }

    // 감속 (입력 없을 때 자연스럽게 멈추게)
    if (Mathf.Approximately(moveInput, 0))
    {
      Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
      localVel.z = Mathf.MoveTowards(localVel.z, 0, deceleration * Time.fixedDeltaTime);
      rb.velocity = transform.TransformDirection(localVel);
    }
  }
}
