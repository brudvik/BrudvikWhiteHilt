<#
.SYNOPSIS
    Generates original giant spider clicks, rasps and hisses as mono PCM WAV files.
#>
$ErrorActionPreference = 'Stop'
$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'Sounds'
$rate = 44100
$random = New-Object System.Random 7391
$sounds = @(
    @{ Name = 'spideridle'; Seconds = 0.65; Tone = 170; Clicks = 5; Hiss = 0.2 },
    @{ Name = 'spideralert'; Seconds = 1.1; Tone = 110; Clicks = 9; Hiss = 0.8 },
    @{ Name = 'spiderbite'; Seconds = 0.35; Tone = 230; Clicks = 3; Hiss = 0.5 },
    @{ Name = 'spiderhit'; Seconds = 0.45; Tone = 310; Clicks = 4; Hiss = 0.65 },
    @{ Name = 'spiderdeath'; Seconds = 1.4; Tone = 90; Clicks = 12; Hiss = 0.75 }
)
foreach ($sound in $sounds) {
    $count = [int]($rate * $sound.Seconds)
    $stream = [System.IO.File]::Create((Join-Path $out ($sound.Name + '.wav')))
    $writer = New-Object System.IO.BinaryWriter $stream
    try {
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $writer.Write([int](36 + 2 * $count))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
        $writer.Write([int]16)
        $writer.Write([int16]1)
        $writer.Write([int16]1)
        $writer.Write([int]$rate)
        $writer.Write([int](2 * $rate))
        $writer.Write([int16]2)
        $writer.Write([int16]16)
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $writer.Write([int](2 * $count))
        $previousNoise = 0.0
        $peak = 0.0
        $energy = 0.0
        for ($index = 0; $index -lt $count; $index++) {
            $time = $index / $rate
            $progress = $index / $count
            $envelope = [Math]::Min(1.0, $time / 0.012) * [Math]::Pow(1.0 - $progress, 1.8)
            $noise = 2.0 * $random.NextDouble() - 1.0
            $hiss = ($noise - $previousNoise) * 0.22 * $sound.Hiss
            $previousNoise = $noise
            $phase = ($progress * $sound.Clicks) % 1.0
            $click = [Math]::Exp(-$phase * 45.0) * [Math]::Sin(2.0 * [Math]::PI * 1800.0 * $time) * 0.5
            $rasp = [Math]::Sin(2.0 * [Math]::PI * $sound.Tone * ($time - 0.22 * $time * $progress))
            $rasp *= 0.13 * (0.5 + 0.5 * [Math]::Sin(2.0 * [Math]::PI * 43.0 * $time))
            $sample = ($hiss + $click + $rasp) * $envelope
            $peak = [Math]::Max($peak, [Math]::Abs($sample))
            $energy += $sample * $sample
            $writer.Write([int16]([Math]::Round($sample * 32767.0)))
        }
        if ($peak -ge 1.0 -or $energy -le 0.0) { throw "Invalid audio: $($sound.Name)" }
        Write-Host "$($sound.Name): $count samples, peak $peak, RMS $([Math]::Sqrt($energy / $count))"
    }
    finally { $writer.Dispose() }
}