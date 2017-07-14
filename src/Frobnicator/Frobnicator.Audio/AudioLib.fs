namespace Frobnicator.Audio

open System
open System.Collections.Generic
open NAudio.Midi
open NAudio.Wave

type Stream = float seq

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
        Seq.zip ctrl signal |> Seq.map (fun (c, s) -> c * s)


