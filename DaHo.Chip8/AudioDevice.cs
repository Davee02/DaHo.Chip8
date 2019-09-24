using DaHo.Chip8.Cpu;
using SFML.Audio;
using System;

namespace DaHo.Chip8
{
    internal class SfmlAudioDevice : IAudioDevice
    {
        private const int SAMPLES = 1200;
        private const int SAMPLE_RATE = 44100;
        private const int AMPLITUDE = 30000;
        private const int FREQUENCY = 440;

        private readonly Sound _beepSound;

        public SfmlAudioDevice()
        {
            short[] raw = new short[SAMPLES];

            double x = 0;
            for (var i = 0; i < SAMPLES; i++)
            {
                raw[i] = (short)(AMPLITUDE * Math.Sin(x * 2 * Math.PI));
                x += (double)FREQUENCY / SAMPLE_RATE;
            }

            var buffer = new SoundBuffer(raw, 1, SAMPLE_RATE);
            _beepSound = new Sound(buffer);
        }

        public void PlayBeep()
        {
            _beepSound.Play();
        }
    }
}