module Frobnicator.Audio.Output

open Types

open NAudio.Wave
open System
open System.Collections.Generic

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
