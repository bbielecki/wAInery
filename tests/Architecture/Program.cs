using System.Xml.Linq;

var root = FindRoot(AppContext.BaseDirectory);
var solutionPath = Path.Combine(root, "wAInery.slnx");
var solution = XDocument.Load(solutionPath);
var projectPaths = solution.Descendants("Project")
    .Select(p => (string?)p.Attribute("Path"))
    .Where(p => !string.IsNullOrWhiteSpace(p))
    .Select(p => Path.GetFullPath(Path.Combine(root, p!)))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

var moduleNames = new[] { "Vineyard", "Winery", "Decisions", "Observations", "Weather", "Knowledge", "Connectivity", "AIExecution" };
var requiredPaths = new[] { "src/BuildingBlocks/wAInery.BuildingBlocks.csproj", "src/Host/wAInery.Host.csproj",
    "tests/Architecture/wAInery.ArchitectureChecks.csproj" }.Concat(moduleNames.SelectMany(module => new[]
    {
        $"src/Modules/{module}/wAInery.Modules.{module}.csproj",
        $"src/Modules/{module}/Contracts/wAInery.Modules.{module}.Contracts.csproj"
    }));
foreach (var required in requiredPaths)
    if (!projectPaths.Contains(Path.GetFullPath(Path.Combine(root, required))))
        throw new InvalidOperationException($"Required monorepo project missing from solution: {required}");

var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
foreach (var project in projectPaths)
{
    if (!File.Exists(project)) throw new FileNotFoundException("Missing solution project", project);
    var doc = XDocument.Load(project);
    var refs = doc.Descendants("ProjectReference").Select(p => (string?)p.Attribute("Include"));
    var targets = new List<string>();
    foreach (var reference in refs)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw new Exception($"Empty project reference in {project}");
        var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, reference));
        if (!projectPaths.Contains(target)) throw new Exception($"Reference outside solution: {project} -> {target}");
        var sourceModule = ModuleName(project);
        var targetModule = ModuleName(target);
        if (sourceModule != null && targetModule != null && sourceModule != targetModule && !target.Contains(
            $"{Path.DirectorySeparatorChar}Contracts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Illegal cross-module implementation reference: {project} -> {target}");
        if (sourceModule is not null && project.Contains($"{Path.DirectorySeparatorChar}Contracts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && targetModule != null)
            throw new Exception($"Public contract assembly depends on another module: {project} -> {target}");
        targets.Add(target);
    }
    graph[project] = targets;
}

var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
foreach (var project in graph.Keys) Visit(project);
Console.WriteLine($"PASS: {projectPaths.Count} solution projects, valid references, no cross-module implementation imports and no cycles.");

void Visit(string project)
{
    if (state.TryGetValue(project, out var status))
    {
        if (status == 1) throw new Exception($"Project reference cycle at {project}");
        if (status == 2) return;
    }
    state[project] = 1;
    foreach (var reference in graph[project]) Visit(reference);
    state[project] = 2;
}

static string? ModuleName(string file)
{
    var segments = file.Split(Path.DirectorySeparatorChar);
    var index = Array.FindIndex(segments, s => s.Equals("Modules", StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < segments.Length ? segments[index + 1] : null;
}

static string FindRoot(string start)
{
    var current = new DirectoryInfo(start);
    while (current != null)
    {
        if (File.Exists(Path.Combine(current.FullName, "wAInery.slnx"))) return current.FullName;
        current = current.Parent;
    }
    throw new DirectoryNotFoundException("Cannot locate wAInery.slnx");
}
