#r "paket:
nuget Fake.Core.Target 
nuget Fake.IO.Filesystem
nuget Fake.DotNet.Cli //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO

let (</>) = Path.combine

let tool = "cite"
let project = "cite.fsproj"
let packagesLocation = __SOURCE_DIRECTORY__ </> "packages"

Target.create "Clean" <| fun _ ->
    DotNet.exec id "clean" project |> ignore
    DotNet.exec id "clean" (sprintf "%s -c Release" project) |> ignore
    Directory.delete packagesLocation

Target.create "Build" <| fun _ -> 
    DotNet.build id project

Target.create "Pack" <| fun _ ->
    project |> DotNet.pack (fun o -> 
        { o with 
            NoBuild = true
            OutputPath = Some packagesLocation })
    
Target.create "Install" <| fun _ ->
    DotNet.exec id "tool install" 
        (sprintf "--global --add-source %s %s" packagesLocation tool)
        |> ignore

Target.create "Uninstall" <| fun _ ->
    DotNet.exec id "tool uninstall"
        (sprintf "--global %s" tool)
        |> ignore

"Build" ==> "Pack" ==> "Install"

Target.runOrDefault "Pack"