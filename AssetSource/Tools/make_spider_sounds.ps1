<#
.SYNOPSIS
    Generates original monster effects as mono PCM WAV files; -AllMonsters includes Lindorm, Kraken and dragon.
#>
param([switch]$AllMonsters)

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
if ($AllMonsters) {
    $sounds += @(
        @{ Name = 'lindormidle'; Seconds = 1.3; Tone = 75; Style = 'rasp'; Noise = 0.2; Fall = 0.15 },
        @{ Name = 'lindormalert'; Seconds = 1.8; Tone = 95; Style = 'rasp'; Noise = 0.4; Fall = 0.35 },
        @{ Name = 'lindormattack'; Seconds = 0.6; Tone = 135; Style = 'rasp'; Noise = 0.5; Fall = 0.55 },
        @{ Name = 'lindormhit'; Seconds = 0.8; Tone = 180; Style = 'rasp'; Noise = 0.35; Fall = 0.4 },
        @{ Name = 'lindormdeath'; Seconds = 2.2; Tone = 110; Style = 'rasp'; Noise = 0.45; Fall = 0.75 },
        @{ Name = 'krakenidle'; Seconds = 2.0; Tone = 48; Style = 'wet'; Noise = 0.3; Fall = 0.15 },
        @{ Name = 'krakenalert'; Seconds = 2.6; Tone = 65; Style = 'wet'; Noise = 0.45; Fall = 0.3 },
        @{ Name = 'krakenattack'; Seconds = 1.2; Tone = 85; Style = 'wet'; Noise = 0.75; Fall = 0.5 },
        @{ Name = 'krakenlash'; Seconds = 0.65; Tone = 240; Style = 'wet'; Noise = 0.95; Fall = 0.8 },
        @{ Name = 'krakenhit'; Seconds = 1.0; Tone = 100; Style = 'wet'; Noise = 0.55; Fall = 0.4 },
        @{ Name = 'krakendeath'; Seconds = 3.0; Tone = 72; Style = 'wet'; Noise = 0.6; Fall = 0.8 },
        @{ Name = 'dragonidle'; Seconds = 1.5; Tone = 95; Style = 'roar'; Noise = 0.2; Fall = 0.2 },
        @{ Name = 'dragonalert'; Seconds = 2.1; Tone = 150; Style = 'roar'; Noise = 0.35; Fall = 0.3 },
        @{ Name = 'dragonattack'; Seconds = 1.2; Tone = 110; Style = 'breath'; Noise = 0.9; Fall = 0.4 },
        @{ Name = 'dragonhit'; Seconds = 0.9; Tone = 220; Style = 'roar'; Noise = 0.35; Fall = 0.5 },
        @{ Name = 'dragondeath'; Seconds = 2.5; Tone = 165; Style = 'roar'; Noise = 0.5; Fall = 0.75 }
    )
}
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
        $filteredNoise = 0.0
        $peak = 0.0
        $energy = 0.0
        for ($index = 0; $index -lt $count; $index++) {
            $time = $index / $rate
            $progress = $index / $count
            $envelope = [Math]::Min(1.0, $time / 0.012) * [Math]::Pow(1.0 - $progress, 1.8)
            $noise = 2.0 * $random.NextDouble() - 1.0
            if ($sound.Style) {
                $filteredNoise += ($noise - $filteredNoise) * 0.14
                $phase = 2.0 * [Math]::PI * $sound.Tone * ($time - 0.5 * $sound.Fall * $time * $progress)
                $voice = [Math]::Sin($phase + 0.8 * [Math]::Sin($phase * 0.51))
                $voice = 0.22 * $voice + 0.1 * [Math]::Sin(2.03 * $phase) + 0.06 * [Math]::Sin(3.97 * $phase)
                $texture = $filteredNoise * $sound.Noise
                switch ($sound.Style) {
                    'rasp' { $voice *= 0.65 + 0.35 * [Math]::Sin(2.0 * [Math]::PI * 29.0 * $time) }
                    'wet' {
                        $bubble = [Math]::Sin(2.0 * [Math]::PI * (320.0 * $time + 40.0 * $time * $time))
                        $texture += 0.14 * $bubble * [Math]::Pow([Math]::Max(0.0, [Math]::Sin(2.0 * [Math]::PI * 7.0 * $time)), 6.0)
                    }
                    'roar' { $voice = 0.4 * [Math]::Tanh(3.0 * $voice) }
                    'breath' { $voice *= 0.25; $texture += ($noise - $previousNoise) * 0.18 }
                }
                $envelope = [Math]::Min(1.0, $time / 0.035) * [Math]::Pow(1.0 - $progress, 0.8)
                $sample = ($voice + $texture) * $envelope
            }
            else {
                $hiss = ($noise - $previousNoise) * 0.22 * $sound.Hiss
                $phase = ($progress * $sound.Clicks) % 1.0
                $click = [Math]::Exp(-$phase * 45.0) * [Math]::Sin(2.0 * [Math]::PI * 1800.0 * $time) * 0.5
                $rasp = [Math]::Sin(2.0 * [Math]::PI * $sound.Tone * ($time - 0.22 * $time * $progress))
                $rasp *= 0.13 * (0.5 + 0.5 * [Math]::Sin(2.0 * [Math]::PI * 43.0 * $time))
                $sample = ($hiss + $click + $rasp) * $envelope
            }
            $previousNoise = $noise
            $peak = [Math]::Max($peak, [Math]::Abs($sample))
            $energy += $sample * $sample
            $writer.Write([int16]([Math]::Round($sample * 32767.0)))
        }
        if ($peak -ge 1.0 -or $energy -le 0.0) { throw "Invalid audio: $($sound.Name)" }
        Write-Host "$($sound.Name): $count samples, peak $peak, RMS $([Math]::Sqrt($energy / $count))"
    }
    finally { $writer.Dispose() }
}