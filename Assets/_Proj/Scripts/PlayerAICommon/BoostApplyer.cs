using System.Collections;
using UnityEngine;

public class BoostApplyer : MonoBehaviour
{
  [Header("FX")]
  public BoostFX fx;
  public CarController controller;
  [Header("Timing")]
  public float fadeTime = 0.15f;

  float boostEndTime = -1f; // 현재 부스트 종료 예정 시각. 여러 부스트가 겹칠 경우 가장 늦은 시간으로 연장됨.
  Coroutine fxRoutine;
  public void ApplyBoost(float duration, float sizeMul = 1f, float speedMul = 1f)
  {
    float newEnd = Time.time + Mathf.Max(0f, duration); // 새 부스트 종료 시각 계산
    boostEndTime = Mathf.Max(boostEndTime, newEnd); // 기존 부스트보다 길면 종료 시간 연장

    if(fxRoutine != null) StopCoroutine(fxRoutine); // 기존 FX 코루틴 중단 후 재시작
    fxRoutine = StartCoroutine(CoBoostFx(sizeMul, speedMul));
  }

  IEnumerator CoBoostFx(float sizeMul, float speedMul)
  {
    fx.LerpToBoost(fadeTime, sizeMul, speedMul);
    while (Time.time < boostEndTime)
    {
      yield return null;
    }
    fx.LerpToNormal(fadeTime);
    fxRoutine = null;
  }
}
