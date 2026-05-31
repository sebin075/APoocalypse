using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // ================================
    // 스폰 위치 설정
    // ================================
    [Header("Spawn Points")]

    [Tooltip(
        "오브젝트 생성 위치\n" +
        "Left / Center / Right 순서 추천"
    )]
    [SerializeField]
    private Transform[] spawnPoints;

    // ================================
    // 생성할 프리팹 설정
    // ================================
    [Header("Prefabs")]

    [Tooltip("일반 좀비 프리팹")]
    [SerializeField]
    private GameObject normalZombiePrefab;

    [Tooltip("대형 좀비 프리팹")]
    [SerializeField]
    private GameObject bigZombiePrefab;

    [Tooltip("쓰레기 프리팹")]
    [SerializeField]
    private GameObject trashPrefab;

    // ================================
    // 생성 주기 설정
    // ================================
    [Header("Spawn Settings")]

    [Tooltip(
        "몇 초마다 생성할지\n" +
        "현재 추천값 : 2"
    )]
    [Range(0.1f, 10f)]
    [SerializeField]
    private float spawnInterval = 2f;

    [Tooltip(
        "한 번에 생성할 최대 개수\n" +
        "3라인 기준 2 추천\n" +
        "최소 1라인은 항상 비워두기 위함"
    )]
    [Range(1, 3)]
    [SerializeField]
    private int maxSpawnCountPerWave = 2;

    // ================================
    // 생성 확률 설정
    // ================================
    [Header("Spawn Chance")]

    [Tooltip(
        "일반 좀비 생성 확률\n" +
        "현재 추천값 : 60"
    )]
    [Range(0, 100)]
    [SerializeField]
    private int normalZombieChance = 60;

    [Tooltip(
        "대형 좀비 생성 확률\n" +
        "현재 추천값 : 20"
    )]
    [Range(0, 100)]
    [SerializeField]
    private int bigZombieChance = 20;

    [Tooltip(
        "쓰레기 생성 확률\n" +
        "현재 추천값 : 20"
    )]
    [Range(0, 100)]
    [SerializeField]
    private int trashChance = 20;

    // ================================
    // 생성 높이 보정
    // ================================
    [Header("Spawn Height Offset")]

    [Tooltip(
        "일반 좀비 생성 높이 보정\n" +
        "보통 0 추천"
    )]
    [SerializeField]
    private float normalZombieYOffset = 0f;

    [Tooltip(
        "대형 좀비 생성 높이 보정\n" +
        "땅에 박히는 현상 방지\n" +
        "현재 추천값 : 1~1.5"
    )]
    [SerializeField]
    private float bigZombieYOffset = 1f;

    [Tooltip(
        "쓰레기 생성 높이 보정"
    )]
    [SerializeField]
    private float trashYOffset = 0f;

    // ================================
    // 겹침 검사 설정
    // ================================
    [Header("Overlap Check")]

    [Tooltip(
        "생성 위치 주변 검사 범위\n" +
        "값이 클수록 오브젝트끼리 멀리 생성됨"
    )]
    [Range(0f, 5f)]
    [SerializeField]
    private float checkRadius = 1.5f;

    [Tooltip(
        "겹침 검사에 사용할 Layer\n" +
        "일반 좀비 / 대형 좀비 / 쓰레기에\n" +
        "Hazard Layer 적용 필요"
    )]
    [SerializeField]
    private LayerMask hazardLayer;

    // ================================
    // 디버그 상태 표시
    // Inspector에서 실시간 확인 가능
    // ================================
    [Header("Debug Status")]

    [Tooltip("현재 생성 타이머")]
    [SerializeField]
    private float currentSpawnTimer;

    [Tooltip("마지막 생성 가능 여부")]
    [SerializeField]
    private bool lastCanSpawnResult;

    [Tooltip("마지막 생성 라인 번호")]
    [SerializeField]
    private int lastSpawnLineIndex = -1;

    [Tooltip("마지막 생성된 오브젝트 이름")]
    [SerializeField]
    private string lastSpawnedObjectName;

    // 내부 생성 타이머
    private float spawnTimer;

    private void Update()
    {
        // 시간 누적
        spawnTimer += Time.deltaTime;

        // Inspector 디버그 표시용
        currentSpawnTimer = spawnTimer;

        // 생성 시간이 되면 실행
        if (spawnTimer >= spawnInterval)
        {
            // 타이머 초기화
            spawnTimer = 0f;

            // 웨이브 생성
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        // 스폰 위치 없으면 실행 안 함
        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
            return;

        // 이번 웨이브 생성 개수 결정
        int spawnCount =
            Random.Range(
                1,
                maxSpawnCountPerWave + 1
            );

        // 최소 1라인은 비우기
        spawnCount =
            Mathf.Min(
                spawnCount,
                spawnPoints.Length - 1
            );

        // 이번 웨이브에서 사용한 라인 체크
        bool[] usedLines =
            new bool[spawnPoints.Length];

        // 생성 반복
        for (int i = 0; i < spawnCount; i++)
        {
            // 사용 안 한 랜덤 라인 선택
            int lineIndex =
                GetRandomUnusedLine(usedLines);

            // 실패 시 종료
            if (lineIndex == -1)
                return;

            // 해당 라인 사용 처리
            usedLines[lineIndex] = true;

            // 생성할 프리팹 선택
            GameObject prefab =
                GetRandomPrefab();

            // 프리팹 없으면 건너뜀
            if (prefab == null)
                continue;

            // 기본 생성 위치
            Vector3 spawnPosition =
                spawnPoints[lineIndex].position;

            // 프리팹 종류에 따라 높이 보정
            spawnPosition.y +=
                GetYOffset(prefab);

            // 생성 가능 여부 검사
            lastCanSpawnResult =
                CanSpawn(spawnPosition);

            // 디버그 표시용 저장
            lastSpawnLineIndex =
                lineIndex;

            lastSpawnedObjectName =
                prefab.name;

            // 이미 주변에 오브젝트 있으면 생성 안 함
            if (!lastCanSpawnResult)
            {
                continue;
            }

            // 오브젝트 생성
            Instantiate(
                prefab,
                spawnPosition,
                spawnPoints[lineIndex].rotation
            );
        }
    }

    private int GetRandomUnusedLine(bool[] usedLines)
    {
        // 무한 루프 방지용
        int safetyCount = 0;

        while (safetyCount < 20)
        {
            // 랜덤 라인 선택
            int index =
                Random.Range(
                    0,
                    usedLines.Length
                );

            // 아직 사용 안 한 라인이면 반환
            if (!usedLines[index])
            {
                return index;
            }

            safetyCount++;
        }

        // 실패 시 -1 반환
        return -1;
    }

    private GameObject GetRandomPrefab()
    {
        // 전체 확률 계산
        int totalChance =
            normalZombieChance +
            bigZombieChance +
            trashChance;

        // 전체 확률이 0이면 생성 안 함
        if (totalChance <= 0)
            return null;

        // 랜덤 값 생성
        int randomValue =
            Random.Range(
                0,
                totalChance
            );

        // 일반 좀비 선택
        if (randomValue < normalZombieChance)
        {
            return normalZombiePrefab;
        }

        // 대형 좀비 선택
        if (
            randomValue <
            normalZombieChance +
            bigZombieChance
        )
        {
            return bigZombiePrefab;
        }

        // 나머지는 쓰레기 선택
        return trashPrefab;
    }

    private float GetYOffset(GameObject prefab)
    {
        // 일반 좀비 높이 보정
        if (prefab == normalZombiePrefab)
        {
            return normalZombieYOffset;
        }

        // 대형 좀비 높이 보정
        if (prefab == bigZombiePrefab)
        {
            return bigZombieYOffset;
        }

        // 쓰레기 높이 보정
        if (prefab == trashPrefab)
        {
            return trashYOffset;
        }

        // 기본값
        return 0f;
    }

    private bool CanSpawn(Vector3 position)
    {
        // position 주변에
        // checkRadius 범위 안에
        // Hazard Layer 오브젝트가 있는지 검사

        bool hasOverlap =
            Physics.CheckSphere(
                position,
                checkRadius,
                hazardLayer
            );

        // 겹치는 오브젝트 없을 때만 생성 가능
        return !hasOverlap;
    }

    private void OnDrawGizmosSelected()
    {
        // Spawn Point 없으면 종료
        if (spawnPoints == null)
            return;

        // 검사 범위 색상
        Gizmos.color = Color.yellow;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            // 생성 겹침 검사 범위 표시
            Gizmos.DrawWireSphere(
                point.position,
                checkRadius
            );
        }
    }
}