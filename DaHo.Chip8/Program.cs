using DaHo.Chip8.Cpu;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.IO;
using System.Threading;

namespace DaHo.Chip8
{
    public class Program
    {
        private readonly Chip8Emu _cpu;
        private readonly SfmlPPU _sfmlPPU;
        private readonly InputDevice _inputDevice;
        private readonly SfmlAudioDevice _audioDevice;

        public Program(string[] args)
        {
            _sfmlPPU = new SfmlPPU();
            _inputDevice = new InputDevice();
            _audioDevice = new SfmlAudioDevice();

            _cpu = new Chip8Emu(new StaticFontLoader(), _audioDevice, _sfmlPPU, _inputDevice, File.ReadAllBytes(args[0]));
        }

        static void Main(string[] args)
        {
            var program = new Program(args);
            program.StartGame();
        }

        private void StartGame()
        {
            var window = new RenderWindow(new VideoMode(Chip8Emu.DISPLAY_WIDTH * 10, Chip8Emu.DISPLAY_HEIGHT * 10), "DaHo.Chip8");
            window.KeyPressed += KeyDown;
            window.Closed += OnClosing;
            window.SetActive(false);
            window.SetFramerateLimit(60);

            var renderThread = new Thread(() => RenderLoop(window));
            renderThread.Start();

            while(window.IsOpen)
            {
                window.DispatchEvents();

                _cpu.Tick();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000 / 750));
            }
        }

        private void RenderLoop(RenderWindow window)
        {
            window.SetActive(true);

            while(window.IsOpen)
            {
                window.Clear();
                window.Draw(_sfmlPPU.GetSprite());
                window.Display();
            }
        }

        private void OnClosing(object? sender, EventArgs e)
        {
            var window = (RenderWindow)sender;
            window.Close();
        }

        private void KeyDown(object? sender, KeyEventArgs e)
        {
            _inputDevice.KeyDown(e.Code);
        }
    }
}
