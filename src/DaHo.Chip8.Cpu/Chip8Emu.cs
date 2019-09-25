using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace DaHo.Chip8.Cpu
{
    public class Chip8Emu
    {
        public const ushort DISPLAY_WIDTH = 64;
        public const ushort DISPLAY_HEIGHT = 32;
        public const ushort DISPLAY_HZ = 60;

        private readonly bool[,] _displayBuffer = new bool[DISPLAY_HEIGHT, DISPLAY_WIDTH];
        private readonly byte[] _memory = new byte[0x1000];
        private readonly byte[] _registers = new byte[16];
        private readonly Stack<ushort> _stack = new Stack<ushort>(16);

        private readonly IPPU _ppu;
        private readonly IAudioDevice _audioDevice;
        private readonly IInputDevice _inputDevice;
        private readonly Random _rng = new Random();

        private ushort _index;
        private ushort _pc = 0x200;
        private ushort _delayTimer;
        private ushort _soundTimer;
        private bool _redrawScreen;

        private readonly IReadOnlyDictionary<byte, Action<OpCodeData>> _opCodes;
        private readonly IReadOnlyDictionary<byte, Action<OpCodeData>> _misc0OpCodes;
        private readonly IReadOnlyDictionary<byte, Action<OpCodeData>> _miscFOpCodes;


        public Chip8Emu(IFontLoader fontLoader, IAudioDevice audioDevice, IPPU ppu, IInputDevice inputDevice, byte[] rom)
        {
            _audioDevice = audioDevice;
            _ppu = ppu;
            _inputDevice = inputDevice;

            Array.Copy(rom, 0, _memory, 0x200, rom.Length);

            var font = fontLoader.GetFont();
            Array.Copy(font, 0, _memory, 0x50, font.Length);

            _opCodes = new Dictionary<byte, Action<OpCodeData>>
            {
                {0x0, Misc0 },
                {0x1, JumpToNNN },
                {0x2, CallSubroutine },
                {0x3, SkipXEqualsNN },
                {0x4, SkipXNotEqualsNN },
                {0x5, SkipXEqualsY },
                {0x6, SetXToNN },
                {0x7, AddXToNN },
                {0x8, Algebra },
                {0x9, SkipXNotEqualsY },
                {0xA, SetIToNNN },
                {0xB, JumpToNNNPlusV0 },
                {0xC, SetRandomX },
                {0xD, DrawSprite },
                {0xE, KeyPressedOps },
                {0xF, MiscF },
            };
            _misc0OpCodes = new Dictionary<byte, Action<OpCodeData>>
            {
                {0xE0, ClearScreen},
                {0xEE, ReturnSubroutine},
            };
            _miscFOpCodes = new Dictionary<byte, Action<OpCodeData>>
            {
                {0x07, SetXToTimer},
                {0x0A, AwaitAndStoreKeyPress},
                {0x15, SetDelayTimer},
                {0x18, SetSoundTimer},
                {0x1E, AddXToI},
                {0x29, SetIToFont},
                {0x33, SetIToBCD},
                {0x55, SetX},
                {0x65, LoadX},
            };

            var timer = new Timer(1000 / DISPLAY_HZ);
            timer.Elapsed += On60HzTimerTick;
            timer.Start();
        }


        public void Tick()
        {
            var opCode = FetchOpCode();
            var opCodeAction = DecodeOpCode(opCode);
            var opCodeData = GetOpCodeData(opCode);

            _pc += 2;
            opCodeAction(opCodeData);
        }

        public void ResetCpu()
        {
            _index = 0;
            _pc = 0x200;
            _delayTimer = 0;
            _soundTimer = 0;
            _stack.Clear();
            Array.Clear(_registers, 0, _registers.Length);

            ClearScreen(new OpCodeData());
        }

        public DebugData GetDebugData() =>
            new DebugData(_pc, _soundTimer, _delayTimer, _stack, _registers.ToList(), _index);

        private OpCodeData GetOpCodeData(ushort opCode)
        {
            return new OpCodeData
            (
                nnn: (ushort)(opCode & 0x0FFF),
                nn: (byte)(opCode & 0x00FF),
                n: (byte)(opCode & 0x000F),
                x: (byte)((opCode & 0x0F00) >> 8),
                y: (byte)((opCode & 0x00F0) >> 4)
            );
        }

        private ushort FetchOpCode()
        {
            return (ushort)(_memory[_pc] << 8 | _memory[_pc + 1]);
        }

        private Action<OpCodeData> DecodeOpCode(ushort opCode)
        {
            var opCodeNibble = (byte)(opCode >> 12);
            if (_opCodes.ContainsKey(opCodeNibble))
                return _opCodes[opCodeNibble];

            throw new NotSupportedException($"The opcode {opCode:X4} is not supported");
        }

        private void On60HzTimerTick(object sender, ElapsedEventArgs e)
        {
            if (_soundTimer > 0)
                _soundTimer--;

            if (_delayTimer > 0)
                _delayTimer--;

            if (_redrawScreen)
            {
                _ppu.DrawDisplay(_displayBuffer);
                _redrawScreen = false;
            }

            if (_soundTimer > 0)
                _audioDevice.PlayBeep();
        }


        private void KeyPressedOps(OpCodeData data)
        {
            var keys = _inputDevice.GetPressedKeys();
            switch (data.NN)
            {
                case 0x9E:
                    if (keys.Contains(_registers[data.X]))
                        _pc += 2;
                    break;
                case 0xA1:
                    if (!keys.Contains(_registers[data.X]))
                        _pc += 2;
                    break;
            }
        }

        private void DrawSprite(OpCodeData data)
        {
            byte x = _registers[data.X];
            byte y = _registers[data.Y];
            byte height = data.N;
            _registers[0xF] = 0;

            for (int yline = 0; yline < height; yline++)
            {
                var spriteLine = _memory[_index + yline];

                for (int xline = 0; xline < 8; xline++)
                {
                    byte xCoord = (byte)((x + xline) % DISPLAY_WIDTH);
                    byte yCoord = (byte)((y + yline) % DISPLAY_HEIGHT);

                    var spriteBit = ((spriteLine >> (7 - xline)) & 1);
                    var oldBit = _displayBuffer[yCoord, xCoord] ? 1 : 0;

                    if (oldBit != spriteBit)
                        _redrawScreen = true;

                    // New bit is XOR of existing and new.
                    var newBit = oldBit ^ spriteBit;

                    _displayBuffer[yCoord, xCoord] = newBit == 1;

                    // If we wiped out a pixel, set flag for collission.
                    if (oldBit == 1 && newBit == 0)
                        _registers[0xF] = 1;
                }
            }
        }

        private void SetRandomX(OpCodeData data)
        {
            _registers[data.X] = (byte)(_rng.Next(0, 0xFF) & data.NN);
        }

        private void JumpToNNNPlusV0(OpCodeData data)
        {
            _pc = (ushort)(data.NNN + _registers[0]);
        }

        private void SetIToNNN(OpCodeData data)
        {
            _index = data.NNN;
        }

        private void SkipXNotEqualsY(OpCodeData data)
        {
            if (_registers[data.X] != _registers[data.Y])
                _pc += 2;
        }

        private void Algebra(OpCodeData data)
        {
            switch (data.N)
            {
                case 0x0:
                    _registers[data.X] = _registers[data.Y];
                    break;
                case 0x1:
                    _registers[data.X] |= _registers[data.Y];
                    break;
                case 0x2:
                    _registers[data.X] &= _registers[data.Y];
                    break;
                case 0x3:
                    _registers[data.X] ^= _registers[data.Y];
                    break;
                case 0x4:
                    if (_registers[data.X] + _registers[data.Y] > 0xFF)
                        _registers[0xF] = 1;
                    else
                        _registers[0xF] = 0;

                    _registers[data.X] += _registers[data.Y];
                    break;
                case 0x5:
                    if (_registers[data.X] - _registers[data.Y] < 0)
                        _registers[0xF] = 0;
                    else
                        _registers[0xF] = 1;

                    _registers[data.X] -= _registers[data.Y];
                    break;
                case 0x6:
                    _registers[0xF] = (byte)(_registers[data.X] & 0b0000_0001);
                    _registers[data.X] = (byte)(_registers[data.X] >> 0x1);
                    break;
                case 0x7:
                    if (_registers[data.Y] - _registers[data.X] < 0)
                        _registers[0xF] = 0;
                    else
                        _registers[0xF] = 1;

                    _registers[data.X] = (byte)(_registers[data.Y] - _registers[data.X]);
                    break;
                case 0xE:
                    _registers[0xF] = (byte)(_registers[data.X] >> 7);
                    _registers[data.X] = (byte)(_registers[data.X] << 0x1);
                    break;
            }
        }

        private void AddXToNN(OpCodeData data)
        {
            _registers[data.X] += data.NN;
        }

        private void SetXToNN(OpCodeData data)
        {
            _registers[data.X] = data.NN;
        }

        private void SkipXEqualsY(OpCodeData data)
        {
            if (_registers[data.X] == _registers[data.Y])
                _pc += 2;
        }

        private void SkipXNotEqualsNN(OpCodeData data)
        {
            if (_registers[data.X] != data.NN)
                _pc += 2;
        }

        private void SkipXEqualsNN(OpCodeData data)
        {
            if (_registers[data.X] == data.NN)
                _pc += 2;
        }

        private void CallSubroutine(OpCodeData data)
        {
            _stack.Push(_pc);
            _pc = data.NNN;
        }

        private void JumpToNNN(OpCodeData data)
        {
            _pc = data.NNN;
        }

        private void Misc0(OpCodeData data)
        {
            if (_misc0OpCodes.ContainsKey(data.NN))
                _misc0OpCodes[data.NN](data);
            else
                throw new NotSupportedException($"The opcode 0{data.NNN:X3} is not supported");
        }

        private void MiscF(OpCodeData data)
        {
            if (_miscFOpCodes.ContainsKey(data.NN))
                _miscFOpCodes[data.NN](data);
            else
                throw new NotSupportedException($"The opcode F{data.NNN:X3} is not supported");
        }

        private void ReturnSubroutine(OpCodeData data)
        {
            _pc = _stack.Pop();
        }

        private void ClearScreen(OpCodeData data)
        {
            for (int y = 0; y < DISPLAY_HEIGHT; y++)
            {
                for (int x = 0; x < DISPLAY_WIDTH; x++)
                {
                    _displayBuffer[y, x] = false;
                }
            }

            _redrawScreen = true;
        }

        private void LoadX(OpCodeData data)
        {
            for (var i = 0; i <= data.X; i++)
                _registers[i] = _memory[_index + i];
        }

        private void SetX(OpCodeData data)
        {
            for (var i = 0; i <= data.X; i++)
                _memory[_index + i] = _registers[i];
        }

        private void SetIToBCD(OpCodeData data)
        {
            _memory[_index] = (byte)(_registers[data.X] / 100);
            _memory[_index + 1] = (byte)((_registers[data.X] / 10) % 10);
            _memory[_index + 2] = (byte)((_registers[data.X] % 100) % 10);
        }

        private void SetIToFont(OpCodeData data)
        {
            var font = _registers[data.X];
            _index = (ushort)(0x50 + font * 5);
        }

        private void AddXToI(OpCodeData data)
        {
            if (_registers[data.X] + _index > 0xFFF)
                _registers[0xF] = 1;
            else
                _registers[0xF] = 0;

            _index += _registers[data.X];
        }

        private void SetSoundTimer(OpCodeData data)
        {
            _soundTimer = _registers[data.X];
        }

        private void SetDelayTimer(OpCodeData data)
        {
            _delayTimer = _registers[data.X];
        }

        private void AwaitAndStoreKeyPress(OpCodeData data)
        {
            var keys = _inputDevice.GetPressedKeys();
            if (!keys.Any())
                _pc -= 2;
            else
                _registers[data.X] = keys[0];
        }

        private void SetXToTimer(OpCodeData data)
        {
            _registers[data.X] = (byte)_delayTimer;
        }
    }
}
