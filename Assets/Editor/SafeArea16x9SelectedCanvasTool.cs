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
            Undo.RecordObject(child, "Move UI Under SafeArea16x9Root");
            Undo.SetTransformParent(child, safeAreaTransform, "Move UI Under SafeArea16x9Root");
            child.SetParent(safeAreaTransform, false);
            movedObjects.Add(GetHierarchyPath(child));
        }

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
}
#endif
