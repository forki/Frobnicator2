module Frobnicator.Audio.Wave

open Types
open Output

open NAudio.Wave
open System

let TwoPi = 2.0 * Math.PI

let generate func (waveFormat : WaveFormat) (freq : Stream) : Stream = 
    let generator theta =
        let f = freq |> Seq.head
        let delta = TwoPi * f / (float)waveFormat.SampleRate
        (theta + delta) % TwoPi        

    Seq.unfold (fun theta -> Some(func theta, generator theta)) 0.0

let constStream value =
    Seq.unfold (fun _ -> Some(value, 0)) 0

let silence = constStream 0.0

let sampleAndHold (e : IObservable<float>) =
    let mutable value = 0.0
    e |> Observable.add (fun v -> value <- v)

    Seq.unfold (fun _ -> Some(value, 0)) 0

let sine (waveFormat : WaveFormat) (freq : Stream) = generate Math.Sin waveFormat freq 

let combine (f : (float * float) -> float) (s1 : Stream) (s2: Stream) =
     Seq.zip s1 s2 |> Seq.map f

let gain = combine (fun (c, s) -> c * s)

let pluck (waveFormat : WaveFormat) freq =
    let rnd = new Random()
    let bufferLength = int((float)waveFormat.SampleRate / freq)
    let buffer = [| for i in [1..bufferLength] -> rnd.NextDouble() - 0.5 |]

    let gen s i =
        buffer.[i] <- (s + buffer.[i]) / 2.0
        buffer.[i]

    let step i = (i + 1) % bufferLength

    Seq.unfold (fun (sample, index) -> 
        let newSample = gen sample index
        let newState = (newSample, (step index))
        Some(newSample, newState) ) (0.0, 0)

let envelope (e : IObservable<Trigger>) (env : Envelope) (signal : Stream) =
    let mutable triggered = false
    let mutable released = false
    e |> Observable.add (fun t -> 
        match t with
        | Fire -> triggered <- true
        | Release -> released <- true)

    let inc s =
        match s with 
        | None -> None
        | Some v -> Some (v + 1)

    let generator (s : int option) = 
        if triggered then
            triggered <- false
            Some 0
        else if released then
            released <- false
            inc s
        else
            match s with
            | Some v when v < (env.data.Length - 1) -> 
                match env.holdPoint with
                | Some p when p = v -> s
                | _ -> inc s
            | _  -> None

    let value s = 
        match s with
        | None -> 0.0
        | Some v -> env.data.[v]

    Seq.unfold (fun s -> Some(value s, generator s)) None |> gain signal