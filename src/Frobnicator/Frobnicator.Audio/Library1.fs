namespace Frobnicator.Audio

open NAudio.Wave
open NAudio.CoreAudioApi

type WaveOut() = 
    member this.out = new WasapiOut()
    
    member this.start () =
        this.out.Init(new SilenceProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)))
        this.out.Play()
