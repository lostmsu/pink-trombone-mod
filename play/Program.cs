namespace Vocal {
    using System;
    using System.Linq;
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

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            player.Stop();
        }
    }
}
