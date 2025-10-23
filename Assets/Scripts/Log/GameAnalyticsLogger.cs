// GameAnalyticsLogger (Newtonsoft 제거 버전, txt + csv 출력 전용)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

public enum LogCategory { Session, Wave, Build, Resources, Combat, Movement,Total}

public class GameAnalyticsLogger : MonoBehaviour
{
    public static GameAnalyticsLogger instance { get; private set; }

    [Header("Build/Schema")]
    [SerializeField] string buildVersion = "6000.0.55f1";   // 빌드 버전
    [SerializeField] int eventSchemaVersion = 0;            // 로그 구조 버전
    public int playerBulletCount;
    public int playerBulletHitCount;
    public int waveCount = 0;
    public int exitCount = 0;
    public bool isInSpaceShip = false;
    public Vector2 playerLastPosition;
    public float playerMoveDistance = 0f;
    public float playerMaxMoveDistance = 0f;
    public float maxDistanceToOrigin = 0f;


    string userId, sessionId, sessionDir;
    UTF8Encoding noBom = new UTF8Encoding(false);   // 인코딩 시 BOM 제거 - CSV 분석 도구에서 문제 방지용.

    readonly Dictionary<LogCategory, StreamWriter> writers = new();
    readonly Dictionary<LogCategory, string> fileNames = new()
{
    { LogCategory.Session,     "session.txt" },
    { LogCategory.Wave,        "wave.txt" },
    { LogCategory.Build,       "Build.txt" },
    { LogCategory.Resources,   "resource.txt" },
    { LogCategory.Combat,      "combat.txt" },
    { LogCategory.Movement,    "movement.txt" },
};

    readonly Dictionary<LogCategory, string[]> csvHeaders = new()
{
    { LogCategory.Session, new[]{"event_name","ts","t","play_time_sec"}},
    { LogCategory.Wave, new[]{"event_name","ts","t","wave","minute","second"}},
    { LogCategory.Build , new[]{"event_name"}},  // 이거 추가해줘.
    { LogCategory.Resources, new[]{"event_name","ts","t","amount","wave","tile_x","tile_y","tile_type","cause"}},
    { LogCategory.Combat, new[]{"event_name","ts","t","play_time_sec"}},
    { LogCategory.Movement, new[]{"event_name","ts","t","pos_x","pos_y","vel_x","vel_y","vel_z","dt"}},
};

    readonly Dictionary<LogCategory, StreamWriter> csvWriters = new();

    float sessionStartRealtime;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        userId = LoadOrCreateUserId();
        sessionId = Guid.NewGuid().ToString("N");
        sessionStartRealtime = Time.realtimeSinceStartup;

