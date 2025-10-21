// WorldAnalysisLogger.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;       // 파일 입출력을 위해 꼭 필요합니다!
using System;         // 날짜와 시간을 위해 꼭 필요합니다!
using System.Text;    // 긴 텍스트를 효율적으로 만들기 위해 필요합니다!

public class WorldAnalysisLogger : MonoBehaviour
{
    [Header("분석 대상")]
    [Tooltip("월드에 있는 메인 타일맵을 연결해주세요.")]
    [SerializeField] private Tilemap worldTilemap;

    [Header("설정")]
    [Tooltip("월드 생성이 끝날 때까지 기다릴 시간 (초)")]
    [SerializeField] private float waitSecondsBeforeAnalysis = 2.0f;
    
    [Tooltip("로그 파일 이름")]
    [SerializeField] private string logFileName = "World_Analysis_Report.txt";

    void Start()
    {
        if (worldTilemap == null)
        {
            return;
        }
        StartCoroutine(AnalyzeAfterDelay());
    }

    private IEnumerator AnalyzeAfterDelay()
    {
        yield return new WaitForSeconds(waitSecondsBeforeAnalysis);
        
        AnalyzeAndLogWorldOres();
    }

    public void AnalyzeAndLogWorldOres()
    {

        // 1. 모든 타일의 개별 카운트를 먼저 집계합니다.
        Dictionary<string, int> rawCounts = new Dictionary<string, int>();
        int totalOreCount = 0;

        worldTilemap.CompressBounds();
        foreach (var pos in worldTilemap.cellBounds.allPositionsWithin)
        {
            if (worldTilemap.HasTile(pos))
            {
                TileBase tile = worldTilemap.GetTile(pos);
                string tileName = tile.name;

                if (rawCounts.ContainsKey(tileName))
                {
                    rawCounts[tileName]++;
                }
                else
                {
                    rawCounts[tileName] = 1;
                }
                totalOreCount++;
            }
        }
        
        // 2. 집계된 데이터를 기반으로 리포트를 작성하고 파일에 저장합니다.
        LogResultsAsReport(rawCounts, totalOreCount);
    }

    // ✨ --- 이 함수가 완전히 새로워졌습니다! --- ✨
    /// <summary>
    /// 분석 결과를 사람이 읽기 쉬운 리포트 형식으로 .txt 파일에 누적 기록합니다.
    /// </summary>
    private void LogResultsAsReport(Dictionary<string, int> rawCounts, int total)
    {
        string filePath = Path.Combine(Application.dataPath, logFileName);
        StringBuilder report = new StringBuilder();

        // --- 그룹화된 데이터 생성 ---
        Dictionary<string, int> groupedCounts = new Dictionary<string, int>
        {
            { "Stone", 0 }, { "Coal", 0 }, { "Iron", 0 }, { "Gold", 0 }, { "Diamond", 0 }, { "Other", 0 }
        };

        foreach (var pair in rawCounts)
        {
            string name = pair.Key;
            int count = pair.Value;

            if (name.EndsWith("_Stone_Tile")) groupedCounts["Stone"] += count;
            else if (name == "CoalOre_Tile") groupedCounts["Coal"] += count;
            else if (name == "IronOre_Tile") groupedCounts["Iron"] += count;
            else if (name == "GoldOre_Tile") groupedCounts["Gold"] += count;
            else if (name == "Diamond_Tile") groupedCounts["Diamond"] += count;
            else groupedCounts["Other"] += count; // 예상치 못한 타일은 'Other'로 집계
        }

        // --- 리포트 내용 생성 시작 ---
        report.AppendLine("==============================================================");
        report.AppendLine($" 분석 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine("--------------------------------------------------------------");
        report.AppendLine();
        report.AppendLine("      [ 월드 생성 분석 리포트 ]");
        report.AppendLine();
        report.AppendLine($"  > 발견된 총 타일 수: {total:N0}개");
        report.AppendLine();
        
        // --- 1. 상세 분포 ---
        report.AppendLine("  ▼ 상세 분포 (개수 순) ▼");
        var sortedRawCounts = rawCounts.OrderByDescending(pair => pair.Value);
        foreach (var oreEntry in sortedRawCounts)
        {
            float percentage = (total > 0) ? (float)oreEntry.Value / total * 100f : 0f;
            report.AppendLine($"    - {oreEntry.Key,-20} : {oreEntry.Value,8:N0}개 ({percentage,6:F2}%)");
        }
        report.AppendLine();

        // --- 2. 최종 요약 (가장 중요) ---
        report.AppendLine("  ▼ 최종 광물 비율 요약 (중요도 순) ▼");
        string[] reportOrder = { "Stone", "Coal", "Iron", "Gold", "Diamond", "Other" };

        foreach (string oreType in reportOrder)
        {
            if (groupedCounts.ContainsKey(oreType) && groupedCounts[oreType] > 0)
            {
                int count = groupedCounts[oreType];
                float percentage = (total > 0) ? (float)count / total * 100f : 0f;
                report.AppendLine($"    - {oreType,-10} : {percentage,7:F2}% ({count,8:N0}개)");
            }
        }
        
        report.AppendLine();
        report.AppendLine("==============================================================");
        report.AppendLine(); // 리포트 사이에 공백 추가

        // --- 리포트 내용 생성 끝 ---
        File.AppendAllText(filePath, report.ToString());
    }
}