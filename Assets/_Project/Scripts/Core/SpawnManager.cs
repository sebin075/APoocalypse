using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Points")]
    // 오브젝트가 생성될 위치들
    // Left / Center / Right 순서로 넣기
    [SerializeField] private Transform[] spawnPoints;

    [Header("Prefabs")]
    // 생성할 프리팹들
    [SerializeField] private GameObject normalZombiePrefab;
    [SerializeField] private GameObject bigZombiePrefab;
    [SerializeField] private GameObject trashPrefab;

    [Header("Spawn Settings")]
    // 몇 초마다 생성할지
    [SerializeField] private float spawnInterval = 2f;

    // 한 번에 최대 몇 개 생성할지
    // 3라인 기준 2로 설정하면 최소 1라인은 항상 비어있음
    [SerializeField] private int maxSpawnCountPerWave = 2;

    [Header("Spawn Chance")]
    // 일반 좀비 생성 확률
    [SerializeField] private int normalZombieChance = 60;

    // 대형 좀비 생성 확률
    [SerializeField] private int bigZombieChance = 20;

    // 쓰레기 생성 확률
    [SerializeField] private int trashChance = 20;

    [Header("Spawn Height Offset")]
    // 일반 좀비 생성 높이 보정값
    [SerializeField] private float normalZombieYOffset = 0f;

    // 대형 좀비 생성 높이 보정값
    [SerializeField] private float bigZombieYOffset = 1f;

    // 쓰레기 생성 높이 보정값
    [SerializeField] private float trashYOffset = 0f;

    // 생성 타이머
    private float spawnTimer = 0f;

    private void Update()
    {
        // 시간 누적
        spawnTimer += Time.deltaTime;

        // 일정 시간이 지나면 생성 실행
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
        // 스폰 위치 없으면 종료
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        // 이번 웨이브 생성 개수 결정
        int spawnCount =
            Random.Range(1, maxSpawnCountPerWave + 1);

        // 모든 라인이 막히지 않도록 제한
        spawnCount =
            Mathf.Min(spawnCount, spawnPoints.Length - 1);

        // 사용한 라인 체크용 배열
        bool[] usedLines = new bool[spawnPoints.Length];

        // 생성 반복
        for (int i = 0; i < spawnCount; i++)
        {
            // 사용하지 않은 랜덤 라인 가져오기
            int lineIndex =
                GetRandomUnusedLine(usedLines);

            // 실패 시 종료
            if (lineIndex == -1)
                return;

            // 사용한 라인 표시
            usedLines[lineIndex] = true;

            // 랜덤 프리팹 선택
            GameObject prefab = GetRandomPrefab();

            // 프리팹 없으면 건너뛰기
            if (prefab == null)
                continue;

            // 기본 생성 위치 가져오기
            Vector3 spawnPosition =
                spawnPoints[lineIndex].position;

            // 프리팹 종류에 따라 높이 보정
            spawnPosition.y += GetYOffset(prefab);

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
                Random.Range(0, usedLines.Length);

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
        // 전체 확률 합 계산
        int totalChance =
            normalZombieChance +
            bigZombieChance +
            trashChance;

        // 랜덤 값 생성
        int randomValue =
            Random.Range(0, totalChance);

        // 일반 좀비 선택
        if (randomValue < normalZombieChance)
        {
            return normalZombiePrefab;
        }

        // 대형 좀비 선택
        if (randomValue <
            normalZombieChance + bigZombieChance)
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
}