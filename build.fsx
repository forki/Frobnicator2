// include Fake lib
#r @"packages/FAKE/tools/FakeLib.dll"
open Fake

// Default target
Target "Build" (fun _ ->
    MSBuildWithDefaults "Build" ["./src/Frobnicator/Frobnicator.sln"] |> Log "AppBuild-Output: "
)

// start build
RunTargetOrDefault "Build"