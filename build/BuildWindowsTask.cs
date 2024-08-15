namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildToolTask))]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        var buildWorkingDir = "crunch_build/";
        Directory.CreateDirectory(buildWorkingDir);
        // Path relative to the buildWorkingDir
        var cmakeListsPath = System.IO.Path.Combine("..", "crunch", "CMakeLists.txt");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = $"-DBUILD_CRUNCH=ON -DBUILD_SHARED_LIBCRN=OFF -DBUILD_SHARED_LIBS=OFF -DBUILD_EXAMPLES=OFF {cmakeListsPath}" });
        context.ReplaceTextInFiles("crunch_build/crunch.vcxproj", "MultiThreadedDLL", "MultiThreaded");
        //context.ReplaceTextInFiles("crunch_build/crnlib/crn-obj.vcxproj",  "MultiThreadedDLL", "MultiThreaded");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "--build . --config release" });
        var files = Directory.GetFiles(System.IO.Path.Combine (buildWorkingDir, "Release"), "crunch.exe", SearchOption.TopDirectoryOnly);
        context.CopyFile(files[0], $"{context.ArtifactsDir}/crunch.exe");
    }
}