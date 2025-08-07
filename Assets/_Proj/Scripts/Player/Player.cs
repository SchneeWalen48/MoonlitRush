using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

public class Player : MonoBehaviour
{
  public BaseInput inputSource;
  public Stats stats;
  public Rigidbody rb;
  public Transform CenterOfMass;

  WheelFrictionCurve defaultSidewaysFriction;

  public InputData input { get; private set; }
  public float airPercent { get; private set; }
  public float groundPercent { get; private set; }
  public List<GameObject> visualWheelsMesh;

  [Header("WheelColliders")]
  public WheelCollider frontLeftWheel;
  public WheelCollider frontRightWheel;
  public WheelCollider rearLeftWheel;
  public WheelCollider rearRightWheel;

  [Header("Steering")]
  [Range(1.0f, 20.0f), Tooltip("스티어링이 얼마나 빠르게 반응하는지 조절")]
  public float steerSpeed = 10.0f;

  [Range(0.0f, 20.0f)]
  public float airborneReorientation = 5f;

  [Header("Drift")]
  [Range(0.01f, 1.0f)]
  public float driftGrip = 0.4f;
  [Range(0.0f, 10.0f)]
  public float addDriftSteer = 5.0f;
  [Range(1.0f, 30.0f)]
  public float inAngleToFinishDrift = 10.0f;
  [Range(0.01f, 0.99f)]
  public float minSpeedPercentToFinishDrift = 0.5f;
  [Range(1.0f, 20.0f)]
  public float driftControl = 10.0f;
  [Range(0.0f, 20.0f)]
  public float driftDampening = 10.0f;
  public LayerMask groundLayers = Physics.DefaultRaycastLayers;

  [Header("Booster")]
  public float boosterSpeedAdd;
  public float boosterAccelAdd;
  public float boosterDuration;
  
  private float currBoosterTime;
  private Stats originalStats;

  public bool wantsToDrift { get; private set; } = false;
  public bool isDrifting { get; private set; } = false;
  float currGrip = 1.0f;
  float driftTurningPower = 0.0f;
  float preGroundPercent = 1.0f;
  bool canMove = true;
  List<PowerUpEffect> activePowerupList = new List<PowerUpEffect>();
  Stats finalStats;

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
    if (inputSource == null)
      inputSource = GetComponent<BaseInput>();

