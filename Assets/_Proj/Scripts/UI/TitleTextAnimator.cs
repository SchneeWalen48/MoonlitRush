using UnityEngine;
using DG.Tweening;

public class TitleTextAnimator : MonoBehaviour
{
    [Header("Title Text")]
    public RectTransform titleText; // "MOONLIT RUSH" 텍스트 RectTransform

    [Header("Animation Settings")]
    public float startXOffset = 2000f;   // 시작 위치 (오른쪽 화면 밖)
    public float slideDuration = 0.4f;   // 슬라이드 인 속도
    public float overshootDistance = 80f; // 제자리 지나치는 거리
    public float overshootTime = 0.15f;   // 되돌아오는 시간

    [Header("Skid Effect")]
    public float skidPower = 20f;        // 좌우 끼익 흔들림 강도
    public float skidDuration = 0.5f;   // 끼익 흔들림 지속 시간
    public int skidVibrato = 20;         // 흔들림 진동 횟수

    void Start()
    {
        AnimateTitle();
    }

    void AnimateTitle()
    {
        if (!titleText) return;

        // 현재 위치를 목표 지점으로 설정
        Vector2 targetPos = titleText.anchoredPosition;

        // 시작 위치를 오른쪽 화면 밖으로 설정
        Vector2 startPos = targetPos + Vector2.right * startXOffset;
        titleText.anchoredPosition = startPos;

        // DOTween 시퀀스 시작
        Sequence seq = DOTween.Sequence();

        // 1) 오른쪽에서 목표 지점보다 약간 더 지나치도록 이동 (브레이크 전 밀림)
        seq.Append(titleText.DOAnchorPosX(targetPos.x + overshootDistance, slideDuration)
            .SetEase(Ease.OutCubic));

        // 2) 브레이크로 되돌아오기
        seq.Append(titleText.DOAnchorPosX(targetPos.x, overshootTime)
            .SetEase(Ease.OutSine));

        // 3) 브레이크 시 끼익 흔들림 효과
        seq.AppendCallback(() =>
        {
            titleText.DOShakeAnchorPos(
                skidDuration,
                new Vector2(skidPower, 0), // 좌우 흔들림
                vibrato: skidVibrato,
                randomness: 10,
                snapping: false,
                fadeOut: true
            );
        });
    }
}