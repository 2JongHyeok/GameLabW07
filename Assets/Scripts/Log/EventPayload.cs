using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class EventPayload
{
    public string event_name;
    public string event_time_utc; // (구버전 호환용) 필요 시 사용
    public string ts; // ISO8601 UTC
    public double t; // 세션 시작 후 경과초


    public string session_id;
    public string user_id;
    public string build_version;
    public string scene;
    public string app_platform;
    public string app_locale;
    public int event_version;
    public int frame;
    public Vector3 pos;


    public Dictionary<string, object> data = new Dictionary<string, object>();
}