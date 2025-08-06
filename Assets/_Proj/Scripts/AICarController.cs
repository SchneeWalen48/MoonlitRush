using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarController : MonoBehaviour
{
    [Header("Car Components")]
    public WaypointTest WaypointTest;
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight; //WheelCollider 연결, 물리 계산
    public Transform frontLeftTransform, frontRightTransform, rearLeftTransform, rearRightTransform; //Mesh 연결, 시각적, GetWorldPose() 결과 반영하는 데 사용

    //주행 설정
    [Header("Vehicle Settings")] 
    public float maxMotorTorque = 1500f; //최대 가속
    public float maxSteerAngle = 30f; //최대 핸들 회전 각도
    public float maxSpeed = 100f; //최고 속도
    public float brakeTorque = 3000f; //브레이크 최대 

    [Header("Drift Settings")]
    public float driftFactor = 0.95f; //드리프트(미끄러짐) 효과 : 작을수록 효과 커짐

    [Header("Overtaking")] //전방 차량 감지 설정
    public LayerMask carLayer; //AI 및 플레이어 차량 포함된 레이어 마스크(같은 레이어야 함)
    public float detectionDistance = 10f; //전방 차량 감지 거리
    public float sideStep = 2.5f; //앞차를 피해 가기 위해 옮겨지는 거리

    private int currentWaypointIndex = 0;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()  //물리 연산에 적합
    {
        if (WaypointTest == null || WaypointTest.Count == 0) return;

        Vector3 target = WaypointTest.GetWaypoint(currentWaypointIndex).position;

        //추월 시 측면 회피
        Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, carLayer))
        {
            target += transform.right * sideStep;
        }

        //Waypoint의 x방향 위치에 따라 핸들 방향 계산
        Vector3 localTarget = transform.InverseTransformPoint(target); //InverseTransformPoint(): 월드 좌표를 현재 차량의 로컬 좌표계로 변환
        float steer = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1f, 1f);
        float steerAngle = steer * maxSteerAngle;

        //Steering(핸들을 조향각 비율만큼 회전)
        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;

        //속도 및 가속 계산
        float currentSpeed = rb.velocity.magnitude * 3.6f; //rigidbody 속도(m/s)를 km/h로 변환
        float motorTorque = maxMotorTorque;

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

        //드리프트 연출
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        localVelocity.x *= driftFactor;
        rb.velocity = transform.TransformDirection(localVelocity);

        //Motor(앞바퀴에 가속력 부여)
        frontLeft.motorTorque = motorTorque;
        frontRight.motorTorque = motorTorque;

        //다음 Waypoint 확인
        float distance = Vector3.Distance(transform.position, target);
        if (distance < 10f)
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
    }




}
