using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

public static class GitHubPagesWebGLBuild
{
    private const string OutputDirectory = "build";

    [MenuItem("Build/GitHub Pages WebGL")]
    public static void BuildForGitHubPages()
    {
        ConfigureWebGLForGitHubPages();

        if (Directory.Exists(OutputDirectory))
        {
            Directory.Delete(OutputDirectory, true);
        }

        Directory.CreateDirectory(OutputDirectory);

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new BuildFailedException("No enabled scenes found in Build Settings.");
        }

        var report = BuildPipeline.BuildPlayer(
            scenes,
            OutputDirectory,
            BuildTarget.WebGL,
            BuildOptions.None
        );

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"WebGL build failed: {report.summary.result}");
        }

        PostProcessBuild(OutputDirectory);
    }

    [MenuItem("Build/Post-process GitHub Pages WebGL")]
    public static void PostProcessExistingBuild()
    {
        ConfigureWebGLForGitHubPages();
        PostProcessBuild(OutputDirectory);
        Debug.Log("GitHub Pages WebGL post-process complete.");
    }

    [PostProcessBuild(1000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
        {
            return;
        }

        PostProcessBuild(pathToBuiltProject);
        SyncToGitHubPagesBuild(pathToBuiltProject);
    }

    private static void ConfigureWebGLForGitHubPages()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
    }

    private static void PostProcessBuild(string outputPath)
    {
        var absoluteOutputPath = Path.GetFullPath(outputPath);
        var indexPath = Path.Combine(absoluteOutputPath, "index.html");
        var buildPath = Path.Combine(absoluteOutputPath, "Build");

        if (!File.Exists(indexPath))
        {
            throw new FileNotFoundException("Unity WebGL index.html was not found.", indexPath);
        }

        if (!Directory.Exists(buildPath))
        {
            throw new DirectoryNotFoundException($"Unity WebGL Build directory was not found: {buildPath}");
        }

        ValidateUncompressedFiles(buildPath);
        PatchIndex(indexPath);
        DeletePrecompressedFiles(buildPath);
    }

    private static void SyncToGitHubPagesBuild(string sourcePath)
    {
        var absoluteSourcePath = Path.GetFullPath(sourcePath);
        var absoluteOutputPath = Path.GetFullPath(OutputDirectory);

        if (string.Equals(absoluteSourcePath, absoluteOutputPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Directory.Exists(absoluteOutputPath))
        {
            Directory.Delete(absoluteOutputPath, true);
        }

        CopyDirectory(absoluteSourcePath, absoluteOutputPath);
        PostProcessBuild(absoluteOutputPath);

        Debug.Log($"Copied WebGL build to GitHub Pages output: {absoluteOutputPath}");
    }

    private static void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var directory in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath));
        }

        foreach (var file in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(sourcePath, destinationPath), true);
        }
    }

    private static void PatchIndex(string indexPath)
    {
        var html = File.ReadAllText(indexPath);
        var buildVersion = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        html = html
            .Replace("/build.data.br", "/build.data")
            .Replace("/build.framework.js.br", "/build.framework.js")
            .Replace("/build.wasm.br", "/build.wasm")
            .Replace("/build.data.gz", "/build.data")
            .Replace("/build.framework.js.gz", "/build.framework.js")
            .Replace("/build.wasm.gz", "/build.wasm");

        html = Regex.Replace(
            html,
            @"\s*var buildVersion = ""[^""]+"";\s*\n\s*var loaderUrl = buildUrl \+ ""/build\.loader\.js(?:\?v="" \+ buildVersion)?"";",
            string.Empty
        );

        html = html.Replace(
            "var loaderUrl = buildUrl + \"/build.loader.js\";",
            $"var buildVersion = \"{buildVersion}\";\n      var loaderUrl = buildUrl + \"/build.loader.js?v=\" + buildVersion;"
        );

        html = Regex.Replace(
            html,
            @"buildUrl \+ ""/(build\.data|build\.framework\.js|build\.wasm)(?:""|\?v="" \+ buildVersion)",
            "buildUrl + \"/$1?v=\" + buildVersion"
        );

        html = Regex.Replace(
            html,
            @"productVersion: ""[^""]+""",
            $"productVersion: \"{buildVersion}\""
        );

        File.WriteAllText(indexPath, html);
    }

    private static void DeletePrecompressedFiles(string buildPath)
    {
        foreach (var file in Directory.EnumerateFiles(buildPath, "*.br").Concat(Directory.EnumerateFiles(buildPath, "*.gz")))
        {
            File.Delete(file);
        }
    }

    private static void ValidateUncompressedFiles(string buildPath)
    {
        var requiredFiles = new[]
        {
            "build.data",
            "build.framework.js",
            "build.loader.js",
            "build.wasm",
        };

        var missingFiles = requiredFiles
            .Select(file => Path.Combine(buildPath, file))
            .Where(file => !File.Exists(file))
            .ToArray();

        if (missingFiles.Length > 0)
        {
            throw new BuildFailedException(
                "GitHub Pages build is missing uncompressed Unity files: " + string.Join(", ", missingFiles)
            );
        }
    }
}
