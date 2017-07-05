namespace Frobnicator.Audio

open System
open NAudio.Wave

type Sine(waveFormat : WaveFormat)  = 
    let mutable nSample = 0

    interface ISampleProvider with
        member this.WaveFormat with get() = 
            waveFormat
        
        member this.Read (buffer, offset, count) =
            let mutable outIndex = offset
            let multiple = 2.0 * Math.PI * 440.0 / (float)waveFormat.SampleRate

            for sampleCount in [0 .. (count/waveFormat.Channels)-1] do
                let sampleValue = Math.Sin((float)nSample * multiple)
                nSample <- nSample + 1
                
                for i in [0 .. waveFormat.Channels-1] do
                   buffer.[outIndex] = (float32)sampleValue
                   outIndex <- outIndex + 1
                   
            count
        
    
