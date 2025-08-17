using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Dashboard : MonoBehaviour
{
    [Header("Refs")]
    public CarController car;        // 플레이어 CarController (currSpeed 사용)
    public Image gaugeFill;          // Image Type=Filled, Radial180/360
    public RectTransform needle;     // 바늘 RectTransform (없으면 비워도 OK)
    public TextMeshProUGUI speedText;// 중앙 속도 숫자 (선택)

    [Header("Config")]
    public float maxSpeedKmh = 100f;   // 게임 기준 최고 속도(km/h)
    [Tooltip("바늘 각도: 0km/h에서 각도(좌측), 최고속도에서 각도(우측)")]
    public float minNeedleAngle = 40f;   // 0 km/h일 때 바늘 Z각
    public float maxNeedleAngle = -220f; // 최고속도일 때 바늘 Z각
    public bool invertFill = false;      

    [Header("Smoothing")]
    [Range(0f, 20f)] public float smooth = 8f; // 숫자/바늘/게이지 보간

    // 내부 상태
    float _t;            // 0~1 정규화 속도
    float _shownT;       // 보간된 값
    float _shownKmh;     // 보간된 속도표시

    void Reset()
    {
        // 씬에 CarController 하나면 자동 할당 시도
        if (!car) car = FindObjectOfType<CarController>();
    }

    void LateUpdate()
    {
        if (!car) return;

        // 1) 속도(m/s -> km/h)
        float kmh = Mathf.Max(0f, car.currSpeed * 3.6f);

        // 2) 0~1 정규화
        _t = Mathf.Clamp01(kmh / Mathf.Max(0.01f, maxSpeedKmh));

        // 3) 부드럽게 보간(선택)
        float s = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        _shownT = Mathf.Lerp(_shownT, _t, s);
        _shownKmh = Mathf.Lerp(_shownKmh, kmh, s);

        // 4) 게이지 채우기
        if (gaugeFill)
            gaugeFill.fillAmount = invertFill ? (1f - _shownT) : _shownT;

        // 5) 바늘 회전(선택)
        if (needle)
        {
            float ang = Mathf.Lerp(minNeedleAngle, maxNeedleAngle, _shownT);
            needle.localEulerAngles = new Vector3(0, 0, ang);
        }

        // 6) 숫자 표시(선택)
        if (speedText)
            speedText.text = Mathf.RoundToInt(_shownKmh).ToString();
    }
}
