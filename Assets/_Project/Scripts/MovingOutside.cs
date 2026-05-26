using UnityEngine;

public class MovingOutside : MonoBehaviour
{
    [Header("Move Settings")]
    [Tooltip("창밖 배경이 움직이는 방향")]
    [SerializeField] private Vector3 moveDirection = Vector3.back;

    [Tooltip("버스 주행 느낌을 위한 배경 이동 속도")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool isMoving = true;
    [SerializeField] private float currentSpeed;

    private float targetSpeed;
    private float stopTimer;
    private float stopDuration;
    private bool isStopping = false;

    private void Start()
    {
        moveDirection.y = 0f;
        moveDirection.Normalize();

        currentSpeed = moveSpeed;
        targetSpeed = moveSpeed;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        if (isStopping)
        {
            stopTimer += Time.deltaTime;
            float t = stopTimer / stopDuration;

            currentSpeed = Mathf.Lerp(targetSpeed, 0f, t);

            if (t >= 1f)
            {
                currentSpeed = 0f;
                isMoving = false;
                isStopping = false;
            }
        }

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
    }

    public void StopMoving(float duration)
    {
        stopDuration = duration;
        stopTimer = 0f;
        targetSpeed = currentSpeed;
        isStopping = true;
    }
}
