namespace BuildScripts;

[TaskName("Build Linux")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildToolTask))]
public sealed class BuildLinuxTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnLinux();

    public override void Run(BuildContext context)
    {
        var buildWorkingDir = "crunch_build/";
        Directory.CreateDirectory(buildWorkingDir);
        // Path relative to the buildWorkingDir
        var cmakeListsPath = System.IO.Path.Combine("..", "crunch", "CMakeLists.txt");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = $"-DCMAKE_BUILD_TYPE=Release -DBUILD_CRUNCH=ON -DBUILD_SHARED_LIBCRN=OFF -DBUILD_SHARED_LIBS=OFF -DBUILD_EXAMPLES=OFF {cmakeListsPath}" });
        context.StartProcess("make", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "" });
        var files = Directory.GetFiles(buildWorkingDir, "crunch", SearchOption.TopDirectoryOnly);
        context.CopyFile(files[0], $"{context.ArtifactsDir}/crunch");
    }
}