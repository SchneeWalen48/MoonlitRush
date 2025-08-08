using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    [Header("Car Components")]
    public WaypointTest WaypointTest;
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight; //WheelCollider 연결, 물리 계산
    public Transform frontLeftTransform, frontRightTransform, rearLeftTransform, rearRightTransform; //Mesh 연결, 시각적, GetWorldPose() 결과 반영하는 데 사용
    public Transform jumpPoint; //배럴롤 점프대

    //주행 설정
    [Header("Vehicle Settings")] 
    public float maxMotorTorque = 3000f; //최대 가속
    public float maxSteerAngle = 30f; //최대 핸들 회전 각도
    public float maxSpeed = 120f; //최고 속도
    public float brakeTorque = 3000f; //브레이크 최대 
    //스피드 발판
    float baseSpeed = 80f; 
    Coroutine boostCoroutine;
    ISpeedUp lastSpeedUp;

    [Header("Drift Settings")]
    // public float driftFactor = 0.95f; //드리프트(미끄러짐) 효과 : 작을수록 효과 커짐
    public float maxDriftAngle = 40f; //드리프트 시작을 위한 최소 조향 각도
    public float driftFrictionStiffness = 0.5f; //드리프트 시 마찰력

    [Header("Barrel Roll Settings")]
    public float barrelRollTorque = 500f; // 배럴롤 회전력
    public float minBarrelRollSpeed = 100f; //배럴롤 최소 속도
    bool isBarrelRolling = false;

    [Header("Suspensions")]
    [Tooltip("The maximum extension possible between the kart's body and the wheels.")]
    [Range(0.0f, 1.0f)]
    public float SuspensionHeight = 0.2f;
    [Range(10.0f, 100000.0f), Tooltip("The higher the value, the stiffer the suspension will be.")]
    public float SuspensionSpring = 20000.0f;
    [Range(0.0f, 5000.0f), Tooltip("The higher the value, the faster the kart will stabilize itself.")]
    public float SuspensionDamp = 500.0f;
    [Tooltip("Vertical offset to adjust the position of the wheels relative to the kart's body.")]
    [Range(-1.0f, 1.0f)]
    public float WheelsPositionVerticalOffset = 0.0f;

    [Header("Ray settings")] //감지 설정
    public float forwardDis = 15f;
    public float sideDis = 3f;    
    public float downDis = 2f;
    public float sideStep = 2.5f; //앞차를 피해 가기 위해 옮겨지는 거리
    public LayerMask carLayer; //AI 및 플레이어가 같은 레이어야 함)
    
    private int currentWaypointIndex = 0;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();        
        baseSpeed = maxSpeed;
    }

    private void Awake()
    {
        UpdateSuspensionParams(frontLeft);
        UpdateSuspensionParams(frontRight);
        UpdateSuspensionParams(rearLeft);
        UpdateSuspensionParams(rearRight);
    }

    void UpdateSuspensionParams(WheelCollider wheel)
    {
        wheel.suspensionDistance = SuspensionHeight;
        wheel.center = new Vector3(0.0f, WheelsPositionVerticalOffset, 0.0f);
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = SuspensionSpring;
        spring.damper = SuspensionDamp;
        wheel.suspensionSpring = spring;
    }

    private void FixedUpdate()  //물리 연산에 적합
    {
        UpdateSuspensionParams(frontLeft);
        UpdateSuspensionParams(frontRight);
        UpdateSuspensionParams(rearLeft);
        UpdateSuspensionParams(rearRight);

        if (WaypointTest == null || WaypointTest.Count == 0) return;

        Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;

        //Raycast 기반 회피                             차체의 앞쪽 범퍼 근처에서 레이 시작 권장
        Vector3 origin = transform.position + Vector3.up * 0.3f + transform.forward * 0.5f;

        float currentSpeed = rb.velocity.magnitude * 3.6f; //rigidbody 속도(m/s)를 km/h로 변환
        float dynamicRayDistance = Mathf.Max(forwardDis, currentSpeed * 0.2f); //속도에 비례한 레이 길이

        //중앙 레이
        Debug.DrawRay(origin, transform.forward * dynamicRayDistance, Color.red);
        bool forward = Physics.Raycast(origin, transform.forward, out RaycastHit forwardHit, dynamicRayDistance, carLayer);
        //전방의 좌, 우 레이
        Debug.DrawRay(origin, Quaternion.AngleAxis(-35, transform.up) * transform.forward * dynamicRayDistance, Color.red);
        bool left = Physics.Raycast(origin, Quaternion.AngleAxis(-35, transform.up) * transform.forward, out RaycastHit leftHit, dynamicRayDistance, carLayer);
        Debug.DrawRay(origin, Quaternion.AngleAxis(35, transform.up) * transform.forward * dynamicRayDistance, Color.red);
        bool right = Physics.Raycast(origin, Quaternion.AngleAxis(35, transform.up) * transform.forward, out RaycastHit rightHit, dynamicRayDistance, carLayer);
        //사이드 레이 좌, 우
        Debug.DrawRay(origin, -transform.right * sideDis, Color.green);
        bool sideLeft = Physics.Raycast(origin, -transform.right, out RaycastHit sideLHit, sideDis, carLayer);
        Debug.DrawRay(origin, transform.right * sideDis, Color.green);
        bool sideRight = Physics.Raycast(origin, transform.right, out RaycastHit sideRHit, sideDis, carLayer);
        //다운 레이
        Debug.DrawRay(origin, Vector3.down * downDis, Color.blue);
        bool isDown = Physics.Raycast(origin, Vector3.down, out RaycastHit downHit, downDis);
       
        if (isDown)  //스피드 발판 감지
        {
            ISpeedUp speedUp = downHit.collider.GetComponent<ISpeedUp>();
            if (speedUp != null && speedUp != lastSpeedUp) { //점프대처럼 태그 추가 필
                speedUp.ApplySpeedUp(gameObject);
                lastSpeedUp = speedUp;
            }
            //점프대 감지
            if(downHit.collider.CompareTag("JumpRamp") && currentSpeed > minBarrelRollSpeed && !isBarrelRolling)
            {
                StartCoroutine(BarrelRollRoutine());
            }
        }
        else { lastSpeedUp = null; }

        //코루틴으로 barrelRoll 상태가 true면 AddRelativeTorque 함수로 차량 회전
        if (isBarrelRolling)
        {
            rb.AddRelativeTorque(Vector3.forward * barrelRollTorque, ForceMode.Acceleration);
        }

        //추월 시 회피 로직        
        if (forward || left || right)
        {
            if (false == sideLeft)
            {
                target += transform.right * sideStep;
            }
            else if (false == sideRight)
            {
                target -= transform.right * sideStep;
            }
        }        
        else if (!forward && !left && !right) //원래 경로로 복귀, ray를 만족했을 때만 실행
        {
            target = WaypointTest.GetWaypoint(currentWaypointIndex).position;
        }

        //Waypoint의 x방향 위치에 따라 핸들 방향 계산
        Vector3 localTarget = transform.InverseTransformPoint(target); //InverseTransformPoint(): 월드 좌표를 현재 차량의 로컬 좌표계로 변환
        float steer = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f);
        float steerAngle = steer * maxSteerAngle;

        //Steering(핸들을 조향각 비율만큼 회전)
        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;

        float motorTorque = maxMotorTorque;
                 
        //드리프트 연출 및 제어
        if(Mathf.Abs(steerAngle) > maxDriftAngle && currentSpeed > 40f)
        {
            SetDriftFriction(driftFrictionStiffness);
        }
        else
        {
            SetDriftFriction(1f); //기본 마찰력으로 복구
        }

        //핸들 꺾은 정도에 따라 감속
        if (Mathf.Abs(steer) > 0.5f)
        {
            motorTorque *= 0.5f;
        }

        //다음 waypoint 방향 고려한 감속: 급커브 등 추가 감속
        float predictedSteer = PredicNextSteer();
        if (Mathf.Abs(predictedSteer) > 0.6f)
        {
            motorTorque *= 0.4f;
        }

        //waypoint별 제한 속도 적용
        float speedLimit = WaypointTest.GetSpeedLimit(currentWaypointIndex);
        if (currentSpeed > speedLimit)
        {
            motorTorque = 0f;
        }

        //공중에서 가속 X
        if (!isDown)
        {
            motorTorque = 0;
        }

        //브레이크
        if (Mathf.Abs(steer) > 0.8f || currentSpeed > maxSpeed)
        {
            frontLeft.brakeTorque = brakeTorque;
            frontRight.brakeTorque = brakeTorque;
        }
        else
        {
            frontLeft.brakeTorque = 0f;
            frontRight.brakeTorque = 0f;
        }
        
        //Motor(앞바퀴에 가속력 부여)
        frontLeft.motorTorque = motorTorque;
        frontRight.motorTorque = motorTorque;

        //다음 Waypoint 확인
        float waypointDetectiionDistance = 10f + (currentSpeed / maxSpeed * 5f);
        float distance = Vector3.Distance(transform.position, target);
        if (distance < waypointDetectiionDistance)
        {                                                  //마지막에 도달하면 다시 0부터 시작(루프)
            currentWaypointIndex = (currentWaypointIndex + 1) % WaypointTest.Count;
        }

        //WheelCollider가 실제 위치한 위치와 회전을 Wheel Mesh에 반영
        UpdateWheelPose(frontLeft, frontLeftTransform);
        UpdateWheelPose(frontRight, frontRightTransform);
        UpdateWheelPose(rearLeft, rearLeftTransform);
        UpdateWheelPose(rearRight, rearRightTransform);

        //급커브 등에서 미리 감속하기 위한 함수
        float PredicNextSteer()
        {
            int nextIndex = (currentWaypointIndex + 1) % WaypointTest.Count;
            Vector3 future = transform.InverseTransformPoint(WaypointTest.GetWaypoint(nextIndex).position);
            return Mathf.Clamp(future.x / future.magnitude, -1f, 1f);
        }

        //Mesh Wheel이 WheelCollider와 똑같이 움직이게 만드는 함수
        void UpdateWheelPose(WheelCollider collider, Transform wheelTransform)
        {
            Vector3 pos;
            Quaternion rot;
            collider.GetWorldPose(out pos, out rot);
            wheelTransform.position = pos;
            wheelTransform.rotation = rot;
        }

        void SetDriftFriction(float stiffness) //뒷바퀴의 마찰력을 낮춤
        {
            WheelFrictionCurve rearFriction = rearLeft.sidewaysFriction;
            rearFriction.stiffness = stiffness;
            rearLeft.sidewaysFriction = rearFriction;
            rearRight.sidewaysFriction = rearFriction;
        }
    }

   //스피드 발판 적용 코루틴
    public void ApplySpeedPadBoost(float force, float duration)
    {
        if (boostCoroutine != null) {
        StopCoroutine(boostCoroutine);
        }
        boostCoroutine = StartCoroutine(BoostRoutine(force, duration));
    }
    
    IEnumerator BoostRoutine(float force, float duration)
    {
        Debug.Log("AI 스피드 패드 코루틴 시작");
        maxSpeed += force;
        yield return new WaitForSeconds(duration);

        Debug.Log("AI 스피드 패드 코루틴 끝");
        maxSpeed = baseSpeed;
    }
    
    //배럴롤 코루틴
    IEnumerator BarrelRollRoutine()
    {
        isBarrelRolling = true;
        Debug.Log("AI 배럴롤 시작");

        yield return new WaitForSeconds(1f); 

        isBarrelRolling = false;
        Debug.Log("AI 배럴롤 종료");
    }

}



//갑자기 든 생각
//wheelCollider는 물리적 계산이라 다른 콜라이더와 다르다 했으니 경사면에서 휠 부분이 아래로 내려가는 이유가 되지 않을까 싶은....
//해결방안은 raycast로 달리는 바닥 감지...?