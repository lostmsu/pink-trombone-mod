namespace Vocal {
    using System;
    using System.Linq;
    using System.Threading;
    using NAudio.Wave;

    class Program {
        static void Main() {
            var player = new WaveOutEvent {
                NumberOfBuffers = 2,
                DesiredLatency = 100,
            };
            var trombone = new PinkTromboneSampleProvider(sampleRate: 48000);
            player.Init(trombone);

            player.Play();

            for (int tone = 0; tone < 24; tone++) {
                trombone.Thrombone.SetMusicalNote(tone);
                Thread.Sleep(300);
            }
            for (int tone = 22; tone >= 0; tone--) {
                trombone.Thrombone.SetMusicalNote(tone);
                Thread.Sleep(300);
            }

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            player.Stop();
        }
    }
}
