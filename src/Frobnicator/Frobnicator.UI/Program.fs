namespace Frobnicator.UI

open System
open System.Reactive.Subjects
open System.Reactive.Linq
open Elmish
open Elmish.WPF
open NAudio.Wave

module Types =
    type Model = {buttonText : string; frequency : float; out: IWavePlayer }

    type Msg = 
    | Click
    | Frequency of float

module State =
    open Types
    open Frobnicator.Audio
    open Frobnicator.Audio.Wave
    open NAudio.CoreAudioApi

    let evt = new Subject<float>()
    
    let init () =
        let out = new WasapiOut(AudioClientShareMode.Exclusive, true, 5)
        let fmt = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)
        let freq = sampleAndHold (evt.StartWith(440.0))
        let sigGen = sine fmt freq
        out.Init(new Output(fmt, sigGen))
        { buttonText = "Start" ; frequency = 440.0; out = out }
        
    let update msg model = 
        match msg, model.out.PlaybackState with
        | Click, PlaybackState.Playing ->
            model.out.Stop()
            { model with buttonText = "Start" }
        | Click, _ -> 
            model.out.Play()
            { model with buttonText = "Stop" }
        | Frequency f, _ ->
            evt.OnNext f
            { model with frequency = f }
             
module App = 
    open Types
    open State
    
    let view _ _ =
        [ "Text" |> Binding.oneWay (fun m -> m.buttonText)
          "GetFreq" |> Binding.oneWay (fun m -> sprintf "%.2f" m.frequency)
          "Start" |> Binding.cmd (fun _ _ -> Click)
          "Frequency" |> Binding.twoWay (fun m -> m.frequency) (fun v m -> Frequency v )]


    [<EntryPoint; STAThread>]
    let main argv = 
        Program.mkSimple init update view
        //|> Program.withConsoleTrace
        |> Program.runWindow (Frobnicator.Views.MainWindow())