    finalStats = new Stats(stats);
    originalStats = new Stats(stats);
    currGrip = stats.grip;
    defaultSidewaysFriction = frontLeftWheel.sidewaysFriction;
  }

  void FixedUpdate()
  {
    input = inputSource.GenerateInput();
    stats.currSpeed = rb.velocity.magnitude;
    rb.centerOfMass = transform.InverseTransformPoint(CenterOfMass.position);

    wantsToDrift = Input.GetKey(KeyCode.W) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && Vector3.Dot(rb.velocity, transform.forward) > 0;

    
    ApplyPowerups();

    ApplySpeedUp();

    // 물리 조작
    if (canMove)
    {
      ApplyMotorAndBrake();
      ApplySteering();
    }

    // 휠 접지 상태 확인
    UpdateGroundedState();

    // 공중 상태 처리
    GroundAirbourne();

    // 드리프트 처리
    HandleDrift();

    // 시각적 휠 업데이트
    UpdateVisualWheels();
  }

  void ApplyMotorAndBrake()
  {
    if (rb == null)
    {
      Debug.LogError("ApplyMotorAndBrake: Rigidbody가 null입니다.");
    }
    if (rearLeftWheel == null)
    {
      Debug.LogError("ApplyMotorAndBrake: rearLeftWheel이 null입니다.");
    }
    if (finalStats == null)
    {
      Debug.LogError("ApplyMotorAndBrake: finalStats가 null입니다.");
    }
    float accelInput = (input.Accelerate ? 1.0f : 0.0f) - (input.Brake || input.Reverse ? 1.0f : 0.0f);
    bool isBraking = (Vector3.Dot(rb.velocity, transform.forward) > 0 && accelInput < 0) || (Vector3.Dot(rb.velocity, transform.forward) < 0 && accelInput > 0);

    if (isBraking)
    {
      rearLeftWheel.brakeTorque = finalStats.braking * 1000;
      rearRightWheel.brakeTorque = finalStats.braking * 1000;
      rearLeftWheel.motorTorque = 0;
      rearRightWheel.motorTorque = 0;
    }
    else if (Mathf.Abs(accelInput) > 0.01f)
    {
      float motorForce = finalStats.acceleration * rb.mass;
      float motorTorque = motorForce / 2;
      rearLeftWheel.motorTorque = motorTorque * accelInput;
      rearRightWheel.motorTorque = motorTorque * accelInput;
      rearLeftWheel.brakeTorque = 0;
      rearRightWheel.brakeTorque = 0;
    }
    else
    {
      // 입력이 없으면 감속 위해 브레이크 토크 적용
      rearLeftWheel.motorTorque = 0;
      rearRightWheel.motorTorque = 0;
      rearLeftWheel.brakeTorque = finalStats.dragTime * 100;
      rearRightWheel.brakeTorque = finalStats.dragTime * 100;
    }
  }

  void ApplySteering()
  {
    float currentSteerAngle = frontLeftWheel.steerAngle;
    float steerMultiplier = isDrifting ? (1.0f + addDriftSteer / finalStats.steer) : 1.0f;
    float targetSteerAngle = input.TurnInput * finalStats.steer * steerMultiplier;
    float newSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steerSpeed);

    frontLeftWheel.steerAngle = newSteerAngle;
    frontRightWheel.steerAngle = newSteerAngle;
  }

  void HandleDrift()
  {
    wantsToDrift = input.Brake && Vector3.Dot(rb.velocity, transform.forward) > 0f;

    if (groundPercent > 0.0f)
    {
      // 드리프트 시작
      if (!isDrifting && wantsToDrift)
      {
        isDrifting = true;

        // WheelFrictionCurve 설정
        WheelFrictionCurve driftFriction = frontLeftWheel.sidewaysFriction;
        driftFriction.stiffness = driftGrip;

        frontLeftWheel.sidewaysFriction = driftFriction;
        frontRightWheel.sidewaysFriction = driftFriction;
        rearLeftWheel.sidewaysFriction = driftFriction;
        rearRightWheel.sidewaysFriction = driftFriction;
      }
      // 드리프트 종료
      else if (isDrifting && !wantsToDrift)
      {
        isDrifting = false;

        // WheelFrictionCurve 원상 복구
        frontLeftWheel.sidewaysFriction = defaultSidewaysFriction;
        frontRightWheel.sidewaysFriction = defaultSidewaysFriction;
        rearLeftWheel.sidewaysFriction = defaultSidewaysFriction;
        rearRightWheel.sidewaysFriction = defaultSidewaysFriction;
      }
    }
    else
    {
      isDrifting = false;

      // 공중에 있을 때 마찰력 원상 복구
      frontLeftWheel.sidewaysFriction = defaultSidewaysFriction;
      frontRightWheel.sidewaysFriction = defaultSidewaysFriction;
      rearLeftWheel.sidewaysFriction = defaultSidewaysFriction;
      rearRightWheel.sidewaysFriction = defaultSidewaysFriction;
    }
  }

  void ApplySpeedUp()
  {
    float elapsedTime = boosterDuration - currBoosterTime;
    float t = Mathf.Clamp01(elapsedTime / boosterDuration);
    if(currBoosterTime > 0)
    {
      currBoosterTime -= Time.fixedDeltaTime;
      
      finalStats.acceleration = originalStats.acceleration + boosterAccelAdd;

      float boostForce = Mathf.Lerp(0, boosterSpeedAdd, t);
      rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
    }
    else
    {
      float revertTime = 1f;
      float lerpRatio = Mathf.Clamp01((revertTime - elapsedTime) / revertTime);
      finalStats.acceleration = originalStats.acceleration;
      finalStats.acceleration = Mathf.Lerp(finalStats.acceleration, originalStats.acceleration, Time.fixedDeltaTime * 2.0f);
    }
  }

  void UpdateGroundedState()
  {
    int groundedCount = 0;
    if (frontLeftWheel.isGrounded) groundedCount++;
    if (frontRightWheel.isGrounded) groundedCount++;
    if (rearLeftWheel.isGrounded) groundedCount++;
    if (rearRightWheel.isGrounded) groundedCount++;

    groundPercent = (float)groundedCount / 4.0f;
    airPercent = 1 - groundPercent;
  }

  void GroundAirbourne()
  {
    if (airPercent >= 1)
    {
      rb.velocity += Physics.gravity * Time.fixedDeltaTime * finalStats.addedGravity;
    }
  }

  void ApplyPowerups()
  {
    activePowerupList.RemoveAll(p => p.elapsedTime > p.duration);
    Stats totalEffects = new Stats();
    for (int i = 0; i < activePowerupList.Count; i++)
    {
      activePowerupList[i].elapsedTime += Time.fixedDeltaTime;
      var effect = activePowerupList[i].modifier;
    }

    finalStats.grip = Mathf.Clamp(finalStats.grip, 0f, 1f);
  }

  void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("SpeedUp")) // 추후 태그 명 변경(Booster, Barrel 등)
    {
      Debug.Log("Trigger Detected");
      currBoosterTime = boosterDuration;

      //Vector3 boosterForce = transform.forward * boosterSpeedAdd * rb.mass;
      //rb.AddForce(boosterForce, ForceMode.Impulse);
    }
  }

  void UpdateVisualWheels()
  {
    UpdateWheelPose(frontRightWheel, visualWheelsMesh[0].transform);
    UpdateWheelPose(frontLeftWheel, visualWheelsMesh[1].transform);
    UpdateWheelPose(rearRightWheel, visualWheelsMesh[2].transform);
    UpdateWheelPose(rearLeftWheel, visualWheelsMesh[3].transform);
  }

  void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
  {
    Vector3 pos;
    Quaternion rot;
    collider.GetWorldPose(out pos, out rot);
    wheelTransform.position = pos;
    wheelTransform.rotation = rot;
  }

  
  public void AddPowerup(PowerUpEffect powerUpEffect) => activePowerupList.Add(powerUpEffect);
  public void SetCanMove(bool move) => canMove = move;
  public float GetMaxSpeed() => Mathf.Max(finalStats.limitSpeed, finalStats.reverseSpeed);
  public void Reset()
  {
    Vector3 euler = transform.rotation.eulerAngles;
    euler.x = euler.z = 0f;
    transform.rotation = Quaternion.Euler(euler);
  }
}