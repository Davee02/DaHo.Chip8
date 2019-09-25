using DaHo.Chip8.Cpu;
using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SFML.System;
using System.Diagnostics;

namespace DaHo.Chip8.Sfml
{
    public class Program
    {
        private readonly Chip8Emu _cpu;
        private readonly SfmlPPU _sfmlPPU;
        private readonly InputDevice _inputDevice;
        private int _emulationSpeed = 750;
        private bool _isPaused = false;

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
            var window = new RenderWindow(new VideoMode(Chip8Emu.DISPLAY_WIDTH * 12, Chip8Emu.DISPLAY_HEIGHT * 13), "DaHo.Chip8");
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

                if(!_isPaused)
                {
                    _cpu.Tick();
                    Sleep(1000 / _emulationSpeed);
                }
            }
        }

        private void RenderLoop(RenderWindow window)
        {
            window.SetActive(true);

            var font = new Font("./res/cascadia.ttf");
            var registerText = new Text { Font = font, CharacterSize = 15, Position = new Vector2f(Chip8Emu.DISPLAY_WIDTH * 11, 0) };
            var miscText = new Text { Font = font, CharacterSize = 15, Position = new Vector2f(0, Chip8Emu.DISPLAY_HEIGHT * 11) };

            while (window.IsOpen)
            {
                var debugData = _cpu.GetDebugData();
                registerText.DisplayedString = GetRegisterDebugText(debugData);
                miscText.DisplayedString = GetMiscDebugText(debugData);

                window.Clear();

                window.Draw(_sfmlPPU.GetSprite());
                window.Draw(registerText);
                window.Draw(miscText);

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

        private string GetMiscDebugText(DebugData data)
        {
            string text = string.Empty;

            text += $"HZ: {_emulationSpeed}\t";
            text += $"Sound-timer: {data.SoundTimer}{Environment.NewLine}";
            text += $"PC: {data.Pc}\t";
            text += $"Delay-timer: {data.DelayTimer}{Environment.NewLine}";
            text += $"Index: {data.IndexRegister}";

            return text;
        }

        private void OnClosing(object sender, EventArgs e)
        {
            var window = (RenderWindow)sender;
            window.Close();
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Code)
            {
                case Keyboard.Key.Add:
                    _emulationSpeed += 10;
                    break;
                case Keyboard.Key.Subtract when _emulationSpeed > 10:
                    _emulationSpeed -= 10;
                    break;
                case Keyboard.Key.Pause:
                    _isPaused = !_isPaused;
                    break;
                case Keyboard.Key.Delete:
                    _cpu.ResetCpu();
                    break;
                default:
                    _inputDevice.KeyDown(e.Code);
                    break;
            }
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            _inputDevice.KeyUp(e.Code);
        }

        private void Sleep(double milliseconds)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < milliseconds) ;
        }
    }
}
