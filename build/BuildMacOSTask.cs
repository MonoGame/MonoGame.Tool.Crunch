namespace BuildScripts;

[TaskName("Build macOS")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildToolTask))]
public sealed class BuildMacOSTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnMacOs();

    public override void Run(BuildContext context)
    {
        var buildWorkingDir = "crunch_build/";
        Directory.CreateDirectory(buildWorkingDir);
        // Path relative to the buildWorkingDir
        var cmakeListsPath = System.IO.Path.Combine("..", "crunch", "CMakeLists.txt");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = $"-DCMAKE_OSX_ARCHITECTURES=\"x86_64;arm64\" {cmakeListsPath}" });
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "" });
        var files = Directory.GetFiles(buildWorkingDir, "crunch", SearchOption.TopDirectoryOnly);
        context.CopyFile(files[0], $"{context.ArtifactsDir}/crunch");
    }
}