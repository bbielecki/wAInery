using System.Diagnostics;
using System.Xml.Linq;

namespace wAInery.Contracts.Tests;

// #34: run the actual compiled checker against XML-only synthetic repositories.
public sealed class ArchitectureBoundaryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Current_project_graph_is_accepted_in_an_isolated_repository()
    {
        using var repository = SyntheticRepository.Create();
        var result = await RunChecker(repository);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("PASS:", result.Output);
    }

    [Fact]
    public async Task Public_contract_cannot_reference_shared_persistence_implementation()
    {
        using var repository = SyntheticRepository.Create();
        repository.AddReference(
            "src/Modules/Weather/Contracts/wAInery.Modules.Weather.Contracts.csproj",
            "src/Persistence/wAInery.Persistence.csproj");
        var result = await RunChecker(repository);
        Assert.NotEqual(0, result.ExitCode);
        var diagnostic = result.Output + result.Error;
        Assert.Contains("contract", diagnostic, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wAInery.Persistence.csproj", diagnostic);
        Assert.Contains("wAInery.Modules.Weather.Contracts.csproj", diagnostic);
    }

    private sealed record CheckerResult(int ExitCode, string Output, string Error);

    private static async Task<CheckerResult> RunChecker(SyntheticRepository repository)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repository.Root,
            UseShellExecute = false,
            CreateNoWindow = true,
            ErrorDialog = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(repository.CheckerPath);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start architecture checker.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            return new(process.ExitCode, await output, await error);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    try { process.Kill(); } // Only this owned checker PID, never a process-name sweep.
                    catch (InvalidOperationException) when (process.HasExited) { }
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await process.WaitForExitAsync(cleanup.Token);
                }
            }
            finally
            {
                deadline.Cancel();
                // Observe both reads even when cancellation/setup/checker failure unwinds.
                try { await Task.WhenAll(output, error); }
                catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
            }
        }
    }

    private sealed class SyntheticRepository : IDisposable
    {
        private const string Prefix = "wainery-architecture-boundary-";
        private readonly string temporaryParent;
        public string Root { get; }
        public string CheckerPath => Within(Root, "tests/Architecture/runner/wAInery.ArchitectureChecks.dll");

        private SyntheticRepository(string root)
        {
            Root = Path.GetFullPath(root);
            temporaryParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static SyntheticRepository Create()
        {
            var sourceRoot = FindRepositoryRoot();
            var repository = new SyntheticRepository(Directory.CreateTempSubdirectory(Prefix).FullName);
            try
            {
                var solution = XDocument.Load(Path.Combine(sourceRoot, "wAInery.slnx"));
                File.Copy(Path.Combine(sourceRoot, "wAInery.slnx"), Within(repository.Root, "wAInery.slnx"));
                foreach (var project in solution.Descendants("Project"))
                {
                    var relative = (string?)project.Attribute("Path")
                        ?? throw new InvalidOperationException("Solution project has no path.");
                    var destination = Within(repository.Root, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(Within(sourceRoot, relative), destination);
                }

                // ProjectReference ReferenceOutputAssembly=false ensures this tool
                // is built; it is never copied from a different owner's worktree.
                var testOutput = new DirectoryInfo(AppContext.BaseDirectory);
                var configuration = testOutput.Parent?.Name
                    ?? throw new InvalidOperationException("Cannot determine test configuration.");
                var toolOutput = Path.Combine(sourceRoot, "tests", "Architecture", "bin", configuration, testOutput.Name);
                var runner = Path.GetDirectoryName(repository.CheckerPath)!;
                Directory.CreateDirectory(runner);
                foreach (var suffix in new[] { ".dll", ".deps.json", ".runtimeconfig.json" })
                {
                    var name = "wAInery.ArchitectureChecks" + suffix;
                    var source = Path.Combine(toolOutput, name);
                    if (!File.Exists(source))
                        throw new FileNotFoundException("Build ArchitectureChecks for the same test configuration first.", source);
                    File.Copy(source, Path.Combine(runner, name));
                }
                return repository;
            }
            catch
            {
                repository.Dispose();
                throw;
            }
        }

        public void AddReference(string sourceRelativePath, string targetRelativePath)
        {
            var source = Within(Root, sourceRelativePath);
            var target = Within(Root, targetRelativePath);
            var project = XDocument.Load(source);
            var ns = project.Root!.Name.Namespace;
            project.Root.Add(new XElement(ns + "ItemGroup",
                new XElement(ns + "ProjectReference", new XAttribute("Include",
                    Path.GetRelativePath(Path.GetDirectoryName(source)!, target)))));
            project.Save(source);
        }

        public void Dispose()
        {
            // Only the unique root created by this instance can be recursively removed.
            var resolved = Path.GetFullPath(Root);
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!string.Equals(Path.GetDirectoryName(resolved), temporaryParent, comparison)
                || !Path.GetFileName(resolved).StartsWith(Prefix, StringComparison.Ordinal)
                || !string.Equals(resolved, Root, comparison))
                throw new InvalidOperationException("Refusing cleanup outside the owned synthetic repository.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, recursive: true);
        }

        private static string Within(string root, string relative)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(fullRoot, relative));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!candidate.StartsWith(fullRoot + Path.DirectorySeparatorChar, comparison))
                throw new InvalidOperationException("Synthetic project path escapes its root.");
            return candidate;
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "wAInery.slnx"))) return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException("Cannot locate repository for synthetic XML fixtures.");
        }
    }
}
