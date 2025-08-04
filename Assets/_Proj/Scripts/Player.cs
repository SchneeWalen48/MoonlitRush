using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

//Player Script
public class Player : MonoBehaviour
{
  [Header("Movement Settings")]
  public float currSpeed; // 현재 속도 -> Rigidbody로 실시간 계산
  public float acceleration; // 가속도
  public float brakeForce; // 브레이크 적용 정도
  public float backwardAccel; // 후진 가속도
  public float reverseSpeedLimit; // 후진 제한 속도
  public float limitSpeed; // 최고 속도
  public float driftSteerBoost;  // 드리프트
  public float steerSpeed; // 조향 반응 속도
  public float driftBrakeBoost; // 회전 속도

  [Header("Wheel Colliders")]
  public WheelCollider frontLWheelCollider;
  public WheelCollider frontRWheelCollider;
  public WheelCollider rearLWheelCollider;
  public WheelCollider rearRWheelCollider;

  [Header("Wheel Meshes")]
  public Transform frontLWheelTrans;
  public Transform frontRWheelTrans;
  public Transform rearLWheelTrans;
  public Transform rearRWheelTrans;

  public float maxMotorTorque;
  public float maxSteerAngle;
  
  
  private Rigidbody rb;

  //TODO:
  //1. w누르면 가속도가 더해지면서 currSpeed(속도)에 계속 현재 속도를 채워줌. limitSpeed에(200f정도) 도달하면 가속도 0.(속도가0이 되는게 아님!!)
  //2. ad좌우 이동하면서 해당 방향으로 차체 각도 조금씩 틀어짐
  //3. s누르면 서서히 속도 감속(Linear)
  //4. currSpeed가 0이라면 s눌렀을때 후진(limitSpeed = 10f)
  //5. SpeedUp발판에 닿으면 순간적으로 속도 올랐다가 n초 뒤 밟기 전 속도로 돌아와야 함
  //6. BarrelRoll발판에 닿으면 순간적으로 속도 오르면서 n초동안 360도 회전하고 땅에 닿은 후 n1초 후에 원래 발판 닿기 전 속도로 돌아와야 함
  //7. 아이템 트리거에 닿으면 아이템을 획득하고 할당. ctrl키 누르면 할당된 아이템 순서대로 사용.(Item관련 .cs가 필요함)
  //8. 속도가 50이상일 경우, s와 a or d를 누른다면 순간 감속하면서(brakeAmount) 드리프트 진행(회전각 커져야 함)

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
  }
  void Update()
  {
    CarMove();
  }

  void FixedUpdate()
  {
    ApplyTurn();
    Drift();
  }

  void LateUpdate()
  {
    UpdateWheelVisual(frontLWheelCollider, frontLWheelTrans);
    UpdateWheelVisual(frontRWheelCollider, frontRWheelTrans);
    UpdateWheelVisual(rearLWheelCollider, rearLWheelTrans);
    UpdateWheelVisual(rearRWheelCollider, rearRWheelTrans);
  }

  private void UpdateWheelVisual(WheelCollider coll, Transform wheelTrans)
  {
    coll.GetWorldPose(out Vector3 pos, out Quaternion rot);
    wheelTrans.position = pos;
    wheelTrans.rotation = rot;
  }

  void CarMove()
  {
    float x = Input.GetAxis("Horizontal"); // 좌우
    float z = Input.GetAxis("Vertical"); // 앞뒤(z==1 : 앞, z==-1 : 뒤, z==0 아무것도 누르지 않음)

    currSpeed = rb.velocity.magnitude * Vector3.Dot(transform.forward, rb.velocity.normalized);

    if (z > 0 && currSpeed < limitSpeed)
    {
      rearLWheelCollider.motorTorque = acceleration;
      rearRWheelCollider.motorTorque = acceleration;

      rearLWheelCollider.brakeTorque = 0f;
      rearRWheelCollider.brakeTorque = 0f;
    }
    else if (z < 0f)
    {
      if (currSpeed > 0f)
      {
        rearLWheelCollider.brakeTorque = brakeForce;
        rearRWheelCollider.brakeTorque = brakeForce;

        rearLWheelCollider.motorTorque = 0f;
        rearRWheelCollider.motorTorque = 0f;
      }
      else if (currSpeed > -reverseSpeedLimit)
      {
        rearLWheelCollider.motorTorque = -backwardAccel;
        rearRWheelCollider.motorTorque = -backwardAccel;

        rearLWheelCollider.brakeTorque = 0f;
        rearRWheelCollider.brakeTorque = 0f;
      }
    }
    // 어떤 키도 누르고 있지 않을때 서서히 감속(현재 속도를 0f에 가까워지도록 1초동안 brakeAmount만큼 프레임 보정(프레임 속도와 무관하게 일정힌 감속 유지))
    else {
      {
        rearLWheelCollider.motorTorque = 0f;
        rearRWheelCollider.motorTorque = 0f;

        rearLWheelCollider.brakeTorque = brakeForce * 0.5f;
        rearRWheelCollider.brakeTorque = brakeForce * 0.5f;
      }


      if (x > 0f)
      {

      }
      else if (x < 0f)
      {

      }


      Vector3 move = transform.forward * currSpeed * Time.deltaTime;
      rb.MovePosition(rb.position + move);
    }
  }

    bool IsDrift() {
      return Mathf.Abs(currSpeed) > 50f && Input.GetKey(KeyCode.S) && Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;
    }


  void ApplyTurn()
  {
      float steerInput = Input.GetAxis("Horizontal");
      float steerAngle = steerInput * maxSteerAngle;

      if (IsDrift())
      {
        steerAngle *= driftSteerBoost;
      }

      frontLWheelCollider.steerAngle = Mathf.Lerp(frontLWheelCollider.steerAngle, steerAngle, steerSpeed * Time.deltaTime);
      frontRWheelCollider.steerAngle = Mathf.Lerp(frontRWheelCollider.steerAngle, steerAngle, steerSpeed * Time.deltaTime);
  }

  void Drift()
  {
    if (IsDrift())
    {
      rearLWheelCollider.brakeTorque = brakeForce * driftBrakeBoost;
      rearRWheelCollider.brakeTorque = brakeForce * driftBrakeBoost;
    }
  }

  void ApplyExternalBoosts()
  {

  }
}
