#i "nuget: https://nuget.cloudsmith.io/shayanhabibi/shayanhabibi-partas-build/v3/index.json"
#r "nuget: Partas.Build, 0.8.2"
#r "nuget: Partas.Build.Baked, 0.3.0"
#r "nuget: Partas.Build.EasyBuild.ShipIt, 0.1.0"
#r "nuget: Fake.IO.FileSystem"

open System
open System.IO
open System.Runtime.InteropServices
open Partas.Build
open Fake.IO.Globbing.Operators

module Spec =
    let root = __SOURCE_DIRECTORY__
    do Directory.SetCurrentDirectory root
    let solution = "Ranvier.Keyboard.slnx"
    let library = "src/Ranvier.Keyboard/Ranvier.Keyboard.fsproj"
    let dom = "src/Ranvier.Keyboard.Dom/Ranvier.Keyboard.Dom.fsproj"
    let fableDom = "src/Fable.Ranvier.Keyboard.Dom/Fable.Ranvier.Keyboard.Dom.fsproj"
    /// The packed projects: the core library first, then the DOM adapters that reference it.
    let packages = [ library; dom; fableDom ]
    let unitTests = "tests/Ranvier.Keyboard.Tests/Ranvier.Keyboard.Tests.fsproj"
    let aotSmoke = "tests/Ranvier.Keyboard.AotSmoke/Ranvier.Keyboard.AotSmoke.fsproj"
    let fableSmoke = "fable/Ranvier.Keyboard.Smoke.Fable"
    /// External Ranvier packages checked for matching traced and untraced ids.
    let corePackages = [ "Ranvier.Dom"; "Fable.Ranvier.Dom" ]

    let localTools =
        use manifest =
            System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".config/dotnet-tools.json")))

        manifest.RootElement.GetProperty("tools").EnumerateObject()
        |> Seq.map (fun tool -> tool.Name, tool.Value.GetRawText())
        |> Seq.toList

    let formatting =
        !! "**/*.fs"
        -- "**/obj/**"
        -- "**/bin/**"
        -- "**/dist/**"
        -- "**/artifacts/**"
        -- "**/node_modules/**"
        -- "**/fable_modules/**"
        |> Seq.toList

    let rid = RuntimeInformation.RuntimeIdentifier

    let executable name =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            name + ".exe"
        else
            name

module Options =
    let group =
        Input.option "--group"
        |> Input.mapFromAmong [ "all", "all"; "dotnet", "dotnet"; "fable", "fable"; "aot", "aot" ]
        |> Input.def "all"
        |> Input.description "Checks to run: all, or a single group (default: all)"

    let dryFormat =
        Input.option<bool> "--dry-format"
        |> Input.description "Check formatting without rewriting files"

    let dryPublish =
        Input.option<bool> "--dry-publish"
        |> Input.description "Build and pack without uploading packages"

    let ranvierRoot =
        Input.option<string> "--ranvier-root"
        |> Input.def (Path.GetFullPath(Path.Combine(Spec.root, "../Ranvier")))
        |> Input.description "Path to the sibling Ranvier repository"

