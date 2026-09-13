using LibGit2Sharp;

namespace Qgp.Api.Infrastructure.Git;

/// <summary>
/// Lưu nội dung normative của version vào Git (ADR-0001 docs-as-code, BR-03 immutable).
/// Commit khi publish → <c>content_git_ref</c> = commit SHA. Bare-ish working repo cục bộ
/// (data volume); sau này có thể <c>git push</c> lên GitLab (ADR-0011).
/// </summary>
public interface IGitContentStore
{
    /// <summary>Ghi + commit nội dung 1 version. Trả commit SHA (dùng cho content_git_ref).</summary>
    string CommitVersion(string docId, string version, string markdown, string authorSub, string message);

    /// <summary>Đọc nội dung version theo commit SHA (nguồn sự thật nội dung — SDD DR §).</summary>
    string? ReadVersion(string docId, string version, string sha);
}

public sealed class LibGit2GitContentStore : IGitContentStore
{
    private readonly string _repoPath;
    // static: serialize mọi commit/init trên cùng repo (index libgit2 không thread-safe; test song song
    // dùng chung 1 repo path → tránh race). Prod 1 instance nên không ảnh hưởng throughput đáng kể.
    private static readonly object _lock = new();

    public LibGit2GitContentStore(string repoPath)
    {
        _repoPath = repoPath;
        lock (_lock)
        {
            Directory.CreateDirectory(_repoPath);
            if (!Repository.IsValid(_repoPath))
                Repository.Init(_repoPath);
        }
    }

    private static string RelPath(string docId, string version) => $"{docId}/{version}.md";

    public string CommitVersion(string docId, string version, string markdown, string authorSub, string message)
    {
        lock (_lock) // serialize toàn process → tránh race index libgit2 (test song song + prod 1 instance)
        {
            var rel = RelPath(docId, version);
            var abs = Path.Combine(_repoPath, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(abs)!);
            File.WriteAllText(abs, markdown ?? string.Empty);

            var who = string.IsNullOrWhiteSpace(authorSub) ? "system" : authorSub;
            var sig = new Signature(who, $"{who}@qgp", DateTimeOffset.UtcNow);

            // Retry 1 lần cho lỗi lock tạm thời; nội dung không đổi → trả HEAD sha (idempotent).
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using var repo = new Repository(_repoPath);
                    Commands.Stage(repo, rel);
                    if (repo.RetrieveStatus(rel) == FileStatus.Unaltered && repo.Head.Tip is { } tip)
                        return tip.Sha; // không có thay đổi để commit
                    return repo.Commit(message, sig, sig, new CommitOptions { AllowEmptyCommit = false }).Sha;
                }
                catch (LibGit2SharpException) when (attempt < 1)
                {
                    // thử lại 1 lần (lock/index tạm thời)
                }
            }
        }
    }

    public string? ReadVersion(string docId, string version, string sha)
    {
        lock (_lock)
        {
            using var repo = new Repository(_repoPath);
            if (repo.Lookup(sha) is not Commit commit) return null;
            return commit[RelPath(docId, version)]?.Target is Blob blob ? blob.GetContentText() : null;
        }
    }
}
