module Ranvier.Keyboard.AotSmoke.Program

open System
open Ranvier.Keyboard.Smoke

[<EntryPoint>]
let main _ =
    let mutable failures = 0

    for name, ok in Checks.run () do
        if ok then
            Console.WriteLine ("ok   " + name)
        else
            failures <- failures + 1
            Console.WriteLine ("FAIL " + name)

    if failures = 0 then
        Console.WriteLine "all checks passed"
        0
    else
        Console.WriteLine (string failures + " checks failed")
        1
