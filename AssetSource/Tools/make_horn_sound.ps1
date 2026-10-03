<#
.SYNOPSIS
    Generates an original sustained low horn tone as mono 44.1 kHz PCM WAV.
#>
$ErrorActionPreference = 'Stop'
$path = Join-Path (Split-Path $PSScriptRoot -Parent) 'Sounds\summoninghorn.wav'
$rate = 44100
$seconds = 11
$count = $rate * $seconds
$stream = [System.IO.File]::Create($path)
$writer = New-Object System.IO.BinaryWriter $stream
$random = New-Object System.Random 1947
$phase = 0.0
$breath = 0.0
$peak = 0.0
$energy = 0.0
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
    for ($index = 0; $index -lt $count; $index++) {
        $time = $index / $rate
        $frequency = 73.42 * (1.0 + 0.003 * [Math]::Sin(2.0 * [Math]::PI * 4.1 * $time))
        $phase += 2.0 * [Math]::PI * $frequency / $rate
        $breath += (2.0 * $random.NextDouble() - 1.0 - $breath) * 0.035
        $tone = 0.38 * [Math]::Sin($phase) + 0.21 * [Math]::Sin(2.0 * $phase) + 0.09 * [Math]::Sin(3.0 * $phase)
        $tone += 0.035 * [Math]::Sin(5.0 * $phase) + 0.025 * $breath
        $envelope = [Math]::Min(1.0, $time / 0.08) * [Math]::Min(1.0, ($seconds - $time) / 0.15)
        $sample = $tone * $envelope
        $peak = [Math]::Max($peak, [Math]::Abs($sample))
        $energy += $sample * $sample
        $writer.Write([int16]([Math]::Round($sample * 32767.0)))
    }
    if ($peak -ge 1.0 -or $energy -le 0.0) { throw 'Invalid horn audio' }
    Write-Host "summoninghorn: $count samples, $seconds seconds, peak $peak, RMS $([Math]::Sqrt($energy / $count))"
}
finally { $writer.Dispose() }