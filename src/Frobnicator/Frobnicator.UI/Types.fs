module Frobnicator.UI.Types
    
open NAudio.Midi
open NAudio.Wave

type Model = {buttonText : string; frequency : float; volume : float; output: IWavePlayer; input: MidiIn option }

type Msg = 
| StartStop
| Frequency of float
| Volume of float
| Midi of MidiEvent
| NoteOn
| NoteOff
