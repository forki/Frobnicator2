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
    type Model = {buttonText : string; frequency : float; volume : float; output: IWavePlayer; input: MidiIn option }

    type Msg = 
    | StartStop
    | Frequency of float
    | Volume of float
    | Midi of MidiEvent
    | NoteOn
    | NoteOff

module State =
    open Types
    open Frobnicator.Audio
    open Frobnicator.Audio.Wave
    open NAudio.CoreAudioApi

    let frequencyEvent = new Subject<float>()
    let volumeEvent = new Subject<float>()
    let triggerEvent = new Subject<Trigger>()
        
    let init () =
        let fmt = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)
        let freq = sampleAndHold (frequencyEvent.StartWith(440.0))
        let vol = sampleAndHold (volumeEvent.StartWith(0.0))
        let env = {data = [| for i in [1..fmt.SampleRate] -> 1.0 - ((float)i / (float)fmt.SampleRate)|]; holdPoint = Some (fmt.SampleRate / 2) }

        let signalChain = freq |> sine fmt |> gain vol |> envelope fmt triggerEvent env
             
        let output = new WasapiOut(AudioClientShareMode.Shared, 1)
        output.Init(Output(fmt, signalChain))
                
        let input = 
            try
                Some (new MidiIn(0))
            with 
            | :? NAudio.MmException -> None

        { buttonText = "Start" ; frequency = 440.0; volume = 0.0; output = output; input = input }
        
    let midiNoteToFrequency (noteNumber : int) : float =
        Math.Pow(2.0, (float (noteNumber - 69)) / 12.0) * 440.0

    let update msg model = 
        match msg with
        | StartStop ->
            match model.output.PlaybackState with
            | PlaybackState.Playing ->
                match model.input with
                | Some i -> i.Stop()
                | None -> ()
                model.output.Stop()
                { model with buttonText = "Start" }
            | _ ->
                model.output.Play()
                match model.input with
                | Some i -> i.Start()
                | None -> ()
                { model with buttonText = "Stop" }
        | Frequency f ->
            frequencyEvent.OnNext f
            { model with frequency = f }
        | Volume v ->
            volumeEvent.OnNext (v / 100.0)
            { model with volume = v }
        | Midi msg ->
            match msg with
            | :? NoteEvent -> 
                let noteEvent = msg :?> NoteEvent
                let f = 
                    if NoteEvent.IsNoteOn(noteEvent) then
                        triggerEvent.OnNext Fire
                        midiNoteToFrequency noteEvent.NoteNumber
                    else
                        triggerEvent.OnNext Release
                        0.0
                frequencyEvent.OnNext f
                { model with frequency = f }
            | _ ->
                model
        | NoteOn -> 
            triggerEvent.OnNext Fire
            model
        | NoteOff -> 
            triggerEvent.OnNext Release
            model
             
module App = 
    open Types
    open State
    open FsXaml

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

    type MainWindow = XAML<"MainWindow.xaml">
    
    [<EntryPoint; STAThread>]
    let main argv = 
        Program.mkSimple init update view
        |> Program.withSubscription keys
        |> Program.withConsoleTrace
        |> Program.runWindow (MainWindow())
