// GameAnalyticsLogger (Newtonsoft 제거 버전, txt + csv 출력 전용)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

public enum LogCategory { Session, Stage, Combat, Progression, Movement, Ore, Core, Wave }

public class GameAnalyticsLogger : MonoBehaviour
{
    public static GameAnalyticsLogger instance { get; private set; }

    [Header("Build/Schema")]
    [SerializeField] string buildVersion = "6000.0.55f1";   // 빌드 버전
    [SerializeField] int eventSchemaVersion = 0;            // 로그 구조 버전

    string userId, sessionId, sessionDir;
    UTF8Encoding noBom = new UTF8Encoding(false);   // 인코딩 시 BOM 제거 - CSV 분석 도구에서 문제 방지용.

    readonly Dictionary<LogCategory, StreamWriter> writers = new();
    readonly Dictionary<LogCategory, string> fileNames = new()
{
    { LogCategory.Session,     "session.txt" },
    { LogCategory.Wave,        "wave.txt" },
    { LogCategory.Combat,      "combat.txt" },
    { LogCategory.Progression, "progression.txt" },
    { LogCategory.Movement,    "movement.txt" },
    { LogCategory.Ore,         "ore.txt" },
    { LogCategory.Core,        "core.txt" },
};

    readonly Dictionary<LogCategory, string[]> csvHeaders = new()
{
    { LogCategory.Session, new[]{"event_name","ts","t","play_time_sec"}},
    { LogCategory.Wave, new[]{"event_name","ts","t","wave","minute","second"}},
    { LogCategory.Combat, new[]{"event_name","ts","t","boss_id","arena_id","result","cause","killer_id","pos_x","pos_y"}},
    { LogCategory.Progression, new[]{"event_name","ts","t","skill_id","wave","item_id","amount","source","target_type"}},
    { LogCategory.Movement, new[]{"event_name","ts","t","pos_x","pos_y","vel_x","vel_y","vel_z","dt"}},
    { LogCategory.Ore, new[]{"event_name","ts","t","amount","wave","tile_x","tile_y","tile_type","cause"}},
    { LogCategory.Core, new[]{"event_name","ts","t","delta","hp_after","blocked_amount"}},
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
        if (!PlayerPrefs.HasKey(KEY)) { 
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
    string GetCurrentTime()
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Time.time);
        string formattedTime = $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        return formattedTime;
    }

    string GetLocalTime()
    {
        DateTime startTimeUtc = DateTime.Now;
        return startTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");

    }
    #endregion
    // === Session ===
    #region Session
    public void LogSessionStart()
    {
        var data = new Dictionary<string, object> { 
            { "StartTime",  GetLocalTime()}, 
            };
        WriteTxt(LogCategory.Session, "session_start", data);
        WriteCsv(LogCategory.Session, "session_start", data);
    }

    public void LogSessionEnd()
    {
        var data = new Dictionary<string, object> {
            {"EndTime",GetLocalTime() },
            { "play_time_sec", Math.Round(Time.realtimeSinceStartup - sessionStartRealtime, 3)}
        };
        WriteTxt(LogCategory.Session, "session_end", data);
        WriteCsv(LogCategory.Session, "session_end", data);
    }
    #endregion
    // === Stage ===
    public void LogLevelStart(string levelId, int? wave = null)
    {
        var data = new Dictionary<string, object> { { "level_id", levelId }, { "wave", wave } };
        WriteTxt(LogCategory.Stage, "level_start", data);
        WriteCsv(LogCategory.Stage, "level_start", data);
    }

    public void LogLevelComplete(string levelId, float? clearTimeSec = null)
    {
        var data = new Dictionary<string, object> { { "level_id", levelId }, { "clear_time_sec", clearTimeSec } };
        WriteTxt(LogCategory.Stage, "level_complete", data);
        WriteCsv(LogCategory.Stage, "level_complete", data);
    }

    public void LogLevelFail(string levelId, string reason = null)
    {
        var data = new Dictionary<string, object> { { "level_id", levelId }, { "reason", reason } };
        WriteTxt(LogCategory.Stage, "level_fail", data);
        WriteCsv(LogCategory.Stage, "level_fail", data);
    }

    public void LogCheckpointReach(string levelId, string checkpointId)
    {
        var data = new Dictionary<string, object> { { "level_id", levelId }, { "checkpoint_id", checkpointId } };
        WriteTxt(LogCategory.Stage, "checkpoint_reach", data);
        WriteCsv(LogCategory.Stage, "checkpoint_reach", data);
    }

    // === Combat ===
    public void LogBossAttempt(string bossId)
    {
        var data = new Dictionary<string, object> { { "boss_id", bossId } };
        WriteTxt(LogCategory.Combat, "boss_attempt", data);
        WriteCsv(LogCategory.Combat, "boss_attempt", data);
    }

