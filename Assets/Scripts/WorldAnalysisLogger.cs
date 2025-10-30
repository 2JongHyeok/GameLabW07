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
        // worldTilemap이 인스펙터에서 할당되지 않았으면 분석을 시작하지 않습니다.
        if (worldTilemap == null)
        {
            Debug.LogWarning("WorldAnalysisLogger: worldTilemap이 연결되지 않아 분석을 시작할 수 없습니다.");
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
        // 각 범위별 타일 카운트 및 총 개수를 저장할 딕셔너리 및 변수
        Dictionary<string, int> rawCounts0_20 = new Dictionary<string, int>();
        int total0_20 = 0;
        Dictionary<string, int> rawCounts20_50 = new Dictionary<string, int>();
        int total20_50 = 0;
        Dictionary<string, int> rawCounts50_80 = new Dictionary<string, int>();
        int total50_80 = 0;
        Dictionary<string, int> rawCounts80_110 = new Dictionary<string, int>();
        int total80_110 = 0;

        // 전체 월드 타일맵의 총 타일 개수 (참고용)
        Dictionary<string, int> overallRawCounts = new Dictionary<string, int>();
        int overallTotal = 0;

        worldTilemap.CompressBounds();
        foreach (var pos in worldTilemap.cellBounds.allPositionsWithin)
        {
            if (worldTilemap.HasTile(pos))
            {
                TileBase tile = worldTilemap.GetTile(pos);
                string tileName = tile.name;
                Vector2 worldPos = worldTilemap.CellToWorld(pos); // 셀 위치를 월드 좌표로 변환
                float distanceFromOrigin = worldPos.magnitude; // (0,0)으로부터의 거리 계산

                // 전체 카운트 집계
                AddTileToCounts(overallRawCounts, tileName);
                overallTotal++;

                // 범위별 카운트 집계
                if (distanceFromOrigin >= 0 && distanceFromOrigin <= 40)
                {
                    AddTileToCounts(rawCounts0_20, tileName);
                    total0_20++;
                }
                else if (distanceFromOrigin > 40 && distanceFromOrigin <= 60)
                {
                    AddTileToCounts(rawCounts20_50, tileName);
                    total20_50++;
                }
                else if (distanceFromOrigin > 60 && distanceFromOrigin <= 85)
                {
                    AddTileToCounts(rawCounts50_80, tileName);
                    total50_80++;
                }
                else if (distanceFromOrigin > 85 && distanceFromOrigin <= 120)
                {
                    AddTileToCounts(rawCounts80_110, tileName);
                    total80_110++;
                }
            }
        }
        
        // 모든 분석 결과를 하나의 리포트로 작성하여 파일에 저장
        GenerateFullReport(
            overallRawCounts, overallTotal,
            new Dictionary<string, int>[] { rawCounts0_20, rawCounts20_50, rawCounts50_80, rawCounts80_110 },
            new int[] { total0_20, total20_50, total50_80, total80_110 },
            new string[] { "0 ~ 40", "40 ~ 60", "60 ~ 85", "85 ~ 120" }
        );
    }

    private void AddTileToCounts(Dictionary<string, int> counts, string tileName)
    {
        if (counts.ContainsKey(tileName))
        {
            counts[tileName]++;
        }
        else
        {
            counts[tileName] = 1;
        }
    }

    /// <summary>
    /// 전체 분석 결과를 사람이 읽기 쉬운 리포트 형식으로 .txt 파일에 기록합니다.
    /// </summary>
    private void GenerateFullReport(
        Dictionary<string, int> overallRawCounts, int overallTotal,
        Dictionary<string, int>[] rangeRawCounts, int[] rangeTotals, string[] rangeLabels)
    {
        string filePath = Path.Combine(Application.dataPath, logFileName);
        StringBuilder report = new StringBuilder();

        report.AppendLine("==============================================================");
        report.AppendLine($" 분석 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine("--------------------------------------------------------------");
        report.AppendLine();
        report.AppendLine("      [ 월드 생성 분석 리포트 ]");
        report.AppendLine();
        report.AppendLine($"  > 전체 월드에서 발견된 총 타일 수: {overallTotal:N0}개");
        report.AppendLine();
        
        // 전체 월드 상세 분포
        report.AppendLine("  ▼ 전체 월드 상세 분포 (개수 순) ▼");
        var sortedOverallRawCounts = overallRawCounts.OrderByDescending(pair => pair.Value);
        foreach (var oreEntry in sortedOverallRawCounts)
        {
            float percentage = (overallTotal > 0) ? (float)oreEntry.Value / overallTotal * 100f : 0f;
            report.AppendLine($"    - {oreEntry.Key,-20} : {oreEntry.Value,8:N0}개 ({percentage,6:F2}%)");
        }
        report.AppendLine();
        report.AppendLine("--------------------------------------------------------------");
        report.AppendLine();

        // 각 범위별 리포트
        for (int i = 0; i < rangeRawCounts.Length; i++)
        {
            Dictionary<string, int> rawCounts = rangeRawCounts[i];
            int total = rangeTotals[i];
            string label = rangeLabels[i];

            if (total == 0)
            {
                report.AppendLine($"  [ 반지름 {label} 범위 ] - 해당 범위에 타일이 없습니다.");
                report.AppendLine();
                report.AppendLine("--------------------------------------------------------------");
                report.AppendLine();
                continue;
            }

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

            report.AppendLine($"  [ 반지름 {label} 범위 ]");
            report.AppendLine($"  > 해당 범위에서 발견된 총 타일 수: {total:N0}개");
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
            report.AppendLine("--------------------------------------------------------------");
            report.AppendLine();
        }

        report.AppendLine("==============================================================");

        // 파일에 전체 리포트 기록 (기존 내용에 이어서 쓰기)
        File.AppendAllText(filePath, report.ToString());
    }
}