namespace Frobnicator.UI

open System
open Elmish
open Elmish.WPF
open Frobnicator.Audio

module Types =
    type Model = {buttonText : string; running : bool; out : WaveOut}

    type Msg = 
    | Click

module State =
    open Types
    
    let init () = { buttonText = "Start"; running = false; out = new WaveOut() }

    let update msg model = 
        match msg, model.running with
        | Click, false ->
            model.out.start()
            { model with buttonText = "Stop"; running = true }
        | Click, true -> 
            model.out.stop()
            { model with buttonText = "Start"; running = false }

module App = 
    open Types
    open State
    
    let view _ _ =
        [ "Text" |> Binding.oneWay (fun m -> m.buttonText)
          "Start" |> Binding.cmd (fun _ m -> Click) ]


    [<EntryPoint; STAThread>]
    let main argv = 
        Program.mkSimple init update view
        |> Program.withConsoleTrace
        |> Program.runWindow (Frobnicator.Views.MainWindow())
