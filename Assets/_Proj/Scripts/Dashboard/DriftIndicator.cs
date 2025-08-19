using UnityEngine;
using TMPro;

public class DriftIndicator : MonoBehaviour
{
    public TextMeshProUGUI driftText;

    [Header("Effect Settings")]
    public Color driftColor = Color.yellow;
    public float shakeAmount = 2f;   // 흔들림 강도
    public float shakeSpeed = 20f;   // 흔들림 속도

    private Color originalColor;
    private Vector3 originalPos;

    void Start()
    {
        if (driftText == null)
            driftText = GetComponent<TextMeshProUGUI>();

        originalColor = driftText.color;
        originalPos = driftText.rectTransform.localPosition;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // 색상 변경
            driftText.color = driftColor;

            // 좌우 흔들림 (sin파를 이용한 진동)
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
            driftText.rectTransform.localPosition = originalPos + new Vector3(offsetX, 0, 0);
        }
        else
        {
            // 원래 상태 복귀
            driftText.color = originalColor;
            driftText.rectTransform.localPosition = originalPos;
        }
    }
}