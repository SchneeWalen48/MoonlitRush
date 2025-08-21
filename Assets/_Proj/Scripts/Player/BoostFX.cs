using System.Collections;
using UnityEngine;

public class BoostFX : MonoBehaviour
{
  [Header("FX Reference")]
  public ParticleSystem boostTrail; // Exhaust_Trail 프리팹 연결하면 됨

  [Header("Normal State")]
  public Color normalColor = Color.white;
  public float normalStartSize = 0.4f;
  public float normalStartSpeed = 2f;

  [Header("Boost State")]
  public Color boostColor = Color.cyan;
  public float boostStartSize = 1f;
  public float boostStartSpeed = 5f;

  void Awake()
  {
    if (!boostTrail) boostTrail = GetComponentInChildren<ParticleSystem>();
  }

  void OnEnable()
  {
    if(!boostTrail) boostTrail= GetComponentInChildren<ParticleSystem>();
  }
  void Start()
  {
    ApplyNormalImmediate();
  }

  public void LerpToBoost(float t, float sizeMul = 1f, float speedMul = 1f)
  {
    StopAllCoroutines();
    StartCoroutine(CoLerp(
      fromColor: GetCurrColor(),
      toColor: boostColor,
      fromSize: GetCurrSize(),
      toSize: boostStartSize * sizeMul,
      fromSpeed: GetCurrSpeed(),
      toSpeed: boostStartSpeed * speedMul,
      time: t));
  }

  public void LerpToNormal(float t)
  {
    StopAllCoroutines();
    StartCoroutine(CoLerp(
      fromColor: GetCurrColor(),
      toColor: normalColor,
      fromSize: GetCurrSize(),
      toSize: normalStartSize,
      fromSpeed: GetCurrSpeed(),
      toSpeed: normalStartSpeed,
      time: t));
  }

  void ApplyNormalImmediate()
  {
    if (!boostTrail) return;
    var main = boostTrail.main;
    main.startColor = new ParticleSystem.MinMaxGradient(normalColor);
    main.startSize = normalStartSize;
    main.startSpeed = normalStartSpeed;
  }

  IEnumerator CoLerp(Color fromColor, Color toColor, float fromSize, float toSize, float fromSpeed, float toSpeed, float time)
  {
    float t0 = 0f;
    while (t0 < time)
    {
      float a = time <= 0f ? 1f : t0 / time;
      var main = boostTrail.main;
      main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(fromColor, toColor, a));
      main.startSize = Mathf.Lerp(fromSize, toSize, a);
      main.startSpeed = Mathf.Lerp(fromSpeed, toSpeed, a);
      t0 += Time.deltaTime;
      yield return null;
    }
    var mainFinal = boostTrail.main;
    mainFinal.startColor = new ParticleSystem.MinMaxGradient(toColor);
    mainFinal.startSize = toSize;
    mainFinal.startSpeed = toSpeed;
  }

  Color GetCurrColor()
  {
    if (!boostTrail) return normalColor;
    var main = boostTrail.main;
    return main.startColor.color;
  }
  float GetCurrSize()
  {
    if (!boostTrail) return normalStartSize;
    var main = boostTrail.main;
    return main.startSize.constant;
  }
  float GetCurrSpeed()
  {
    if (!boostTrail) return normalStartSpeed;
    var main = boostTrail.main;
    return main.startSpeed.constant;
  }

  public void SetEmission(bool on)
  {
    if (boostTrail == null) return;

    var em = boostTrail.emission;
    em.enabled = on;

    if (on)
    {
      if (!boostTrail.isPlaying) boostTrail.Play();
    }
    else
    {
      if (boostTrail.isPlaying) boostTrail.Stop();
    }
  }
}