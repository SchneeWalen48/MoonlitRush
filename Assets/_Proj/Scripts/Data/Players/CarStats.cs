using UnityEngine;

[CreateAssetMenu(fileName = "carStats", menuName = "CarStats", order = 0)]
public class CarStats : ScriptableObject
{
  [Header("Physics")]
  public float mass = 1000f;
  public float acceleration = 3f; // 기본 가속 크기
  public float maxSpeed = 20f; // 전진 최고 속도 상한
  public float deceleration = 3f; // 기본 감속 크기
  public float decelLerpSpeed = 7f;   // 목표 감속값에 수렴하는 속도. 클수록 빨리 따라감
  [Range(0, 1)] public float coastFactor = 0.6f; // 페달 off일 때 제동 비율
  public float minSpeedForFullDecel = 6f; // 저속에서는 감속 줄이기(m/s)
  public float currDecelMag = 0f; // 현재 적용 중인 감속 크기(스무딩 값)

  [Header("Handling")]
  public float steerForce = 20f; // 회전 드래그(회전 감쇠)
  public float angularDrag = 10f; // 회전 토크 기본 크기(드리프트 시 1.5배)
  public AnimationCurve turningCurve = AnimationCurve.Linear(0, 1, 1, 1); // 속도 비율에 따른 조향 감도 커브

  [Header("Gear Settings")]
  [Range(0.1f, 2f)]public float holdTopSpeed = 1f; // 기어 최고 속도 도달 시 유지 시간

  [Header("Downforce")]
  public float baseDownforce = 1000f; // 기본 다운포스
  public float downforcePerMS = 1f; // 속도 비례해서 추가되는 다운포스 계수

  [Header("Airbourne")]
  [Range(0.1f, 1)] public float airGravity = 0.6f; //공중 상태 중력 보정 비율
  public float airGravityDuration = 0.6f; // 중력 보정 지속 시간

  [Header("Accel Curve")]
  public AnimationCurve accelCurve = AnimationCurve.Linear(0, 1, 1, 1); // 속도에 따른 가속 배율 커브

  [Header("Braking")]
  public float brakePow = 5f; // 브레이크 강도 계수
}
