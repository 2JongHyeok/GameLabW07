using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    [SerializeField] private List<Vector2> points = new List<Vector2>();
    [SerializeField] private float lineThickness = 3f;

    protected override void Awake()
    {
        base.Awake();
        // UI Default Material 설정
        if (material == null || material == defaultMaterial)
        {
            material = defaultMaterial;
        }
    }

    public float LineThickness
    {
        get { return lineThickness; }
        set
        {
            lineThickness = value;
            SetVerticesDirty();
        }
    }

    public List<Vector2> Points
    {
        get { return points; }
        set
        {
            points = value;
            SetVerticesDirty();
        }
    }

    public void SetPoints(Vector2 start, Vector2 end)
    {
        points.Clear();
        points.Add(start);
        points.Add(end);
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null || points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            DrawLine(vh, points[i], points[i + 1], lineThickness);
        }
    }

    private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (thickness / 2f);

        // 선의 4개 꼭지점
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        int startIndex = vh.currentVertCount;

        // Bottom left
        vertex.position = start - perpendicular;
        vh.AddVert(vertex);

        // Top left
        vertex.position = start + perpendicular;
        vh.AddVert(vertex);

        // Top right
        vertex.position = end + perpendicular;
        vh.AddVert(vertex);

        // Bottom right
        vertex.position = end - perpendicular;
        vh.AddVert(vertex);

        // 삼각형 2개로 사각형 만들기
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
