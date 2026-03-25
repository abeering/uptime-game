using UnityEngine;

public static class ScanBarFormatter
{
    public static string BuildOperationsScanBar(
        int stageIndex,
        float confidence01,
        bool isComplete,
        bool willBeDropped,
        int stageCount = 3,
        char activeStageChar = '='
    )
    {
        confidence01 = Mathf.Clamp01(confidence01);

        if (isComplete)
        {
            string completeBar = "[" + new string('#', stageCount) + "]";
            return willBeDropped ? $"{completeBar} !" : completeBar;
        }

        string bar = BuildBarOnly(stageIndex, isComplete, stageCount, activeStageChar);
        int pct = Mathf.RoundToInt(confidence01 * 100f);

        string result = $"{bar} {pct}%";

        if (willBeDropped)
            result += " !";

        return result;
    }

    public static string BuildWorldScanTag(
        int stageIndex,
        float confidence01,
        bool isComplete,
        bool willBeDropped,
        int stageCount = 3,
        char activeStageChar = '='
    )
    {
        confidence01 = Mathf.Clamp01(confidence01);

        if (isComplete)
        {
            string completeBar = "[" + new string('#', stageCount) + "]";
            return willBeDropped ? $"{completeBar}\n!" : completeBar;
        }

        string bar = BuildBarOnly(stageIndex, isComplete, stageCount, activeStageChar);
        int pct = Mathf.RoundToInt(confidence01 * 100f);

        string detail = willBeDropped ? $"{pct}% !" : $"{pct}%";

        return $"{bar}\n{detail}";
    }

    private static string BuildBarOnly(
        int stageIndex,
        bool isComplete,
        int stageCount,
        char activeStageChar
    )
    {
        if (isComplete)
            return "[" + new string('#', stageCount) + "]";

        stageIndex = Mathf.Clamp(stageIndex, 0, stageCount - 1);

        char[] chars = new char[stageCount];

        for (int i = 0; i < stageCount; i++)
        {
            if (i < stageIndex)
                chars[i] = '#';
            else if (i == stageIndex)
                chars[i] = activeStageChar;
            else
                chars[i] = '-';
        }

        return "[" + new string(chars) + "]";
    }
}