#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SafeArea16x9SelectedCanvasTool
{
    private const string MenuPath = "Tools/DevilsCut/UI/Apply SafeArea16x9 To Selected Canvas";
    private const string SafeAreaName = "SafeArea16x9Root";
    private const float SafeAreaAspect = 1.7777778f;

    private static readonly string[] ExcludedNameParts =
    {
        "FullScreenClickCatcher",
        "ClickCatcher",
        "Fade",
        "Dim",
        "Blocker",
        "Loading"
    };

    [MenuItem(MenuPath)]
    public static void ApplyToSelectedCanvas()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("[SafeArea16x9] 선택된 GameObject가 없습니다. Canvas가 붙은 GameObject를 선택한 뒤 다시 실행하세요.");
            return;
        }

        int processedCount = 0;
        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(selectedObject))
            {
                Debug.LogWarning($"[SafeArea16x9] '{selectedObject.name}'은 Project asset이므로 건너뜁니다. 씬 Hierarchy의 Canvas를 선택하세요.");
                continue;
            }

            Canvas canvas = selectedObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning($"[SafeArea16x9] '{GetHierarchyPath(selectedObject.transform)}'에는 Canvas 컴포넌트가 없어 건너뜁니다.");
                continue;
            }

            ApplyToCanvas(canvas);
            processedCount++;
        }

        Debug.Log($"[SafeArea16x9] 선택된 Canvas 처리 완료: {processedCount}개");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateApplyToSelectedCanvas()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            return false;
        }

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject != null && !EditorUtility.IsPersistent(selectedObject) && selectedObject.GetComponent<Canvas>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyToCanvas(Canvas canvas)
    {
        RectTransform canvasTransform = canvas.transform as RectTransform;
        if (canvasTransform == null)
        {
            Debug.LogWarning($"[SafeArea16x9] '{canvas.name}'의 Transform이 RectTransform이 아니어서 건너뜁니다.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName($"Apply SafeArea16x9 To {canvas.name}");

        RectTransform safeAreaTransform = GetOrCreateSafeArea(canvasTransform, out bool createdSafeArea);
        ConfigureSafeArea(safeAreaTransform);

        List<string> movedObjects = new List<string>();
        List<string> excludedObjects = new List<string>();
        List<Transform> childrenToMove = new List<Transform>();

        for (int i = 0; i < canvasTransform.childCount; i++)
        {
            Transform child = canvasTransform.GetChild(i);
            if (child == safeAreaTransform)
            {
                continue;
            }

            if (ShouldExclude(child.name))
            {
                excludedObjects.Add(GetHierarchyPath(child));
                continue;
            }

            childrenToMove.Add(child);
        }

        foreach (Transform child in childrenToMove)
        {
            RectTransformSnapshot snapshot = RectTransformSnapshot.Capture(child);
            Undo.SetTransformParent(child, safeAreaTransform, "Move UI Under SafeArea16x9Root");
            snapshot.Restore(child);
            child.localScale = Vector3.one;
            EditorUtility.SetDirty(child);
            movedObjects.Add(GetHierarchyPath(child));
        }

        safeAreaTransform.localScale = Vector3.one;
        EditorUtility.SetDirty(safeAreaTransform);
        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);

        LogResult(canvas, safeAreaTransform, createdSafeArea, movedObjects, excludedObjects);
    }

    private static RectTransform GetOrCreateSafeArea(RectTransform canvasTransform, out bool created)
    {
        Transform existing = canvasTransform.Find(SafeAreaName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRectTransform))
        {
            created = false;
            return existingRectTransform;
        }

        GameObject safeAreaObject = new GameObject(SafeAreaName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(safeAreaObject, "Create SafeArea16x9Root");

        RectTransform safeAreaTransform = safeAreaObject.GetComponent<RectTransform>();
        safeAreaTransform.SetParent(canvasTransform, false);
        created = true;
        return safeAreaTransform;
    }

    private static void ConfigureSafeArea(RectTransform safeAreaTransform)
    {
        Undo.RecordObject(safeAreaTransform, "Configure SafeArea16x9Root");
        safeAreaTransform.anchorMin = new Vector2(0.5f, 0.5f);
        safeAreaTransform.anchorMax = new Vector2(0.5f, 0.5f);
        safeAreaTransform.pivot = new Vector2(0.5f, 0.5f);
        safeAreaTransform.anchoredPosition = Vector2.zero;
        safeAreaTransform.sizeDelta = new Vector2(1920f, 1080f);
        safeAreaTransform.localScale = Vector3.one;
        safeAreaTransform.localRotation = Quaternion.identity;

        AspectRatioFitter aspectRatioFitter = safeAreaTransform.GetComponent<AspectRatioFitter>();
        if (aspectRatioFitter == null)
        {
            aspectRatioFitter = Undo.AddComponent<AspectRatioFitter>(safeAreaTransform.gameObject);
        }

        Undo.RecordObject(aspectRatioFitter, "Configure SafeArea16x9Root");
        aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectRatioFitter.aspectRatio = SafeAreaAspect;
    }

    private static bool ShouldExclude(string objectName)
    {
        foreach (string excludedNamePart in ExcludedNameParts)
        {
            if (objectName.IndexOf(excludedNamePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void LogResult(Canvas canvas, RectTransform safeAreaTransform, bool createdSafeArea, List<string> movedObjects, List<string> excludedObjects)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[SafeArea16x9] Canvas: {GetHierarchyPath(canvas.transform)}");
        builder.AppendLine($"SafeArea: {(createdSafeArea ? "생성" : "재사용")} - {GetHierarchyPath(safeAreaTransform)}");
        builder.AppendLine($"이동한 오브젝트 ({movedObjects.Count}개):");
        AppendList(builder, movedObjects);
        builder.AppendLine($"제외한 오브젝트 ({excludedObjects.Count}개):");
        AppendList(builder, excludedObjects);

        if (excludedObjects.Count > 0)
        {
            builder.AppendLine("제외된 오브젝트는 전체 화면 클릭/페이드/차단 후보입니다. 필요하면 사용자가 직접 SafeArea 아래로 옮기세요.");
        }

        Debug.Log(builder.ToString());
    }

    private static void AppendList(StringBuilder builder, List<string> values)
    {
        if (values.Count == 0)
        {
            builder.AppendLine("- 없음");
            return;
        }

        foreach (string value in values)
        {
            builder.AppendLine($"- {value}");
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        Stack<string> names = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private struct RectTransformSnapshot
    {
        private readonly bool isRectTransform;
        private readonly Vector3 localPosition;
        private readonly Quaternion localRotation;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 pivot;

        private RectTransformSnapshot(Transform transform)
        {
            RectTransform rectTransform = transform as RectTransform;
            isRectTransform = rectTransform != null;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;

            if (rectTransform != null)
            {
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
            else
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.zero;
                anchoredPosition = Vector2.zero;
                sizeDelta = Vector2.zero;
                pivot = Vector2.zero;
            }
        }

        public static RectTransformSnapshot Capture(Transform transform)
        {
            return new RectTransformSnapshot(transform);
        }

        public void Restore(Transform transform)
        {
            Undo.RecordObject(transform, "Restore UI Transform After SafeArea Reparent");

            if (isRectTransform && transform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.pivot = pivot;
            }
            else
            {
                transform.localPosition = localPosition;
            }

            transform.localRotation = localRotation;
        }
    }
}

public static class SafeArea16x9ScaleRepairTool
{
    private const string MenuPath = "Tools/DevilsCut/UI/Repair Selected SafeArea Zero Scale";

    [MenuItem(MenuPath)]
    public static void RepairSelectedZeroScale()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("[SafeArea16x9 Repair] 선택된 GameObject가 없습니다. FacilityLevelBonusCanvas 또는 SafeArea16x9Root를 선택한 뒤 다시 실행하세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair Selected SafeArea Zero Scale");

        List<string> repairedObjects = new List<string>();
        HashSet<Transform> visitedTransforms = new HashSet<Transform>();

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null)
                continue;

            if (EditorUtility.IsPersistent(selectedObject))
            {
                Debug.LogWarning($"[SafeArea16x9 Repair] '{selectedObject.name}'은 Project asset이므로 건너뜁니다. 씬 Hierarchy의 오브젝트를 선택하세요.");
                continue;
            }

            RepairRecursive(selectedObject.transform, visitedTransforms, repairedObjects);
            EditorSceneManager.MarkSceneDirty(selectedObject.scene);
        }

        Undo.CollapseUndoOperations(undoGroup);
        LogRepairResult(repairedObjects);
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateRepairSelectedZeroScale()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
            return false;

        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject != null && !EditorUtility.IsPersistent(selectedObject))
                return true;
        }

        return false;
    }

    private static void RepairRecursive(Transform transform, HashSet<Transform> visitedTransforms, List<string> repairedObjects)
    {
        if (transform == null || !visitedTransforms.Add(transform))
            return;

        if (HasZeroScaleAxis(transform.localScale))
        {
            Undo.RecordObject(transform, "Repair SafeArea Zero Scale");
            transform.localScale = Vector3.one;
            EditorUtility.SetDirty(transform);
            repairedObjects.Add(GetHierarchyPath(transform));
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            RepairRecursive(transform.GetChild(i), visitedTransforms, repairedObjects);
        }
    }

    private static bool HasZeroScaleAxis(Vector3 scale)
    {
        return Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f) || Mathf.Approximately(scale.z, 0f);
    }

    private static void LogRepairResult(List<string> repairedObjects)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[SafeArea16x9 Repair] zero scale 복구 완료: {repairedObjects.Count}개");

        if (repairedObjects.Count == 0)
        {
            builder.AppendLine("- 복구할 zero scale 오브젝트가 없습니다.");
        }
        else
        {
            foreach (string path in repairedObjects)
            {
                builder.AppendLine($"- {path}");
            }
        }

        Debug.Log(builder.ToString());
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        Stack<string> names = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
#endif
