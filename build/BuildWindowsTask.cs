namespace BuildScripts;

[TaskName("Build Windows")]
[IsDependentOn(typeof(PrepTask))]
[IsDependeeOf(typeof(BuildToolTask))]
public sealed class BuildWindowsTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.IsRunningOnWindows();

    public override void Run(BuildContext context)
    {
        // Apply ARM64 compatibility patches to crunch source
        ApplyArm64Patches(context);

        BuildForArchitecture(context, "x64", "windows-x64");
        BuildForArchitecture(context, "ARM64", "windows-arm64");
    }

    private void ApplyArm64Patches(BuildContext context)
    {
        // Fix crn_core.h: Ensure WIN32 is defined when _WIN32 is defined (MSVC defines _WIN32, not WIN32)
        context.ReplaceTextInFiles(
            "crunch/crnlib/crn_core.h",
            "// File: crn_core.h\r\n// See Copyright Notice and license at the end of inc/crnlib.h\r\n#pragma once",
            "// File: crn_core.h\r\n// See Copyright Notice and license at the end of inc/crnlib.h\r\n#pragma once\r\n\r\n// Ensure WIN32 is defined when _WIN32 is defined (MSVC defines _WIN32, not WIN32)\r\n#if defined(_WIN32) && !defined(WIN32)\r\n#define WIN32 1\r\n#endif");

        // Fix crn_core.h: Add ARM64 platform detection before x64 check
        context.ReplaceTextInFiles(
            "crunch/crnlib/crn_core.h",
            "#define CRNLIB_PLATFORM_PC 1\r\n\r\n#if defined(_WIN64) || defined(__MINGW64__) || defined(_LP64) || defined(__LP64__)\r\n#define CRNLIB_PLATFORM_PC_X64 1",
            "#define CRNLIB_PLATFORM_PC 1\r\n\r\n#if defined(_M_ARM64)\r\n// Windows ARM64\r\n#define CRNLIB_PLATFORM_PC_ARM64 1\r\n#define CRNLIB_64BIT_POINTERS 1\r\n#define CRNLIB_CPU_HAS_64BIT_REGISTERS 1\r\n#define CRNLIB_LITTLE_ENDIAN_CPU 1\r\n#elif defined(_WIN64) || defined(__MINGW64__) || defined(_LP64) || defined(__LP64__)\r\n#define CRNLIB_PLATFORM_PC_X64 1");

        // Fix crn_core.h: Disable MSVC x86 intrinsics on ARM64
        context.ReplaceTextInFiles(
            "crunch/crnlib/crn_core.h",
            "#if defined(_MSC_VER) || defined(__MINGW32__) || defined(__MINGW64__)\r\n#define CRNLIB_USE_MSVC_INTRINSICS 1\r\n#endif",
            "#if defined(_MSC_VER) || defined(__MINGW32__) || defined(__MINGW64__)\r\n#if !defined(_M_ARM64)\r\n#define CRNLIB_USE_MSVC_INTRINSICS 1\r\n#endif\r\n#endif");

        // Fix crn_atomics.h: Add ARM64 yield processor support
        context.ReplaceTextInFiles(
            "crunch/crnlib/crn_atomics.h",
            "#if defined(__GNUC__) && CRNLIB_PLATFORM_PC\r\nextern __inline__ __attribute__((__always_inline__, __gnu_inline__)) void crnlib_yield_processor() {\r\n  __asm__ __volatile__(\"pause\");\r\n}\r\n#else\r\nCRNLIB_FORCE_INLINE void crnlib_yield_processor() {\r\n#if CRNLIB_USE_MSVC_INTRINSICS\r\n#if CRNLIB_PLATFORM_PC_X64\r\n  _mm_pause();\r\n#else\r\n  YieldProcessor();\r\n#endif\r\n#else\r\n// No implementation\r\n#endif\r\n}",
            "#if defined(__GNUC__) && CRNLIB_PLATFORM_PC\r\nextern __inline__ __attribute__((__always_inline__, __gnu_inline__)) void crnlib_yield_processor() {\r\n#if defined(__aarch64__)\r\n  __asm__ __volatile__(\"yield\");\r\n#else\r\n  __asm__ __volatile__(\"pause\");\r\n#endif\r\n}\r\n#else\r\nCRNLIB_FORCE_INLINE void crnlib_yield_processor() {\r\n#if defined(_M_ARM64)\r\n  __yield();\r\n#elif CRNLIB_USE_MSVC_INTRINSICS\r\n#if CRNLIB_PLATFORM_PC_X64\r\n  _mm_pause();\r\n#else\r\n  YieldProcessor();\r\n#endif\r\n#else\r\n// No implementation\r\n#endif\r\n}");
    }

    private void BuildForArchitecture(BuildContext context, string cmakeArch, string rid, string cmakeOptions = "")
    {
        var buildWorkingDir = $"crunch_build_{rid}";
        Directory.CreateDirectory(buildWorkingDir);
        // Path relative to the buildWorkingDir
        var cmakeListsPath = System.IO.Path.Combine("..", "crunch", "CMakeLists.txt");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = $"{cmakeOptions} -A {cmakeArch} -DBUILD_CRUNCH=ON -DBUILD_SHARED_LIBCRN=OFF -DBUILD_SHARED_LIBS=OFF -DBUILD_EXAMPLES=OFF {cmakeListsPath}" });
        context.ReplaceTextInFiles($"{buildWorkingDir}/_crunch/crunch.vcxproj", "MultiThreadedDLL", "MultiThreaded");
        context.ReplaceTextInFiles($"{buildWorkingDir}/crnlib/crn-obj.vcxproj",  "MultiThreadedDLL", "MultiThreaded");
        context.ReplaceTextInFiles($"{buildWorkingDir}/crnlib/crn.vcxproj", "MultiThreadedDLL", "MultiThreaded");
        context.StartProcess("cmake", new ProcessSettings { WorkingDirectory = buildWorkingDir, Arguments = "--build . --config release" });
        Directory.CreateDirectory($"{context.ArtifactsDir}/{rid}");
        var files = Directory.GetFiles(System.IO.Path.Combine (buildWorkingDir, "Release"), "crunch.exe", SearchOption.TopDirectoryOnly);
        context.CopyFile(files[0], $"{context.ArtifactsDir}/{rid}/crunch.exe");
    }
}