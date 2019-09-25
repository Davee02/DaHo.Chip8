using DaHo.Chip8.Cpu;
using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SFML.System;
using DaHo.Chip8.Sfml;

namespace DaHo.Chip8.Sfml
{
    public class Program
    {
        private readonly Chip8Emu _cpu;
        private readonly SfmlPPU _sfmlPPU;
        private readonly InputDevice _inputDevice;

        public Program(IReadOnlyList<string> args)
        {
            _sfmlPPU = new SfmlPPU();
            _inputDevice = new InputDevice();

            _cpu = new Chip8Emu(new StaticFontLoader(), new SfmlAudioDevice(), _sfmlPPU, _inputDevice, File.ReadAllBytes(args[0]));
        }

        private static void Main(string[] args)
        {
            var program = new Program(args);
            program.StartGame();
        }

        private void StartGame()
        {
            var window = new RenderWindow(new VideoMode(Chip8Emu.DISPLAY_WIDTH * 12, Chip8Emu.DISPLAY_HEIGHT * 12), "DaHo.Chip8");
            window.KeyPressed += KeyDown;
            window.KeyReleased += KeyUp;
            window.Closed += OnClosing;
            window.SetActive(false);
            window.SetFramerateLimit(Chip8Emu.DISPLAY_HZ);

            var renderThread = new Thread(() => RenderLoop(window));
            renderThread.Start();

            while (window.IsOpen)
            {
                window.DispatchEvents();
                _cpu.Tick();
                Thread.Sleep(TimeSpan.FromMilliseconds(1000 / 1000));
            }
        }

        private void RenderLoop(RenderWindow window)
        {
            window.SetActive(true);

            var font = new Font(@"res/cascadia.ttf");
            var text = new Text { Font = font, CharacterSize = 15 };
            text.Position = new Vector2f(Chip8Emu.DISPLAY_WIDTH * 11, 0);

            while (window.IsOpen)
            {
                var debugData = _cpu.GetDebugData();
                text.DisplayedString = GetRegisterDebugText(debugData);
                window.Clear();

                window.Draw(_sfmlPPU.GetSprite());
                window.Draw(text);

                window.Display();
            }
        }

        private string GetRegisterDebugText(DebugData data)
        {
            string text = string.Empty;
            for (var i = 0; i < data.Registers.Count; i++)
            {
                var register = data.Registers[i];
                text += $"V{i:X1}: {register:X2}{Environment.NewLine}";
            }

            return text;
        }

        private void OnClosing(object sender, EventArgs e)
        {
            var window = (RenderWindow)sender;
            window.Close();
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            _inputDevice.KeyDown(e.Code);
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            _inputDevice.KeyUp(e.Code);
        }
    }
}
