using System.Text;

namespace Qgp.Api.Application;

/// <summary>
/// Unified diff line-based, KHÔNG phụ thuộc thư viện ngoài (DOC-F-06). Tài liệu quy trình
/// thường ngắn → LCS bằng DP O(n·m) đủ nhanh. Xuất 1 hunk gồm vùng thay đổi + context.
/// </summary>
public static class UnifiedDiff
{
    public static string Compute(string? fromText, string? toText, string fromLabel, string toLabel, int context = 3)
    {
        var a = SplitLines(fromText);
        var b = SplitLines(toText);
        var script = BuildScript(a, b);

        var first = script.FindIndex(x => x.tag != ' ');
        if (first < 0) return string.Empty; // hai bản giống hệt
        var last = script.FindLastIndex(x => x.tag != ' ');

        var start = Math.Max(0, first - context);
        var end = Math.Min(script.Count - 1, last + context);
        var hunk = script.GetRange(start, end - start + 1);

        int aStart = 0, aCount = 0, bStart = 0, bCount = 0;
        foreach (var it in hunk)
        {
            if (it.tag != '+') { if (aCount == 0) aStart = it.aIdx + 1; aCount++; }
            if (it.tag != '-') { if (bCount == 0) bStart = it.bIdx + 1; bCount++; }
        }

        var sb = new StringBuilder();
        sb.Append("--- ").Append(fromLabel).Append('\n');
        sb.Append("+++ ").Append(toLabel).Append('\n');
        sb.Append("@@ -").Append(aStart).Append(',').Append(aCount)
          .Append(" +").Append(bStart).Append(',').Append(bCount).Append(" @@\n");
        foreach (var it in hunk)
            sb.Append(it.tag).Append(it.text).Append('\n');
        return sb.ToString();
    }

    private static string[] SplitLines(string? s) =>
        (s ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    /// <summary>Edit script (LCS): mỗi dòng gắn tag ' ' (giữ) / '-' (xoá) / '+' (thêm) + chỉ số dòng gốc.</summary>
    private static List<(char tag, int aIdx, int bIdx, string text)> BuildScript(string[] a, string[] b)
    {
        int n = a.Length, m = b.Length;
        var dp = new int[n + 1, m + 1];
        for (int i = n - 1; i >= 0; i--)
            for (int j = m - 1; j >= 0; j--)
                dp[i, j] = a[i] == b[j] ? dp[i + 1, j + 1] + 1 : Math.Max(dp[i + 1, j], dp[i, j + 1]);

        var res = new List<(char, int, int, string)>();
        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (a[x] == b[y]) { res.Add((' ', x, y, a[x])); x++; y++; }
            else if (dp[x + 1, y] >= dp[x, y + 1]) { res.Add(('-', x, y, a[x])); x++; }
            else { res.Add(('+', x, y, b[y])); y++; }
        }
        while (x < n) { res.Add(('-', x, y, a[x])); x++; }
        while (y < m) { res.Add(('+', x, y, b[y])); y++; }
        return res;
    }
}
