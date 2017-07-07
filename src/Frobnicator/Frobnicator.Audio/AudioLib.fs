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

    let generate func (waveFormat : WaveFormat) (freq : Stream) : Stream = 
        let mutable theta = 0.0
        let rec gen =
            seq {
                let f = freq |> Seq.take 1 |> Seq.head
                let delta = TwoPi * f / (float)waveFormat.SampleRate
                yield func theta
                theta <- (theta + delta) % TwoPi 
                yield! gen 
            }
        gen
        
    let constStream value =
        let rec gen () =
            seq {
                yield value
                yield! gen ()
            }
        gen ()

    let sampleAndHold (e : IObservable<float>) =
        let mutable value = 0.0

        e |> Observable.add (fun v -> value <- v)
        
        let rec gen () =
            seq {
                yield value
                yield! gen ()
            }
        gen ()

    let sine (waveFormat : WaveFormat) (freq : Stream) = generate Math.Sin waveFormat freq 

    let constSine (waveFormat : WaveFormat) (freq : float) = sine waveFormat (constStream freq) 

