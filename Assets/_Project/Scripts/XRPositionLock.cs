using System.Collections;
using UnityEngine;

public class XRPositionLock : MonoBehaviour
{
    [Header("Lock Target")]
    [Tooltip("위치를 고정할 대상. 보통 XR Origin을 넣습니다.")]
    [SerializeField] private Transform lockTarget;

    [Header("Lock Settings")]
    [Tooltip("위치 고정 여부. 회전은 막지 않습니다.")]
    [SerializeField] private bool lockPosition = true;

    [Header("Debug")]
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private bool isAutoMoving = false;

    private Coroutine lockRoutine;

    private void Start()
    {
        if (lockTarget == null)
        {
            lockTarget = transform;
        }

        lockedPosition = lockTarget.position;
        lockRoutine = StartCoroutine(LockPositionRoutine());
    }

    private IEnumerator LockPositionRoutine()
    {
        while (true)
        {
            // XR Interaction Simulator 이동이 끝난 뒤 고정
            yield return new WaitForEndOfFrame();

            if (!lockPosition)
                continue;

            if (isAutoMoving)
                continue;

            if (lockTarget == null)
                continue;

            // 위치만 고정
            // 회전은 유지
            lockTarget.position = lockedPosition;
        }
    }

    public void SetLockPosition(bool value)
    {
        lockPosition = value;

        if (lockTarget != null)
        {
            lockedPosition = lockTarget.position;
        }
    }

    public void SetAutoMoving(bool value)
    {
        isAutoMoving = value;

        if (!isAutoMoving && lockTarget != null)
        {
            lockedPosition = lockTarget.position;
        }
    }

    public void UpdateLockedPosition()
    {
        if (lockTarget != null)
        {
            lockedPosition = lockTarget.position;
        }
    }
}