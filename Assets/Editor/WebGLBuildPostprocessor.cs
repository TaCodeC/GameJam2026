using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

public sealed class WebGLBuildSettingsPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        // Use normal WebGL compression again. The previous GitHub Pages workaround
        // forced uncompressed .data/.wasm/.js files and deleted .gz/.br output.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = false;
    }
}

public static class WebGLBuildPostprocessor
{
    private const string LoadingBackgroundSourcePath = "BuildAssets/WebGL/loading-background.jpg";
    private const string LoadingBackgroundFileName = "loading-background.jpg";
    private const string LoadingScreenStyleId = "gamejam-loading-screen";
    private const string IosDprMarker = "// GameJam iOS memory safeguard";

    [PostProcessBuild(1000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.WebGL)
            return;

        PostProcessBuild(pathToBuiltProject);
    }

    private static void PostProcessBuild(string outputPath)
    {
        string absoluteOutputPath = Path.GetFullPath(outputPath);
        string indexPath = Path.Combine(absoluteOutputPath, "index.html");

        if (!File.Exists(indexPath))
            throw new FileNotFoundException("Unity WebGL index.html was not found.", indexPath);

        CopyLoadingBackground(absoluteOutputPath);
        PatchIndex(indexPath);
        PatchLoadingScreen(indexPath);
        PatchIosDevicePixelRatio(indexPath);
    }

    private static void PatchIndex(string indexPath)
    {
        string html = File.ReadAllText(indexPath);
        string buildVersion = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        // Keep cache busting, but preserve whatever compression suffix Unity generated
        // (.gz, .br, or none) instead of rewriting compressed files to uncompressed ones.
        html = Regex.Replace(
            html,
            @"\s*var buildVersion = ""[^""]+"";\s*",
            "\n      "
        );

        html = Regex.Replace(
            html,
            @"var loaderUrl = buildUrl \+ ""/build\.loader\.js(?:\?v="" \+ buildVersion)?"";",
            $"var buildVersion = \"{buildVersion}\";\n      var loaderUrl = buildUrl + \"/build.loader.js?v=\" + buildVersion;"
        );

        html = Regex.Replace(
            html,
            @"buildUrl \+ ""/(build\.(?:data|framework\.js|wasm)(?:\.gz|\.br)?)(?:\?v="" \+ buildVersion)?""",
            "buildUrl + \"/$1?v=\" + buildVersion"
        );

        File.WriteAllText(indexPath, html);
    }

    private static void PatchLoadingScreen(string indexPath)
    {
        string html = File.ReadAllText(indexPath);
        const string stylesheetLink = "    <link rel=\"stylesheet\" href=\"TemplateData/style.css\">\n";
        string loadingScreenStyle = $@"      <style id=""{LoadingScreenStyleId}"">
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
            html = html.Replace(stylesheetLink, stylesheetLink + loadingScreenStyle);
        else
            html = html.Replace("  </head>", loadingScreenStyle + "  </head>");

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

    private static void PatchIosDevicePixelRatio(string indexPath)
    {
        string html = File.ReadAllText(indexPath);

        if (html.Contains(IosDprMarker))
            return;

        const string unityHint = "        // config.devicePixelRatio = 1;";
        string iosDprPatch =
            $"        {IosDprMarker}\n" +
            "        if (/iPhone|iPad|iPod/i.test(navigator.userAgent)) {\n" +
            "          config.devicePixelRatio = 1;\n" +
            "        }";

        if (html.Contains(unityHint))
        {
            html = html.Replace(unityHint, iosDprPatch);
        }
        else
        {
            const string mobileClassLine = "        canvas.className = \"unity-mobile\";";
            if (html.Contains(mobileClassLine))
                html = html.Replace(mobileClassLine, mobileClassLine + "\n\n" + iosDprPatch);
            else
                Debug.LogWarning("Could not inject the iOS devicePixelRatio safeguard into WebGL index.html.");
        }

        File.WriteAllText(indexPath, html);
    }

    private static void CopyLoadingBackground(string outputPath)
    {
        string sourcePath = Path.GetFullPath(LoadingBackgroundSourcePath);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("WebGL loading background was not found.", sourcePath);

        string templateDataPath = Path.Combine(outputPath, "TemplateData");
        Directory.CreateDirectory(templateDataPath);
        File.Copy(sourcePath, Path.Combine(templateDataPath, LoadingBackgroundFileName), true);
    }
}
