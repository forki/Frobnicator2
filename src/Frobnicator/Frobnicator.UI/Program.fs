namespace Frobnicator.UI

open System
open System.Reactive.Linq
open System.Reactive.Subjects
open Elmish
open Elmish.WPF
open NAudio.CoreAudioApi
open NAudio.Midi
open NAudio.Wave

module Types =
    type Model = {buttonText : string; frequency : float; output: IWavePlayer; input: MidiIn }

    type Msg = 
    | Click
    | Frequency of float
    | Midi of MidiEvent

module State =
    open Types
    open Frobnicator.Audio
    open Frobnicator.Audio.Wave
    open NAudio.CoreAudioApi

    let frequencyEvent = new Subject<float>()
        
    let init () =
        let fmt = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)
        let freq = sampleAndHold (frequencyEvent.StartWith(440.0))
        let sigGen = sine fmt freq

        let output = new WasapiOut(AudioClientShareMode.Shared, 1)
        output.Init(new Output(fmt, sigGen))

        let input = new MidiIn(0)

        { buttonText = "Start" ; frequency = 440.0; output = output; input = input }
        
    let midiNoteToFrequency (noteNumber : int) : float =
        Math.Pow(2.0, (float (noteNumber - 69)) / 12.0) * 440.0

    let update msg model = 
        match msg with
        | Click ->
            match model.output.PlaybackState with
            | PlaybackState.Playing -> 
                model.output.Stop()
                { model with buttonText = "Start" }
            | _ ->
                model.output.Play()
                { model with buttonText = "Stop" }
        | Frequency f ->
            frequencyEvent.OnNext f
            { model with frequency = f }
        | Midi msg ->
            match msg with
            | :? NoteEvent -> 
                let noteEvent = msg :?> NoteEvent
                let f = 
                    if NoteEvent.IsNoteOn(noteEvent) then
                        midiNoteToFrequency noteEvent.NoteNumber
                    else
                        0.0
                frequencyEvent.OnNext f
                { model with frequency = f }
            | _ ->
                model
             
module App = 
    open Types
    open State
    
    let view _ _ =
        [ "Text" |> Binding.oneWay (fun m -> m.buttonText)
          "GetFreq" |> Binding.oneWay (fun m -> sprintf "%.2f" m.frequency)
          "Start" |> Binding.cmd (fun _ _ -> Click)
          "Frequency" |> Binding.twoWay (fun m -> m.frequency) (fun v m -> Frequency v )]

    let keys initial =
        let sub dispatch = 
            initial.input.MessageReceived |> Observable.add (fun evt -> dispatch (Midi evt.MidiEvent))
        Cmd.ofSub sub

    [<EntryPoint; STAThread>]
    let main argv = 
        Program.mkSimple init update view
        |> Program.withSubscription keys
        |> Program.withConsoleTrace
        |> Program.runWindow (Frobnicator.Views.MainWindow())