module Stage =
    let restoreTools =
        stage "restore tools in isolated SDK processes" {
            for name, registration in Spec.localTools do
                stage $"restore {name}" {
                    run (fun _ ->
                        let directory = Path.Combine(Spec.root, ".config/.tool-restore")
                        Directory.CreateDirectory directory |> ignore

                        let json =
                            "{\"version\":1,\"isRoot\":true,\"tools\":{"
                            + System.Text.Json.JsonSerializer.Serialize(name)
                            + ":"
                            + registration
                            + "}}"

                        File.WriteAllText(Path.Combine(directory, name + ".json"), json))

                    run (
                        Cmd.ofList
                            "dotnet"
                            [ "tool"
                              "restore"
                              "--tool-manifest"
                              Path.Combine(Spec.root, ".config/.tool-restore", name + ".json")
                              "--verbosity"
                              "q" ]
                    )
                }
        }

    let restore =
        input {
            let! quick = Baked.Common.quick
            and! config = Baked.Dotnet.configOrRelease

            return
                stage "restore" {
                    when' (not quick)
                    restoreTools

                    stage "restore solution" {
                        run (cmd $"dotnet restore {Spec.solution} -p:Configuration={config}")
                    }
                }
        }

    let build =
        input {
            let! config = Baked.Dotnet.configOrRelease

            return
                stage "build" {
                    for project in Spec.packages @ [ Spec.unitTests ] do
                        stage $"build {project}" {
                            run (cmd $"dotnet build {project} -c {config} -m:1 --no-restore")
                        }
                }
        }

    let formatWith (check: bool) =
        stage "format" {
            run (cmd $"dotnet fantomas" |> Cmd.argIf check [ "--check" ] |> Cmd.args Spec.formatting)
        }

    let format =
        input {
            let! check = Options.dryFormat
            return formatWith check
        }

    let dotnet =
        stage "test the suite, untraced and traced" {
            run (cmd $"dotnet test {Spec.unitTests} -c Release -m:1")
            run (cmd $"dotnet test {Spec.unitTests} -c Release -m:1 -p:RanvierTrace=true")
        }

    let fable =
        stage "compile the smoke program with fickl and run it under Node" {
            stage "compile" {
                run (cmd $"dotnet fickl {Spec.fableSmoke} -e .fs.js -o dist/fable-smoke -c Release --noCache")
            }

            stage "run" { run (Cmd.ofList "node" [ "dist/fable-smoke/Main.fs.js" ]) }
        }

    let aot =
        stage "publish and run the NativeAOT smoke test" {
            run (cmd $"dotnet publish {Spec.aotSmoke} -c Release -r {Spec.rid} -o artifacts/aot")
            run (Path.Combine(Spec.root, "artifacts/aot", Spec.executable "Ranvier.Keyboard.AotSmoke"))
        }

    let testsWith (group: InputSpec<string>) =
        input {
            let! group = group
            and! skip = Baked.Common.skipTests

            return
                stage "checks" {
                    when' (not skip)

                    stage "dotnet" {
                        when' (group = "all" || group = "dotnet")
                        dotnet
                    }

                    stage "fable" {
                        when' (group = "all" || group = "fable")
                        fable
                    }

                    stage "aot" {
                        when' (group = "all" || group = "aot")
                        aot
                    }
                }
        }

    let tests = testsWith (InputSpec.ofInput Options.group)
    let allTests = testsWith (InputSpec.ret "all")

    let pack =
        stage "pack untraced and traced packages" {
            run (fun _ ->
                let bin = Path.Combine(Spec.root, "bin")

                if Directory.Exists bin then
                    for file in Directory.GetFiles(bin, "*.nupkg") do
                        File.Delete file)

            for project in Spec.packages do
                for traced in [ "false"; "true" ] do
                    stage $"pack {project} (trace={traced})" {
                        run (cmd $"dotnet pack {project} -c Release -p:RanvierTrace={traced} -o bin")
                    }

            stage "check package ids, versions, frameworks, trace builds and dependencies" {
                run (
                    Cmd.ofList
                        "node"
                        ([ ".github/scripts/packages.cjs"; "bin"; "src"; "--external" ] @ Spec.corePackages)
                )
            }

            stage "check Fable source layouts in packed packages" {
                run (Cmd.ofList "node" [ ".github/scripts/fable-sources.cjs"; "bin" ])
            }
        }

    let publish =
        input {
            let! key = Baked.NuGet.apiKey.option
            and! dry = Options.dryPublish

            return
                stage "publish" {
                    when' (not dry)

                    stage "push (requires NUGET_API_KEY or --nuget-key)" {
                        failIfIgnored
                        when' key.IsSome

                        run (
                            cmd $"dotnet nuget push bin/*.nupkg --source https://api.nuget.org/v3/index.json --skip-duplicate"
                            |> Cmd.secretOption "--api-key" (Option.defaultValue "" key)
                        )
                    }
                }
        }

    let local =
        input {
            let! root = Options.ranvierRoot
            let root = Path.GetFullPath(root, Spec.root)

            return
                stage "configure sibling Ranvier checkout" {
                    stage "clone if missing" {
                        when' (not (Directory.Exists root))
                        run (cmd $"git clone https://github.com/ranvier-org/Ranvier.git {root}")
                    }

                    stage "write local configuration" {
                        run (fun _ ->
                            if not (File.Exists(Path.Combine(root, "src/Ranvier.Dom/Ranvier.Dom.fsproj"))) then
                                invalidArg "ranvier-root" "The path is not a Ranvier checkout."

                            let escaped = System.Security.SecurityElement.Escape root

                            File.WriteAllText(
                                Path.Combine(Spec.root, "Local.Build.props"),
                                $"<Project><PropertyGroup><UseLocalRanvier>true</UseLocalRanvier><RanvierRoot>{escaped}</RanvierRoot></PropertyGroup></Project>"
                            ))
                    }
                }
        }

    let packages =
        stage "configure published package dependencies" {
            run (fun _ ->
                File.WriteAllText(
                    Path.Combine(Spec.root, "Local.Build.props"),
                    "<Project><PropertyGroup><UseLocalRanvier>false</UseLocalRanvier></PropertyGroup></Project>"
                ))
        }

exit
<| rootCommand fsi.CommandLineArgs[1..] {
    description "Ranvier.Keyboard development, validation and releases"
    workingDir Spec.root

    command "tools" {
        description "Restore pinned tools individually to avoid SDK tool-metadata collisions on fresh runners"
        Command.pipeline { Stage.restoreTools }
    }

    command "dev" {
        description "Set up dependencies and switch between published and sibling Ranvier references"

        command "setup" {
            description "Restore .NET tools and projects"
            Command.pipeline { Stage.restore }
        }

        command "local" {
            description "Clone Ranvier if missing and configure local project references"
            Command.pipeline { Stage.local; Stage.restore }
        }

        command "packages" {
            description "Switch back to pinned published Ranvier dependencies"
            Command.pipeline { Stage.packages; Stage.restore }
        }
    }

    command "build" {
        description "Build the libraries and the test project"
        Command.pipeline { Stage.restore; Stage.build }
    }

    command "test" {
        alias "tests"
        description "Run the checks; --group selects a CI shard (default: all)"
        Command.pipeline { Stage.restore; Stage.build; Stage.tests }
    }

    command "ci" {
        description "Run the CI gates locally and pack the validated libraries"
        Command.pipeline { Stage.restore; Stage.formatWith true; Stage.build; Stage.allTests; Stage.pack }
    }

    command "format" {
        description "Format source files (--dry-format to check)"
        Command.pipeline { Stage.restore; Stage.format }
    }

    command "pack" {
        description "Pack all packages, untraced and traced, and check them"
        Command.pipeline { Stage.restore; Stage.pack }
    }

    command "publish" {
        description "Validate and publish (--dry-publish to skip upload)"
        Command.pipeline { Stage.restore; Stage.build; Stage.allTests; Stage.pack; Stage.publish }
    }

    command "bump" {
        description "Calculate release versions with ShipIt in local mode"
        Command.pipeline { Partas.Build.EasyBuild.ShipIt.Stages.bump }
    }

    addCommand Partas.Build.EasyBuild.ShipIt.Commands.shipit
}
