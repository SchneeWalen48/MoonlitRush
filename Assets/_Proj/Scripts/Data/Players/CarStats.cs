using UnityEngine;

[CreateAssetMenu(fileName = "carStats", menuName = "CarStats", order = 0)]
public class CarStats : ScriptableObject
{
  [Header("Physics")]
  public float mass = 1000f;
  public float acceleration = 3f;
  public float maxSpeed = 20f;
  public float deceleration = 3f;
  public float decelLerpSpeed = 7f;   // 클수록 빨리 따라감
  [Range(0, 1)] public float coastFactor = 0.6f; // 페달 off일 때 제동 비율
  public float minSpeedForFullDecel = 6f; // 저속에서는 감속 줄이기(m/s)
  public float currDecelMag = 0f; // 현재 적용 중인 감속 크기(스무딩 값)

  [Header("Handling")]
  public float steerForce = 20f;
  public float angularDrag = 10f;
  public AnimationCurve turningCurve = AnimationCurve.Linear(0, 1, 1, 1);

  [Header("Gear Settings")]
  [Range(0.1f, 2f)]public float holdTopSpeed = 1f;

  [Header("Downforce")]
  public float baseDownforce = 1000f;
  public float downforcePerMS = 1f;

  [Header("Airbourne")]
  [Range(0.1f, 1)] public float airGravity = 0.6f;
  public float airGravityDuration = 0.6f;

  [Header("Accel Curve")]
  public AnimationCurve accelCurve = AnimationCurve.Linear(0, 1, 1, 1);

  [Header("Braking")]
  public float brakePow = 5f;
}
