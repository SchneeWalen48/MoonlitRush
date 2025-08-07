using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
  public BaseInput inputSource; // KeyboardInput 할당
  public InputData input { get; private set; }
  
  public List<GameObject> wheelsMeshes;

  [Header("WheelColliders")]
  public WheelCollider frontLeftWheel;
  public WheelCollider frontRightWheel;
  public WheelCollider rearLeftWheel;
  public WheelCollider rearRightWheel;

  public Rigidbody rb;
  public Transform CenterOfMass;

  [Header("Movement")]
  public float currSpeed;
  public float reverseSpeed;
  public float accel;
  public float accelReverse;
  public float brake;
  public float dragTime;
  public float steerAngle;
  public float limitSpeed;
  public float limitSpeedReverse;
  [Range(1f, 20f)]
  public float steerSpeed;

  public float accelCurve;
  public float accelCurveCoeff;

  [Header("Drift")]
  [Range(0.01f, 1f), Tooltip("값 작을수록 더 많이 미끄러미")]
  public float driftGrip;
  [Range (0f, 10f), Tooltip("드리프트 조향력. 값 클수록 예민.")]
  public float addDriftSteer;
  [Range(1f, 30f), Tooltip("드리프트 끝내고 원래 자세로 돌아오기")]
  public float inAngleToFinishDrift;
  [Range(0.01f, 0.99f), Tooltip("드리프트 최소 종료 조건. 최고 속도 대비 일정 비율 이하면 드리프트 유지.")]
  public float minSpeedPercentToFinishDrift;
  [Range(1.0f, 20f), Tooltip("드리프트 중 차량 제어. 값 클수록 제어 쉬움")]
  public float driftControl;
  [Range(0f, 20f), Tooltip("값 작을수록 드리프트 오래 유지")]
  public float driftDampening;
  public bool wantsToDrift { get; private set; } = false;
  public bool isDrift { get; private set; } = false;
  float defaultGrip = 1f; // origianl grip <-> drift grip
  float driftSteerSpeed; // <-> steer speed
  [Tooltip("플레이어 땅에 있을 때 드리프트 가능한?")]
  float groundPercentBeforeDrift;

  [Header("Airborne")]
  [Tooltip("공중에 있는 상태인지 판단")]
  [Range(0f, 1f)]
  private float airPercent;
  [Tooltip("땅에 닿은 바퀴 개수 판단")]
  public float groundPercent { get; private set; }
  [Tooltip("이전 프레임에서 공중에 있었는가?")]
  bool inAir = false;
  [Range(0f, 20f), Tooltip("공중에서 수평 유지 하기 위한 힘")]
  public float airborneReorientation = 5f;
  [Tooltip("공중에 있을 때 중력의 영향 추가")]
  public float addedGravity;

  [Tooltip("차체가 정상적으로 착지한 마지막 회전 저장")]
  Quaternion lastValidRotation;
  [Tooltip("차체가 정상적으로 착지한 마지막 위치 저장")]
  Vector3 lastValidPosition;

  public LayerMask groundLayers = Physics.DefaultRaycastLayers;
  bool isControllable = true;
  bool hasCollision = false;

  // Stabilize the vehicle in situations such as collisions
  Vector3 lastCollisionNormal = Vector3.zero;
  Vector3 verticalReference = Vector3.up;

  void Awake()
  {
    rb = GetComponent<Rigidbody>();
    if (inputSource == null)
      inputSource = GetComponent<BaseInput>();
  }

  void FixedUpdate()
  {
    input = inputSource.GenerateInput();
    currSpeed = rb.velocity.magnitude;
    rb.centerOfMass = transform.InverseTransformPoint(CenterOfMass.position);

    wantsToDrift = Input.GetKey(KeyCode.W) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) && Vector3.Dot(rb.velocity, transform.forward) > 0f;

    if (!isControllable) return;
    if (isControllable)
    {
      
        MoveAndSteer();
      
    }
    ApplyBooster();
    ApplyDrift();
    ApplyBarrelRoll();
    ApplySpeedUp();
   
    HandleAirborneState();

    groundPercentBeforeDrift = groundPercent;
    UpdateVisualWheels();

  }
  public void SetControllable(bool canMove) => isControllable = canMove;

  public float GetMaxSpeed() => Mathf.Max(limitSpeed, reverseSpeed);

  
  

  void MoveAndSteer()
  {
    float accelInput = input.Accelerate ? 1f : (input.Brake || input.Reverse ? -1f : 0f);

    Vector3 localVel = transform.InverseTransformVector(rb.velocity);
    bool localVelFwd = localVel.z >= 0f;
    bool accelFwd = accelInput >= 0;

    float maxSpeed = localVelFwd ? limitSpeed : reverseSpeed;
    float accelPower = accelFwd ? accel : accelReverse;

    float speedRatio = Mathf.Clamp01(currSpeed / maxSpeed);
    float accelMultiplier = Mathf.Lerp(accel * accelCurveCoeff, 1f, speedRatio * speedRatio);
    
    bool isBrake = (localVelFwd && accelInput < 0) || (!localVelFwd && accelInput > 0);
    float finalAccelPower = isBrake ? brake : accelPower;
    float finalAccelForce = finalAccelPower * accelMultiplier;

    float steerInput = input.TurnInput;

    float steerPower = steerInput * (isDrift ? (steerAngle + addDriftSteer) : steerAngle);
    frontLeftWheel.steerAngle = steerPower;
    frontRightWheel.steerAngle = steerPower;

    Vector3 moveDir = transform.forward * accelInput * finalAccelForce;
    Vector3 newVel = rb.velocity + moveDir * Time.fixedDeltaTime;
    newVel.y = rb.velocity.y;

    if(groundPercent > 0f && rb.velocity.magnitude < maxSpeed)
      newVel = Vector3.ClampMagnitude(newVel,maxSpeed);

    if(Mathf.Abs(accelInput) < 0.01f && groundPercent > 0f)
      newVel = Vector3.MoveTowards(newVel, new Vector3(0, rb.velocity.y, 0), Time.fixedDeltaTime * dragTime);

    rb.velocity = newVel;
  }

  void ApplyDrift()
  {
    float accelInput = input.Accelerate ? 1f : (input.Brake || input.Reverse ? -1f : 0f);
    float turnInput = input.TurnInput;
    float currSpeed = rb.velocity.magnitude;
    float maxSpeed = GetMaxSpeed();
    float turnPower = isDrift ? driftSteerSpeed : turnInput * steerAngle;
    frontLeftWheel.steerAngle = turnPower;
    frontRightWheel.steerAngle = turnPower;
    if (groundPercent > 0.0f)
    {
      if (inAir)
        inAir = false;

      float angularVelSteering = 0.4f;
      float angularVelSmoothSpeed = 20f;

      bool accelDirFwd = accelInput >= 0;
      bool localVeclDirFwd = Vector3.Dot(transform.forward, rb.velocity) >= 0;

      Vector3 angularVel = rb.angularVelocity;
      angularVel.y = Mathf.MoveTowards(angularVel.y, turnPower * angularVelSteering, Time.fixedDeltaTime * angularVelSmoothSpeed);

      float velSteering = 25f;

      if (groundPercent >= 0.0f && groundPercentBeforeDrift < 0.1f)
      {
        Vector3 flattenVelocity = Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized;
        if (Vector3.Dot(flattenVelocity, transform.forward * Mathf.Sign(accelInput)) < Mathf.Cos(inAngleToFinishDrift * Mathf.Deg2Rad))
        {
          isDrift = true;
          defaultGrip = driftGrip;
          driftSteerSpeed = 0f;
        }
      }

      if (!isDrift)
      {
        bool isBrake = accelInput < 0f;
        if ((wantsToDrift || isBrake) && currSpeed > maxSpeed * minSpeedPercentToFinishDrift)
        {
          isDrift = true;
          driftSteerSpeed = turnPower + Mathf.Sign(turnPower) * addDriftSteer;
          defaultGrip = driftGrip;
        }
      }

      if (isDrift)
      {
        float turnInputAbs = Mathf.Abs(turnInput);

        if (turnInputAbs < 0.01f)
          driftSteerSpeed = Mathf.MoveTowards(driftSteerSpeed, 0f, driftDampening * Time.fixedDeltaTime);

        float driftMaxSteer = steerAngle + addDriftSteer;
        driftSteerSpeed = Mathf.Clamp(driftSteerSpeed + (turnInput * driftControl * Time.fixedDeltaTime), -driftMaxSteer, driftMaxSteer);

        bool facingVelocity = Vector3.Dot(rb.velocity.normalized, transform.forward * Mathf.Sign(accelInput)) > Mathf.Cos(inAngleToFinishDrift * Mathf.Deg2Rad);

        bool canEndDrift = true;
        if (accelInput < 0f) canEndDrift = false;
        else if (!facingVelocity) canEndDrift = false;
        else if (turnInputAbs >= 0.01f && currSpeed > maxSpeed * minSpeedPercentToFinishDrift)
          canEndDrift = false;

        if (canEndDrift || currSpeed < 0.01f)
        {
          isDrift = false;
          defaultGrip = 1f;
        }
      }
      rb.velocity = Quaternion.AngleAxis(turnPower * Mathf.Sign(Vector3.Dot(transform.forward, rb.velocity)) * velSteering * defaultGrip * Time.fixedDeltaTime, Vector3.up) * rb.velocity;
    }
    else
    {
      inAir = true;
    }
  }

  private void UpdateVerticalReference()
  {
    Vector3 rayOrigin = transform.position + transform.up * 0.1f;
    Vector3 rayDirection = -transform.up;
    float rayLength = 3.0f;
    int groundLayerMask = (1 << 9) | (1 << 10) | (1 << 11); // Ground / Environment / Track

    Vector3 targetNormal;

    if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayLength, groundLayerMask))
    {
      // 우선순위: 마지막 충돌이 더 기울어진 상태면 그걸 따름
      targetNormal = (hasCollision && lastCollisionNormal.y > hit.normal.y)
          ? lastCollisionNormal
          : hit.normal;

      float blendFactor = Mathf.Clamp01(airborneReorientation * Time.fixedDeltaTime * (groundPercent > 0.0f ? 10.0f : 1.0f));
      verticalReference = Vector3.Slerp(verticalReference, targetNormal, blendFactor);
    }
    else
    {
      // 지면 못 찾았을 때는 위쪽(Vector3.up) 기준으로 회복
      targetNormal = (hasCollision && lastCollisionNormal.y > 0.0f)
          ? lastCollisionNormal
          : Vector3.up;

      float blendFactor = Mathf.Clamp01(airborneReorientation * Time.fixedDeltaTime);
      verticalReference = Vector3.Slerp(verticalReference, targetNormal, blendFactor);
    }

    // 차량이 똑바르게 서 있는지 판정
    bool validPosition = groundPercent > 0.7f &&
                    !hasCollision &&
                    Vector3.Dot(verticalReference, Vector3.up) > 0.9f;
  }

  void ApplySpeedUp()
  {
    //TODO: 휠콜라이더는 다른 박스콜라이더 트리거를 감지 못한다는 소문이... 어떻게 박스콜라이더 트리거 발판을 감지하고 적용시킬지 고민해봐야 함. 바퀴 하단과 같은 y축 위치에 박스 콜라이더를 따로 설치하면 또 이상하잖아...?
  }

  void ApplyBarrelRoll()
  {
    //TODO: 휠콜라이더는 다른 박스콜라이더 트리거를 감지 못한다는 소문이... 어떻게 박스콜라이더 트리거 발판을 감지하고 적용시킬지 고민해봐야 함22.
  }

  void ApplyBooster()
  {

  }

  // 공중 회전 보정 및 착지 여부, 중력 보정
  private void HandleAirborneState()
  {
    // 지면 접지율 계산
    int groundedCnt = 0;
    WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };

    foreach (var wheel in wheels)
    {
      if (wheel.isGrounded && wheel.GetGroundHit(out WheelHit hit))
      {
        groundedCnt++;
      }
    }

    groundPercent = groundedCnt / 4f;
    airPercent = 1f - groundPercent;

    // 공중에 있을 경우 중력 추가
    if (airPercent >= 1f)
    {
      rb.velocity += Physics.gravity * Time.fixedDeltaTime * addedGravity;
    }

    // 공중 회전 제어
    bool validPos = groundPercent > 0.7f && !hasCollision && Vector3.Dot(verticalReference, Vector3.up) > 0.9f;
    if (groundPercent < 0.7f)
    {
      rb.angularVelocity = new Vector3(0.0f, rb.angularVelocity.y * 0.98f, 0.0f);

      Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, verticalReference);
      forwardOnPlane.Normalize();

      if (forwardOnPlane.sqrMagnitude > 0.0f)
      {
        Quaternion targetRot = Quaternion.LookRotation(forwardOnPlane, verticalReference);
        rb.MoveRotation(Quaternion.Lerp(rb.rotation, targetRot, Mathf.Clamp01(airborneReorientation * Time.fixedDeltaTime)));
      }
    }
    // 지면 위에서 안정적일 때 위치/회전 저장
    else if (validPos)
    {
      lastValidPosition = transform.position;
      lastValidRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
    }
  }


  void UpdateVisualWheels()
  {
    UpdateWheelPose(frontRightWheel, wheelsMeshes[0].transform);
    UpdateWheelPose(frontLeftWheel, wheelsMeshes[1].transform);
    UpdateWheelPose(rearRightWheel, wheelsMeshes[2].transform);
    UpdateWheelPose(rearLeftWheel, wheelsMeshes[3].transform);
  }
  void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
  {
    Vector3 pos;
    Quaternion rot;
    collider.GetWorldPose(out pos, out rot);
    wheelTransform.position = pos;
    wheelTransform.rotation = rot;

    float steerAngle = collider.steerAngle;
    wheelTransform.localRotation = Quaternion.Euler(0, steerAngle, 0) * wheelTransform.localRotation;
  }
  void OnTriggerEnter(Collider other)
  {
    // TODO: 아이템 획득 및 각 슬롯에 할당 로직(Slot, Item 관련 스크립트 있음)
  }
  
  // Recover only rotation
  public void Reset()
  {
    Vector3 euler = transform.rotation.eulerAngles;
    euler.x = euler.z = 0f;
    transform.rotation = Quaternion.Euler(euler);
  }

  public float LocalSpeed()
  {
    if (isControllable)
    {
      float dot = Vector3.Dot(transform.forward, rb.velocity);
      if (Mathf.Abs(dot) > 0.1f)
      {
        float speed = rb.velocity.magnitude;
        return dot < 0 ? -(speed / accelReverse) : (speed / limitSpeed);
      }
      return 0f;
    }
    else
    {
      // use this value to play kart sound when it is waiting the race start countdown.
      return input.Accelerate ? 1.0f : 0.0f;
    }
  }
}