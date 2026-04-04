using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GestaoCulto.API.Versioning
{
    public static class BuildVersionProvider
    {
        public static BuildVersionInfo Load(IConfiguration configuration, string contentRootPath, string environmentName)
        {
            var info = new BuildVersionInfo
            {
                Name = configuration["AppInfo:Name"] ?? "gestaoculto-api",
                Version = ResolveVersion(contentRootPath),
                Commit = ResolveCommit(contentRootPath),
                BuildDate = ResolveBuildDate(),
                Environment = string.IsNullOrWhiteSpace(environmentName) ? "Production" : environmentName
            };

            var metadataPath = Path.Combine(contentRootPath, "version.json");
            if (!File.Exists(metadataPath))
            {
                return info;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
                var root = document.RootElement;

                if (root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                {
                    info.Name = name.GetString() ?? info.Name;
                }

                if (root.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.String)
                {
                    info.Version = version.GetString() ?? info.Version;
                }

                if (root.TryGetProperty("commit", out var commit) && commit.ValueKind == JsonValueKind.String)
                {
                    info.Commit = commit.GetString() ?? info.Commit;
                }

                if (root.TryGetProperty("buildDate", out var buildDate) && buildDate.ValueKind == JsonValueKind.String)
                {
                    info.BuildDate = buildDate.GetString() ?? info.BuildDate;
                }
            }
            catch
            {
                return info;
            }

            return info;
        }

        private static string ResolveVersion(string contentRootPath)
        {
            var candidate = TryFindFile(contentRootPath, "VERSION");
            if (candidate != null)
            {
                var value = File.ReadAllText(candidate).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return "0.0.0";
        }

        private static string ResolveCommit(string contentRootPath)
        {
            var fromEnv = Environment.GetEnvironmentVariable("GIT_COMMIT");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var headPath = TryFindFile(contentRootPath, Path.Combine(".git", "HEAD"));
            if (headPath == null)
            {
                return "unknown";
            }

            var gitRoot = Directory.GetParent(headPath)?.Parent?.FullName;
            if (string.IsNullOrWhiteSpace(gitRoot))
            {
                return "unknown";
            }

            var head = File.ReadAllText(headPath).Trim();
            if (head.StartsWith("ref:"))
            {
                var refPath = head.Substring(5).Trim();
                var fullRefPath = Path.Combine(gitRoot, refPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullRefPath))
                {
                    var value = File.ReadAllText(fullRefPath).Trim();
                    return ShortCommit(value);
                }

                return "unknown";
            }

            return ShortCommit(head);
        }

        private static string ResolveBuildDate()
        {
            var fromEnv = Environment.GetEnvironmentVariable("BUILD_DATE_UTC");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var timestamp = File.GetLastWriteTimeUtc(assemblyPath);
            return timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        private static string? TryFindFile(string startPath, string relativeFilePath)
        {
            var current = new DirectoryInfo(startPath);

            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, relativeFilePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            return null;
        }

        private static string ShortCommit(string commit)
        {
            if (string.IsNullOrWhiteSpace(commit))
            {
                return "unknown";
            }

            var value = commit.Trim();
            return value.Length <= 7 ? value : value.Substring(0, 7);
        }
    }
}
