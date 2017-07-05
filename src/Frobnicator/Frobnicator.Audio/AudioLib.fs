namespace Frobnicator.Audio

open System
open NAudio.Wave
open System.Globalization

type Sine(waveFormat : WaveFormat)  = 
    let mutable nSample = 0
    let bytesPerSample = waveFormat.BitsPerSample / 8

    interface IWaveProvider with
        member __.WaveFormat with get() = waveFormat
        
        member __.Read (buffer, offset, count) =
            let mutable outIndex = offset
            let numSamples = count / bytesPerSample
            let multiple = 2.0 * Math.PI * 440.0 / (float)waveFormat.SampleRate

            for sampleCount in [0 .. numSamples/(waveFormat.Channels)-1] do
                let sampleValue = Math.Sin((float)nSample * multiple)
                nSample <- nSample + 1
                
                for i in [0 .. waveFormat.Channels-1] do
                    let bytes = BitConverter.GetBytes((float32)sampleValue)
                    for j in [0 .. bytes.Length-1] do
                        buffer.[outIndex] <- bytes.[j]
                        outIndex <- outIndex + 1
                   
            count
        
    
