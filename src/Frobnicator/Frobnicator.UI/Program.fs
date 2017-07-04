namespace Frobnicator.UI

open System
open Elmish
open Elmish.WPF

module Types =
    type TextModel = {text : string}

    type Msg = 
    | Click

module State =
    open Types
    
    let init () = { text = "Initial Text" }

    let update msg model = 
        match msg with
        | Click -> { model with text = "New Text" }

module App = 
    open Types
    open State
    
    let view _ _ =
        [ "Text" |> Binding.oneWay (fun m -> m.text)
          "SetText" |> Binding.cmd (fun _ m -> Click) ]


    [<EntryPoint; STAThread>]
    let main argv = 
        Program.mkSimple init update view
        |> Program.withConsoleTrace
        |> Program.runWindow (Frobnicator.Views.MainWindow())
