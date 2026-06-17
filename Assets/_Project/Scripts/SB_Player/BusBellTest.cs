using UnityEngine;

public class BusBellTest : MonoBehaviour
{
    [Header("색상 변경 설정 (테스트용)")]
    public MeshRenderer bellRenderer; // 큐브의 메쉬 렌더러
    public Color pressedColor = Color.red; // 벨 눌렸을 때 바뀔 색상 (기본 빨간색)

    private Color originalColor;
    private bool isLightOn = false;

    private void Start()
    {
        // 처음에 큐브가 가지고 있던 원래 색상을 기억해둡니다.
        if (bellRenderer != null)
        {
            originalColor = bellRenderer.material.color;
        }
    }

    // 플레이어 이동 스크립트(UnityEvent)에서 호출할 핵심 함수
    public void RingTheBell()
    {
        isLightOn = !isLightOn;

        if (isLightOn)
        {
            Debug.Log("?? [딩동] 버스 벨이 켜졌습니다!");
            if (bellRenderer != null) bellRenderer.material.color = pressedColor;
        }
        else
        {
            Debug.Log("?? [딩동] 버스 벨이 꺼졌습니다!");
            if (bellRenderer != null) bellRenderer.material.color = originalColor;
        }
    }
}