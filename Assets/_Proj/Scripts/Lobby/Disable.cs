using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Disable : MonoBehaviour
{
    public GameObject carRoot; 

    public TMP_InputField tmpInput;     // TextMeshPro 입력창
    
    public GameObject nicknamePanel;    // 닉네임 패널이 따로 있으면 지정
    public bool hideWhenPanelActive = true;

    bool _originalActive = true;

    void OnEnable()
    {
        if (carRoot) _originalActive = carRoot.activeSelf;
        Apply();
    }

    void OnDisable()
    {
        // 스크립트가 꺼질 때는 원래 상태로 복구
        if (carRoot) carRoot.SetActive(_originalActive);
    }

    void Update() => Apply();

    void Apply()
    {
        if (!carRoot) return;

        bool focused = false;
        if (tmpInput && tmpInput.isFocused) focused = true;
        if (hideWhenPanelActive && nicknamePanel && nicknamePanel.activeInHierarchy) focused = true;

        bool shouldShow = !focused; // 입력 중이면 false → 숨김
        if (carRoot.activeSelf != shouldShow)
            carRoot.SetActive(shouldShow);
    }
}