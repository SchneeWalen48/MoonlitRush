using System.Collections;
using UnityEngine;

public class BoostApplyer : MonoBehaviour
{
  [Header("FX")]
  public BoostFX fx;
  public CarController controller;

  [Header("Timing")]
  public float fadeTime = 0.15f;

  float boostEndTime = -1f;
  Coroutine fxRoutine;

  public float SpeedCapMulti { get; private set; } = 1f; // 최고속도 캡(최고 상한선) 배수
  public float AccelMulti { get; private set; } = 1f; // 가속 배수
  public void ApplyBoost(float duration, float sizeMul = 1f, float speedMul = 1f, float capMul = 1.2f, float accelMul = 1.2f)
  {
    //float newEnd = Time.time + Mathf.Max(0f, duration);
    //boostEndTime = Mathf.Max(boostEndTime, newEnd); // 기존 부스트 + 새 부스트 => 부스트 연장
    boostEndTime = Time.time + Mathf.Max(0f, duration); // 기존 부스트 => 새 부스트 교체

    SpeedCapMulti = Mathf.Max(0.01f, capMul);
    AccelMulti = Mathf.Max(0.01f, accelMul);

    // FX 재시작
    if(fxRoutine != null) StopCoroutine(fxRoutine);
    fxRoutine = StartCoroutine(CoBoostFx(sizeMul, speedMul));
  }

  IEnumerator CoBoostFx(float sizeMul, float speedMul)
  {
    //fx.LerpToBoost(fadeTime, sizeMul, speedMul);
    if(fx != null) fx.LerpToBoost(fadeTime, sizeMul, speedMul);
    // boostEndTime까지 유지. 중간에 새 부스트 오면 boostEndTime/배수 갱신. 코루틴 재시작
    while (Time.time < boostEndTime)
    {
      yield return null;
    }

    //부스트 종료 시 배수 원래대로
    SpeedCapMulti = 1f;
    AccelMulti = 1f;

    fx.LerpToNormal(fadeTime);
    fxRoutine = null;
  }
}
