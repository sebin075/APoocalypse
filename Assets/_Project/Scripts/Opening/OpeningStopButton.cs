using UnityEngine;

public class OpeningStopButton : MonoBehaviour
{
    private BusOpeningManager manager;

    private void Start()
    {
        manager = FindObjectOfType<BusOpeningManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 손 태그 확인 후 매니저 실행
        if (other.CompareTag("PlayerHand"))
        {
            if (manager != null) manager.TriggerStopButton();
        }
    }
}