        string exeDir = Path.GetDirectoryName(Application.dataPath);
        var baseDir = Path.Combine(exeDir, "Logs", buildVersion, userId, DateTime.UtcNow.ToString("yyyyMMdd"));
        Directory.CreateDirectory(baseDir);
        sessionDir = Path.Combine(baseDir, $"session-{sessionId}");
        Directory.CreateDirectory(sessionDir);
        Application.wantsToQuit += OnWantsToQuit;
        LogSessionStart();
    }


    #region 저장 함수들(txt,csv)
    string LoadOrCreateUserId()
    {
        const string KEY = "ANON_USER_ID";
        if (!PlayerPrefs.HasKey(KEY))
        {
            PlayerPrefs.SetString(KEY, Guid.NewGuid().ToString("N"));
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetString(KEY);
    }

    StreamWriter GetWriter(LogCategory cat)
    {
        if (writers.TryGetValue(cat, out var w) && w != null) return w;
        var path = Path.Combine(sessionDir, fileNames[cat]);
        var sw = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), noBom);
        writers[cat] = sw;
        return sw;
    }

    StreamWriter GetCsv(LogCategory cat)
    {
        if (csvWriters.TryGetValue(cat, out var w) && w != null) return w;
        var path = Path.Combine(sessionDir, Path.GetFileNameWithoutExtension(fileNames[cat]) + ".csv");
        var sw = CsvUtil.Open(path, csvHeaders[cat]);
        csvWriters[cat] = sw;
        return sw;
    }

    void WriteTxt(LogCategory cat, string eventName, Dictionary<string, object> data)
    {
        var w = GetWriter(cat);
        string time = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        string kv = "";
        if (data != null)
        {
            foreach (var kvp in data)
                kv += $"{kvp.Key}={kvp.Value}, ";
            if (kv.EndsWith(", ")) kv = kv[..^2];
        }
        w.WriteLine($"[{time}] [{eventName}] {kv}");
    }

    void WriteCsv(LogCategory cat, string eventName, Dictionary<string, object> data)
    {
        var header = csvHeaders[cat];
        var row = new List<string>();
        string ts = DateTime.UtcNow.ToString("o");
        string t = (Time.realtimeSinceStartup - sessionStartRealtime).ToString(CultureInfo.InvariantCulture);

        row.Add(eventName);
        row.Add(ts);
        row.Add(t);

        foreach (var col in header)
        {
            if (col == "event_name" || col == "ts" || col == "t") continue;
            string v = data != null && data.ContainsKey(col) ? Convert.ToString(data[col], CultureInfo.InvariantCulture) : "";
            row.Add(v);
        }

        var sw = GetCsv(cat);
        sw.WriteLine(CsvUtil.Join(row));
    }
    string GetCurrentTime() // 게임 시간
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Time.time);
        string formattedTime = $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        return formattedTime;
    }

    string GetLocalTime() // 실제 현재 시간
    {
        DateTime startTimeUtc = DateTime.Now;
        return startTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");

    }
    #endregion

    #region Session
    public void LogSessionStart()
    {
        var data = new Dictionary<string, object> {
            { "StartTime",  GetLocalTime()},
            };
        WriteTxt(LogCategory.Session, "session_start", data);
        WriteCsv(LogCategory.Session, "session_start", data);
    }

    // [session_stop] timestamp: float / session_duration: float
    public void LogSessionEnd()
    {
        var data = new Dictionary<string, object> {
            {"EndTime",GetLocalTime() },
            {"Session_Duration", Math.Round(Time.realtimeSinceStartup - sessionStartRealtime, 3)}
        };
        WriteTxt(LogCategory.Session, "session_end", data);
        WriteCsv(LogCategory.Session, "session_end", data);
    }
    #endregion

    #region Wave
    // [wave_start] wave: int / timestamp: float / core_hp_BeforeWave: float
    public void LogWaveStart(int waveNumber, int coreHpBefore)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Core_Hp_Before",  coreHpBefore}
        };
        WriteTxt(LogCategory.Wave, "wave_start", data);
        WriteCsv(LogCategory.Wave, "wave_start", data);
    }

    // [wave_complete] wave: int / timestamp: float / core_hp_CompleteWave: float
    public void LogWaveComplete(int waveNumber, int coreHpComplete)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Core_Hp_CompleteWave",  coreHpComplete}
        };
        WriteTxt(LogCategory.Wave, "wave_complete", data);
        WriteCsv(LogCategory.Wave, "wave_complete", data);
    }

    // [wave_fail] wave: int / timestamp: float / core_hp_FailWave: float
    public void LogWaveFail(int waveNumber, int coreHpFail)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Core_Hp_FailWave",  coreHpFail}
        };
        WriteTxt(LogCategory.Wave, "wave_fail", data);
        WriteCsv(LogCategory.Wave, "wave_fail", data);
    }
    #endregion

    #region Build
    public void LogBuildUpgrade(int waveNumber, string buildName)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Build_Name", buildName }
        };
        WriteTxt(LogCategory.Build, "build_upgrade", data);
        WriteCsv(LogCategory.Build, "build_upgrade", data);
    }
    #endregion

    #region Resources
    public void LogWaveResources(int waveNumber, string mineralInfo)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { " ", mineralInfo }
        };
        WriteTxt(LogCategory.Resources, "wave_resources", data);
        WriteCsv(LogCategory.Resources, "wave_resources", data);
    }

    // [Total_Resources] mineral_type: string / total_mined_session: string / total_deposited_session: string
    public void LogTotalResources(string mineralType, string totalMinedSession, string totalDepositedSession)
    {
        var data = new Dictionary<string, object>
        {
            { "Mineral_Type", mineralType },
            { "Total_Mined_Session", totalMinedSession },
            { "Total_Deposited_Session", totalDepositedSession }
        };
        WriteTxt(LogCategory.Resources, "total_resources", data);
        WriteCsv(LogCategory.Resources, "total_resources", data);
    }
    #endregion

    #region Combat
    public void LogEnemySpawn(int waveNumber, string enemyType, int enemyNum, string spawnLocation)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Enemy_Type", enemyType },
            { "Enemy_Num", enemyNum },
            { "Spawn_Location", spawnLocation },
        };
        WriteTxt(LogCategory.Combat, "enemy_spawn", data);
        WriteCsv(LogCategory.Combat, "enemy_spawn", data);
    }

    public void LogEnemyStartAttack(int enemyNum)
    {
        var data = new Dictionary<string, object>
        {
            { "Timestamp", GetLocalTime() },
            { "Enemy_Num", enemyNum },
        };
        WriteTxt(LogCategory.Combat, "enemy_attack", data);
        WriteCsv(LogCategory.Combat, "enemy_attack", data);
    }

    public void LogEnemyKilled(int waveNumber, string enemyType, string defeatedBy)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Enemy_Type", enemyType },
            { "Defeated_By", defeatedBy },
            { "Enemy_DestroyedTime", GetLocalTime() }
        };
        WriteTxt(LogCategory.Combat, "enemy_killed", data);
        WriteCsv(LogCategory.Combat, "enemy_killed", data);
    }

    public void LogPlayerDefend(int waveNumber, int playerAttackCount, int playerHitCount)
    {
        var data = new Dictionary<string, object>
        {
            { "Wave", waveNumber },
            { "Timestamp", GetLocalTime() },
            { "Player_AttackCount", playerAttackCount },
            { "Player_AttackHitCount", playerHitCount }
        };
        WriteTxt(LogCategory.Combat, "player_defend", data);
        WriteCsv(LogCategory.Combat, "player_defend", data);
    }
    #endregion

    #region Movement
    public void LogPlayerExitBase()
    {
        ClearMovementValue();
        var data = new Dictionary<string, object>
        {
            { "Wave", waveCount },
            { "Timestamp", GetLocalTime() },
            { "Exit_Count_Session", ++exitCount },
        };
        WriteTxt(LogCategory.Movement, "player_exit_base", data);
        WriteCsv(LogCategory.Movement, "player_exit_base", data);
    }
    public void LogPlayerEnterBase()
    {

        var data = new Dictionary<string, object>
        {
            { "Wave", waveCount },
            { "Timestamp", GetLocalTime() },
            {"Player_Move_Distance", playerMoveDistance },
            {"Player_Max_Move_Distance", playerMaxMoveDistance },
        };
        WriteTxt(LogCategory.Movement, "player_enter_base", data);
        WriteCsv(LogCategory.Movement, "player_enter_base", data);
    }
    #endregion
    bool OnWantsToQuit()
    {
        LogSessionEnd();

        foreach (var kv in writers) kv.Value?.Flush();
        foreach (var kv in writers) kv.Value?.Dispose();
        writers.Clear();

        foreach (var kv in csvWriters) kv.Value?.Flush();
        foreach (var kv in csvWriters) kv.Value?.Dispose();
        csvWriters.Clear();

        // --- csv만 압축 ---
        foreach (var csvPath in Directory.EnumerateFiles(sessionDir, "*.csv"))
        {
            // 이미 gz가 있다면 덮어쓰기
            var gzPath = csvPath + ".gz";
            using var input = File.OpenRead(csvPath);
            using var output = File.Create(gzPath);
            using var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal);
            input.CopyTo(gzip);
            // 원본 csv는 유지 (요청사항)
        }

        return true;
    }
    void ClearMovementValue()
    {
        playerLastPosition = new Vector2(0, 0);
        playerMoveDistance = 0f;
        playerMaxMoveDistance = 0f;
        maxDistanceToOrigin = 0f;
    }
}