    public void LogBossDefeat(string bossId)
    {
        var data = new Dictionary<string, object> { { "boss_id", bossId } };
        WriteTxt(LogCategory.Combat, "boss_defeat", data);
        WriteCsv(LogCategory.Combat, "boss_defeat", data);
    }

    public void LogCombatStart(string arenaId = null)
    {
        var data = new Dictionary<string, object> { { "arena_id", arenaId } };
        WriteTxt(LogCategory.Combat, "combat_start", data);
        WriteCsv(LogCategory.Combat, "combat_start", data);
    }

    public void LogCombatEnd(string result = null)
    {
        var data = new Dictionary<string, object> { { "result", result } };
        WriteTxt(LogCategory.Combat, "combat_end", data);
        WriteCsv(LogCategory.Combat, "combat_end", data);
    }

    public void LogPlayerDeath(string cause, Vector3 pos, string killer = null)
    {
        var data = new Dictionary<string, object> { { "cause", cause }, { "killer_id", killer }, { "pos_x", pos.x }, { "pos_y", pos.y } };
        WriteTxt(LogCategory.Combat, "player_death", data);
        WriteCsv(LogCategory.Combat, "player_death", data);
    }

    // === Progression ===
    public void LogSkillUnlock(string skillId, int? wave = null)
    {
        var data = new Dictionary<string, object> { { "skill_id", skillId }, { "wave", wave } };
        WriteTxt(LogCategory.Progression, "skill_unlock", data);
        WriteCsv(LogCategory.Progression, "skill_unlock", data);
    }

    public void LogSkillUse(string skillId, string targetType = null)
    {
        var data = new Dictionary<string, object> { { "skill_id", skillId }, { "target_type", targetType } };
        WriteTxt(LogCategory.Progression, "skill_use", data);
        WriteCsv(LogCategory.Progression, "skill_use", data);
    }

    public void LogItemAcquire(string itemId, int amount = 1, string source = null)
    {
        var data = new Dictionary<string, object> { { "item_id", itemId }, { "amount", amount }, { "source", source } };
        WriteTxt(LogCategory.Progression, "item_acquire", data);
        WriteCsv(LogCategory.Progression, "item_acquire", data);
    }

    public void LogItemUse(string itemId, int amount = 1)
    {
        var data = new Dictionary<string, object> { { "item_id", itemId }, { "amount", amount } };
        WriteTxt(LogCategory.Progression, "item_use", data);
        WriteCsv(LogCategory.Progression, "item_use", data);
    }

    public void LogItemCraft(string itemId, int amount = 1)
    {
        var data = new Dictionary<string, object> { { "item_id", itemId }, { "amount", amount } };
        WriteTxt(LogCategory.Progression, "item_craft", data);
        WriteCsv(LogCategory.Progression, "item_craft", data);
    }

    // === Movement / Ore / Core ===
    public void LogMovement(Vector3 pos, Vector3 vel, float dt)
    {
        var data = new Dictionary<string, object> { { "pos_x", pos.x }, { "pos_y", pos.y }, { "vel_x", vel.x }, { "vel_y", vel.y }, { "vel_z", vel.z }, { "dt", dt } };
        WriteTxt(LogCategory.Movement, "movement_tick", data);
        WriteCsv(LogCategory.Movement, "movement_tick", data);
    }

    public void LogOreCollect(int amount, int wave, Vector3 pos)
    {
        var data = new Dictionary<string, object> { { "amount", amount }, { "wave", wave }, { "pos_x", pos.x }, { "pos_y", pos.y } };
        WriteTxt(LogCategory.Ore, "ore_collect", data);
        WriteCsv(LogCategory.Ore, "ore_collect", data);
    }

    public void LogOreBreak(Vector3Int tile, string tileType, string cause)
    {
        var data = new Dictionary<string, object> { { "tile_x", tile.x }, { "tile_y", tile.y }, { "tile_type", tileType }, { "cause", cause } };
        WriteTxt(LogCategory.Ore, "ore_break", data);
        WriteCsv(LogCategory.Ore, "ore_break", data);
    }

    public void LogCoreHpChange(float delta, float hpAfter)
    {
        var data = new Dictionary<string, object> { { "delta", delta }, { "hp_after", hpAfter } };
        WriteTxt(LogCategory.Core, "core_hp_change", data);
        WriteCsv(LogCategory.Core, "core_hp_change", data);
    }

    public void LogShieldBlocked(float amount)
    {
        var data = new Dictionary<string, object> { { "blocked_amount", amount } };
        WriteTxt(LogCategory.Core, "shield_blocked", data);
        WriteCsv(LogCategory.Core, "shield_blocked", data);
    }

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
}
