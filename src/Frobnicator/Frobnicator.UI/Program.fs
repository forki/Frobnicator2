module Frobnicator.UI.App

open Types
open State

open Elmish
open Elmish.WPF
open FsXaml
open System

let view _ _ =
    [ "Text" |> Binding.oneWay (fun m -> m.buttonText)
      "GetFreq" |> Binding.oneWay (fun m -> sprintf "%.2f" m.frequency)
      "GetVol" |> Binding.oneWay (fun m -> sprintf "%.2f" m.volume)
      "Start" |> Binding.cmd (fun _ _ -> StartStop)
      "Trigger" |> Binding.cmd (fun _ _ -> NoteOn)
      "Release" |> Binding.cmd (fun _ _ -> NoteOff)
      "Frequency" |> Binding.twoWay (fun m -> m.frequency) (fun v m -> Frequency v )
      "Volume" |> Binding.twoWay (fun m -> m.volume) (fun v m -> Volume v )]

let keys initial =
    let sub dispatch = 
        match initial.input with
        | Some i -> i.MessageReceived |> Observable.add (fun evt -> dispatch (Midi evt.MidiEvent))
        | None -> ()
    Cmd.ofSub sub

type MainWindow = XAML<"Views/MainWindow.xaml">

[<EntryPoint; STAThread>]
let main argv = 
    Program.mkSimple init update view
    |> Program.withSubscription keys
    |> Program.withConsoleTrace
    |> Program.runWindow (MainWindow())
