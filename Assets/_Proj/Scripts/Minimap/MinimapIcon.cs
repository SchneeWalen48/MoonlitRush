using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    public Transform target;
    public Camera minimapCamera;
    public RectTransform minimapRect;
    RectTransform icon;

    void Start() => icon = GetComponent<RectTransform>();

    void LateUpdate()
    {
        if (!target || !minimapCamera || !minimapRect) return;

        Vector3 vp = minimapCamera.WorldToViewportPoint(target.position);
        Vector2 size = minimapRect.rect.size;

        Vector2 localPos = new Vector2(
            (vp.x - 0.5f) * size.x,
            (vp.y - 0.5f) * size.y
        );

        icon.anchoredPosition = localPos;
        icon.localEulerAngles = Vector3.zero; // 방향 고정
    }
}