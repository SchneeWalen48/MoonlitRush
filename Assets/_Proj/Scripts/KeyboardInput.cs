using UnityEngine;

// keyboard input script
public class KeyboardInput : BaseInput
{
  public Rigidbody rb;
  public float reverseThreshold = 0.1f; // 후진 시작 기준점
  public override InputData GenerateInput()
  {
    // Dot으로 Vector 내적 계산. rb.velocity와 rb.transform.forward가 직각일수록 0에 가까워짐.
    float forwardSpeed = Vector3.Dot(rb.velocity, rb.transform.forward);

    return new InputData
    {
      Accelerate = Input.GetKey(KeyCode.W),
      Brake = Input.GetKey(KeyCode.S) && forwardSpeed > reverseThreshold,
      Reverse = Input.GetKey(KeyCode.S) && forwardSpeed <= reverseThreshold,
      TurnInput = Input.GetAxis("Horizontal")
    };
  }
}
