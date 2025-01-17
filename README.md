# Pink Trombone

[![Pink Trombone on NuGet](https://img.shields.io/nuget/v/PinkTrombone)](https://www.nuget.org/packages/PinkTrombone/)

This is a revised version of the Pink Trombone speech
synthesizer originally developed by Neil Thapen in 2017.
The original source code was modularized and converted to TypeScript.
Then the TypeScript code was converted to C#.

Pink Trombone uses two-dimensional
[digital waveguide synthesis](https://en.wikipedia.org/wiki/Digital_waveguide_synthesis)
to synthesize human speech sounds.

**Online demo**: [chdh.github.io/pink-trombone-mod](https://chdh.github.io/pink-trombone-mod)

Screenshot:<br/>
![Pink Trombone screenshot](WebVersionScreenshot.png)

# Sample code

You can easily connect Pink Trombone to any audio framework, that accepts
`float32` inputs. [An example](play) for [NAudio](https://github.com/naudio/NAudio):

```csharp
public sealed class PinkTromboneSampleProvider : ISampleProvider {
    public WaveFormat WaveFormat { get; }

    readonly PinkThrombone pinkThrombone;

    public PinkTromboneSampleProvider(int sampleRate) {
        this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        this.pinkThrombone = new PinkThrombone(sampleRate);
    }

    public int Read(float[] buffer, int offset, int count) {
        this.pinkThrombone.Synthesize(buffer.AsSpan().Slice(offset, count));
        return count;
    }
}

var player = new WaveOutEvent {
    NumberOfBuffers = 2,
    DesiredLatency = 100,
};
var trombone = new PinkTromboneSampleProvider(sampleRate: 48000);
player.Init(trombone);

player.Play();

Console.WriteLine("Press any key to stop");
Console.ReadKey();

player.Stop();
```

# .NET Framework 4

This package can be retrofitted to .NET 4/.NET Standard 1.x by replacing all `float`s
with `double`s and `MathF` references with `Math`.

## Bibliographic references cited by Neil Thapen

- Julius O. Smith III, "Physical audio signal processing for virtual musical instruments and audio effects."<br>
  https://ccrma.stanford.edu/~jos/pasp/

- Story, Brad H. "A parametric model of the vocal tract area function for vowel and consonant simulation."<br>
  The Journal of the Acoustical Society of America 117.5 (2005): 3231-3254.<br>
  http://sal.arizona.edu/sites/default/files/story_jasa2005.pdf

- Lu, Hui-Ling, and J. O. Smith. "Glottal source modeling for singing voice synthesis."<br>
  Proceedings of the 2000 International Computer Music Conference, 2000.

- Mullen, Jack. Physical modelling of the vocal tract with the 2D digital waveguide mesh.<br>
  PhD thesis, University of York, 2006.<br>
  http://www-users.york.ac.uk/~dtm3/Download/JackThesis.pdf
