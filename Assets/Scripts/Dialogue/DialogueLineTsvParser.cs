using System;
using System.Collections.Generic;
using UnityEngine;

public static class DialogueLineTsvParser
{
    public static List<DialogueLine> Parse(TextAsset lineTSV)
    {
        List<DialogueLine> lines = new List<DialogueLine>();

        if (lineTSV == null)
        {
            DevLog.LogWarning("[Dialogue] Line TSV is missing.");
            return lines;
        }

        string[] rows = lineTSV.text.Split('\n');
        if (rows.Length == 0)
            return lines;

        Dictionary<string, int> header = BuildHeaderMap(rows[0]);

        for (int i = 1; i < rows.Length; i++)
        {
            string row = rows[i].TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(row))
                continue;

            string[] columns = row.Split('\t');
            DialogueLine line = ParseLine(columns, header, i + 1);
            if (line != null)
                lines.Add(line);
        }

        return lines;
    }

    private static Dictionary<string, int> BuildHeaderMap(string headerRow)
    {
        Dictionary<string, int> header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string[] columns = headerRow.TrimEnd('\r', '\n').Split('\t');

        for (int i = 0; i < columns.Length; i++)
        {
            string columnName = columns[i].Trim();
            if (!string.IsNullOrEmpty(columnName))
                header[columnName] = i;
        }

        return header;
    }

    private static DialogueLine ParseLine(string[] columns, Dictionary<string, int> header, int rowNumber)
    {
        string bodyTextKey = GetColumn(columns, header, "bodyTextKey");
        if (string.IsNullOrEmpty(bodyTextKey))
        {
            string lineID = GetColumn(columns, header, "lineID");
            DevLog.LogWarning($"[Dialogue] TSV row skipped. bodyTextKey is empty. row={rowNumber}, lineID={lineID}");
            return null;
        }

        DialogueLine line = new DialogueLine
        {
            lineID = GetColumn(columns, header, "lineID"),
            speakerID = GetColumn(columns, header, "speakerID"),
            speakerNameKey = GetColumn(columns, header, "speakerNameKey"),
            bodyTextKey = bodyTextKey,
            leftActorID = GetColumn(columns, header, "leftActorID"),
            leftExpressionID = GetColumn(columns, header, "leftExpressionID"),
            rightActorID = GetColumn(columns, header, "rightActorID"),
            rightExpressionID = GetColumn(columns, header, "rightExpressionID"),
            backgroundID = GetColumn(columns, header, "backgroundID"),
            clearBackground = ParseBool(GetColumn(columns, header, "clearBackground")),
            storyImageID = GetColumn(columns, header, "storyImageID"),
            choiceID = GetColumn(columns, header, "choiceID")
        };

        TryParseAction(GetColumn(columns, header, "lineEndAction"), rowNumber, line.lineID, "lineEndAction", out DialogueChoiceAction lineEndAction);
        line.lineEndAction = lineEndAction;
        line.lineEndActionValue = GetColumn(columns, header, "lineEndActionValue");

        string yesTextKey = GetColumn(columns, header, "yesTextKey");
        string noTextKey = GetColumn(columns, header, "noTextKey");
        string yesActionText = GetColumn(columns, header, "yesAction");
        string noActionText = GetColumn(columns, header, "noAction");
        string yesActionValue = GetColumn(columns, header, "yesActionValue");
        string noActionValue = GetColumn(columns, header, "noActionValue");
        string yesNextLineID = GetColumn(columns, header, "yesNextLineID");
        string noNextLineID = GetColumn(columns, header, "noNextLineID");
        bool hasValidYesAction = TryParseAction(yesActionText, rowNumber, line.lineID, "yesAction", out DialogueChoiceAction yesAction);
        bool hasValidNoAction = TryParseAction(noActionText, rowNumber, line.lineID, "noAction", out DialogueChoiceAction noAction);
        bool hasChoice = !string.IsNullOrEmpty(line.choiceID)
            || !string.IsNullOrEmpty(yesTextKey)
            || !string.IsNullOrEmpty(noTextKey)
            || !string.IsNullOrEmpty(yesNextLineID)
            || !string.IsNullOrEmpty(noNextLineID)
            || hasValidYesAction
            || hasValidNoAction;

        if (!hasChoice && (!string.IsNullOrEmpty(yesActionText) || !string.IsNullOrEmpty(noActionText)))
        {
            DevLog.LogWarning($"[Dialogue] Invalid choice action ignored. Possible TSV column shift. row={rowNumber}, lineID={line.lineID}, yesAction={yesActionText}, noAction={noActionText}");
        }

        if (hasChoice)
        {
            line.choice = new DialogueChoice
            {
                hasChoice = true,
                yesTextKey = yesTextKey,
                noTextKey = noTextKey,
                yesAction = yesAction,
                noAction = noAction,
                yesActionValue = yesActionValue,
                noActionValue = noActionValue,
                yesNextLineID = yesNextLineID,
                noNextLineID = noNextLineID
            };
        }

        return line;
    }

    private static string GetColumn(string[] columns, Dictionary<string, int> header, string columnName)
    {
        if (!header.TryGetValue(columnName, out int index))
            return "";

        if (index < 0 || index >= columns.Length)
            return "";

        return columns[index].Trim();
    }

    private static bool TryParseAction(string actionText, int rowNumber, string lineID, string columnName, out DialogueChoiceAction action)
    {
        action = DialogueChoiceAction.None;

        if (string.IsNullOrEmpty(actionText))
            return false;

        if (IsDialogueChoiceActionName(actionText) && Enum.TryParse(actionText, true, out action))
            return true;

        DevLog.LogWarning($"[Dialogue] Invalid DialogueChoiceAction ignored. row={rowNumber}, lineID={lineID}, column={columnName}, action={actionText}");
        action = DialogueChoiceAction.None;
        return false;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || value == "1";
    }

    private static bool IsDialogueChoiceActionName(string actionText)
    {
        foreach (string name in Enum.GetNames(typeof(DialogueChoiceAction)))
        {
            if (string.Equals(name, actionText, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
