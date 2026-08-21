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
    private const string LoadingBackgroundSourcePath = "BuildAssets/WebGL/loading-background.jpg";
    private const string LoadingBackgroundFileName = "loading-background.jpg";
    private const string LoadingScreenStyleId = "gamejam-loading-screen";

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
        CopyLoadingBackground(absoluteOutputPath);
        PatchIndex(indexPath);
        PatchLoadingScreen(indexPath);
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

    private static void PatchLoadingScreen(string indexPath)
    {
        var html = File.ReadAllText(indexPath);
        var stylesheetLink = "    <link rel=\"stylesheet\" href=\"TemplateData/style.css\">\n";
        var loadingScreenStyle = $@"      <style id=""{LoadingScreenStyleId}"">
        html, body {{ width: 100%; height: 100%; overflow: hidden; background: #02151d; }}
        #unity-container {{ background: #02151d url('TemplateData/{LoadingBackgroundFileName}') center center / cover no-repeat; overflow: hidden; }}
        #unity-canvas {{ background: #02151d; }}
        #unity-loading-bar {{ position: absolute; inset: 0; left: 0; top: 0; width: 100%; height: 100%; transform: none; display: none; align-items: flex-end; justify-content: center; box-sizing: border-box; padding: 0 0 7%; background: #02151d url('TemplateData/{LoadingBackgroundFileName}') center center / cover no-repeat; opacity: 1; z-index: 2; pointer-events: none; }}
        #unity-loading-bar.unity-loading-fade-out {{ opacity: 0; transition: opacity 600ms ease; transition-delay: 2200ms; }}
        #unity-logo {{ display: none; }}
        #unity-progress-bar-empty {{ width: min(420px, 62%); height: 12px; margin: 0; background: rgba(2, 21, 29, 0.58); border: 1px solid rgba(113, 238, 236, 0.75); border-radius: 999px; box-shadow: 0 0 22px rgba(23, 200, 222, 0.32); overflow: hidden; }}
        #unity-progress-bar-full {{ width: 0%; height: 100%; margin: 0; background: linear-gradient(90deg, #89ffcb 0%, #43d8e8 56%, #1970f0 100%); border-radius: inherit; transition: width 160ms ease-out; }}
        #unity-footer {{ position: absolute; right: 12px; bottom: 12px; width: 38px; height: 38px; }}
        #unity-logo-title-footer, #unity-build-title {{ display: none; }}
        #unity-fullscreen-button {{ float: none; border-radius: 6px; background-color: rgba(2, 21, 29, 0.42); background-image: url('TemplateData/fullscreen-button.png'); background-repeat: no-repeat; background-position: center; }}
        .unity-mobile #unity-loading-bar {{ padding-bottom: calc(7% + env(safe-area-inset-bottom)); }}
        .unity-mobile #unity-progress-bar-empty {{ width: min(420px, 72%); }}
      </style>
";

        html = Regex.Replace(
            html,
            $@"\n\s*<style id=""{Regex.Escape(LoadingScreenStyleId)}"">[\s\S]*?</style>\s*",
            "\n"
        );

        if (html.Contains(stylesheetLink))
        {
            html = html.Replace(stylesheetLink, stylesheetLink + loadingScreenStyle);
        }
        else
        {
            html = html.Replace("  </head>", loadingScreenStyle + "  </head>");
        }

        html = html.Replace(
            "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";",
            "      var loadingBar = document.querySelector(\"#unity-loading-bar\");\n      loadingBar.style.display = \"flex\";"
        );

        html = html.Replace(
            "                document.querySelector(\"#unity-loading-bar\").style.display = \"none\";",
            "                loadingBar.classList.add(\"unity-loading-fade-out\");\n                setTimeout(() => {\n                  loadingBar.style.display = \"none\";\n                }, 3200);"
        );

        File.WriteAllText(indexPath, html);
    }

    private static void CopyLoadingBackground(string outputPath)
    {
        var sourcePath = Path.GetFullPath(LoadingBackgroundSourcePath);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("WebGL loading background was not found.", sourcePath);
        }

        var templateDataPath = Path.Combine(outputPath, "TemplateData");
        Directory.CreateDirectory(templateDataPath);
        File.Copy(sourcePath, Path.Combine(templateDataPath, LoadingBackgroundFileName), true);
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
