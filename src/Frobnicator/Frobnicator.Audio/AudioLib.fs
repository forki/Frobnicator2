namespace Frobnicator.Audio

open System
open NAudio.Wave
open System.Globalization

type Stream = float seq

type Output(waveFormat : WaveFormat, stream : Stream)  = 
    let bytesPerSample = waveFormat.BitsPerSample / 8

    interface IWaveProvider with
        member __.WaveFormat with get() = waveFormat
        
        member __.Read (buffer, offset, count) =
            let nSamples = count / (bytesPerSample * waveFormat.Channels)
            let samples = stream |> Seq.take nSamples |> Seq.toArray

            let mutable outIndex = offset

            for nSample in [0 .. nSamples-1] do
                let bytes = BitConverter.GetBytes((float32)samples.[nSample])
                for i in [0 .. waveFormat.Channels-1] do
                    for j in [0 .. bytes.Length-1] do
                        buffer.[outIndex] <- bytes.[j]
                        outIndex <- outIndex + 1
                   
            count
        
module Wave = 
    let TwoPi = 2.0 * Math.PI

    let sine (waveFormat : WaveFormat) freq = 
        let delta = TwoPi * freq / (float)waveFormat.SampleRate
        let rec gen theta =
            seq {
                yield Math.Sin theta
                let newTheta = (theta + delta) % TwoPi 
                yield! gen newTheta
            }
        gen 0.0


