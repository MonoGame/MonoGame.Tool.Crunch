namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildToolTask))]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        BuildForArchitecture(context, "x64", "windows-x64");
        BuildForArchitecture(context, "ARM64", "windows-arm64");
    }

    private void BuildForArchitecture(BuildContext context, string cmakeArch, string rid, string cmakeOptions = "")
    {
        var buildWorkingDir = $"crunch_build_{rid}";
        Directory.CreateDirectory(buildWorkingDir);
        // Path relative to the buildWorkingDir
        var cmakeListsPath = System.IO.Path.Combine("..", "crunch", "CMakeLists.txt");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = $"{cmakeOptions} -DBUILD_CRUNCH=ON -DBUILD_SHARED_LIBCRN=OFF -DBUILD_SHARED_LIBS=OFF -DBUILD_EXAMPLES=OFF {cmakeListsPath}" });
        context.ReplaceTextInFiles("crunch_build/_crunch/crunch.vcxproj", "MultiThreadedDLL", "MultiThreaded");
        context.ReplaceTextInFiles("crunch_build/crnlib/crn-obj.vcxproj",  "MultiThreadedDLL", "MultiThreaded");
        context.ReplaceTextInFiles("crunch_build/crnlib/crn.vcxproj", "MultiThreadedDLL", "MultiThreaded");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "--build . --config release" });
        Directory.CreateDirectory($"{context.ArtifactsDir}/{rid}");
        var files = Directory.GetFiles(System.IO.Path.Combine (buildWorkingDir, "Release"), "crunch.exe", SearchOption.TopDirectoryOnly);
        context.CopyFile(files[0], $"{context.ArtifactsDir}/{rid}/crunch.exe");
    }
}