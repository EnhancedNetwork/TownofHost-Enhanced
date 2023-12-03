using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridArrangeV : MonoBehaviour
{
    public GridArrangeV(IntPtr ptr) : base(ptr) { }

    public void DeclareCells()
    {
        cells = new Il2CppSystem.Collections.Generic.List<Transform>();
    }

    public void CheckCurrentChildren()
    {
        GetChildsActive();
        cells.Clear();

        foreach (var childTransform in transform)
        {
            cells.Add(item: childTransform.TryCast<Transform>());
        }

        ArrangeChilds();
    }

    private void GetChildsActive()
    {
        currentChildren.Clear();
        foreach (var obj in base.transform)
        {
            Transform transform = obj.TryCast<Transform>();
            if (transform == null) { return; }
            if (!transform.gameObject.activeSelf) { return; }
            currentChildren.Add(transform);
        }
    }

    public void ArrangeChilds()
    {
        float num = ((Alignment == GridArrangeV.StartAlign.Left) ? (-CellSize.x) : CellSize.x);
        for (int i = 0; i < cells.Count; i++)
        {
            float num2 = (float)(i % MaxColumns) * num;
            float num3 = Mathf.Floor((float)(i / MaxColumns)) * CellSize.y;
            Transform transform2 = cells[i];
            transform2.position = new Vector3(num2, num3, -1);
            transform2.localPosition = new Vector3(transform2.position.x, transform2.position.y, -1);
        }
    }

    public Vector2 CellSize;

    public GridArrangeV.StartAlign Alignment;

    public int MaxColumns = 3;

    private Il2CppSystem.Collections.Generic.List<Transform> cells;

    private static Il2CppSystem.Collections.Generic.List<Transform> currentChildren = new Il2CppSystem.Collections.Generic.List<Transform>();

    public enum StartAlign
    {
        Left,
        Right
    }
}