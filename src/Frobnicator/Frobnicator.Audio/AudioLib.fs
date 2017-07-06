namespace Frobnicator.Audio

open System
open NAudio.Wave

type Stream = float seq

type Output(waveFormat : WaveFormat, stream : Stream)  = 
    let bytesPerSample = waveFormat.BitsPerSample / 8

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
            stream 
                |> Seq.take nSamples 
                |> Seq.iter (fun x ->
                    [1  .. waveFormat.Channels] |> Seq.iter (fun _ -> putSample x buffer))  
            
            count
        
module Wave = 
    let TwoPi = 2.0 * Math.PI

    let generate func (waveFormat : WaveFormat) freq : Stream = 
        let delta = TwoPi * freq / (float)waveFormat.SampleRate
        let mutable theta = 0.0
        let rec gen =
            seq {
                yield func theta
                theta <- (theta + delta) % TwoPi 
                yield! gen 
            }
        gen
        
    let sine (waveFormat : WaveFormat) (freq : float) = generate Math.Sin waveFormat freq 


