namespace Frobnicator.Audio

open System
open System.Collections.Generic
open NAudio.Midi
open NAudio.Wave

type Stream = float seq
type Sample = float array
type Envelope = { data : Sample; holdPoint : int option}
type Trigger =
    | Fire
    | Release

type Output(waveFormat : WaveFormat, stream : Stream)  = 
    let enumerator = stream.GetEnumerator() // so we don't restart the srtream on every call to Read()
    let bytesPerSample = waveFormat.BitsPerSample / 8

    let head (enum : IEnumerator<float>) =
        enum.MoveNext() |> ignore
        enum.Current

    interface IWaveProvider with
        member __.WaveFormat with get() = waveFormat
        
        member __.Read (buffer, offset, count) =
            let mutable insertIndex = 0
            let putSample sample buffer =
                let bytes = 
                    match bytesPerSample, waveFormat.Encoding with
                    | 4, WaveFormatEncoding.IeeeFloat -> BitConverter.GetBytes((float32)sample)
                    | 8, WaveFormatEncoding.IeeeFloat -> BitConverter.GetBytes((float)sample)
                    | _, _ -> Array.init bytesPerSample (fun _ -> (byte)0)
                
                Array.blit bytes 0 buffer insertIndex bytes.Length
                insertIndex <- insertIndex + bytes.Length
                
            let nSamples = count / (bytesPerSample * waveFormat.Channels)
            for nSample in [1 .. nSamples] do
                let sample = enumerator |> head
                for nChannel in [1  .. waveFormat.Channels] do
                    putSample sample buffer
            
            count

module Wave = 
    let TwoPi = 2.0 * Math.PI

    let generate func (waveFormat : WaveFormat) (freq : Stream) : Stream = 
        let generator theta =
            let f = freq |> Seq.head
            let delta = TwoPi * f / (float)waveFormat.SampleRate
            (theta + delta) % TwoPi        

        Seq.unfold (fun theta -> Some(func theta, generator theta)) 0.0

    let constStream value =
        Seq.unfold (fun _ -> Some(value, 0)) 0

    let sampleAndHold (e : IObservable<float>) =
        let mutable value = 0.0
        e |> Observable.add (fun v -> value <- v)

        Seq.unfold (fun _ -> Some(value, 0)) 0

    let sine (waveFormat : WaveFormat) (freq : Stream) = generate Math.Sin waveFormat freq 

    let gain (ctrl : Stream) (signal : Stream) =
        signal |> Seq.zip ctrl |> Seq.map (fun (c, s) -> c * s)

    
    let envelope (waveFormat : WaveFormat) (e : IObservable<Trigger>) (env : Envelope) (signal : Stream) =
        let mutable triggered = false
        e |> Observable.add (fun t -> 
            match t with
            | Fire -> triggered <- true
            | Release -> ())

        let generator s = 
            if triggered then
                triggered <- false
                Some 0
            else
                match s with
                | None -> None
                | Some v when v >= (env.data.Length - 1) -> None
                | Some v -> Some (v + 1)

        let value s = 
            match s with
            | None -> 0.0
            | Some v -> env.data.[v]

        Seq.unfold (fun s -> Some(value s, generator s)) None |> gain signal