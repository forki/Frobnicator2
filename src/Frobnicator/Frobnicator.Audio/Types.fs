module Frobnicator.Audio.Types

type Stream = float seq
type Sample = float array
type Envelope = { data : Sample; holdPoint : int option}
type Trigger =
    | Fire
    | Release

