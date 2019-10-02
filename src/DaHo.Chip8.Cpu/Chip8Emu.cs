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
        private const int FONT_INDEX = 0x50;
        private const int ROM_INDEX = 0x200;

        private readonly bool[,] _displayBuffer = new bool[DISPLAY_HEIGHT, DISPLAY_WIDTH];
        private readonly byte[] _memory = new byte[0x1000];
        private readonly byte[] _registers = new byte[16];
        private readonly Stack<ushort> _stack = new Stack<ushort>(16);

        private readonly IPPU _ppu;
        private readonly IAudioDevice _audioDevice;
        private readonly IInputDevice _inputDevice;
        private readonly Random _rng = new Random();

        private ushort _index;
        private ushort _pc = ROM_INDEX;
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

            Array.Copy(rom, 0, _memory, ROM_INDEX, rom.Length);

            var font = fontLoader.GetFont();
            Array.Copy(font, 0, _memory, FONT_INDEX, font.Length);

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
                {0x8, Arithmetic },
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

        /// <summary>
        /// Completes a full instruction cycle (fetch, decode, execute)
        /// </summary>
        public void Tick()
        {
            var opCode = FetchOpCode();
            var opCodeAction = DecodeOpCode(opCode);
            var opCodeData = GetOpCodeData(opCode);

            _pc += 2;
            opCodeAction(opCodeData);
        }

        /// <summary>
        /// Resets the CPU to the default values.
        /// The content of the memory is preserved
        /// </summary>
        public void ResetCpu()
        {
            _index = 0;
            _pc = ROM_INDEX;
            _delayTimer = 0;
            _soundTimer = 0;
            _stack.Clear();
            Array.Clear(_registers, 0, _registers.Length);

            ClearScreen(new OpCodeData());
        }

        public DebugData GetDebugData() =>
            new DebugData(_pc, _soundTimer, _delayTimer, _stack, _registers.ToList(), _index);


        /// <summary>
        /// Extracts certain bytes and nibbles out of the two bytes long opcode
        /// </summary>
        /// <param name="opCode">The two bytes long opcode</param>
        private static OpCodeData GetOpCodeData(ushort opCode)
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

        /// <summary>
        /// Reads the next opcode out of the memory
        /// </summary>
        private ushort FetchOpCode()
        {
            // Read the two bytes of OpCode (big endian).
            return (ushort)(_memory[_pc] << 8 | _memory[_pc + 1]);
        }

        private Action<OpCodeData> DecodeOpCode(ushort opCode)
        {
            // Look up the OpCode using the first nibble
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

        /// <summary>
        /// Executes the opcode beginning with 0x0
        /// </summary>
        private void Misc0(OpCodeData data)
        {
            if (_misc0OpCodes.ContainsKey(data.NN))
                _misc0OpCodes[data.NN](data);
            else
                throw new NotSupportedException($"The opcode 0{data.NNN:X3} is not supported");
        }

        /// <summary>
        /// Executes the opcode beginning with 0xF
        /// </summary>
         private void MiscF(OpCodeData data)
        {
            if (_miscFOpCodes.ContainsKey(data.NN))
                _miscFOpCodes[data.NN](data);
            else
                throw new NotSupportedException($"The opcode F{data.NNN:X3} is not supported");
        }

        /// <summary>
		/// Skips the next instruction based on the key at VX being pressed / not pressed.
		/// </summary>
        private void KeyPressedOps(OpCodeData data)
        {
            var keys = _inputDevice.GetPressedKeys();
            switch (data.NN)
            {
                case 0x9E:
                    // If key is pressed
                    if (keys.Contains(_registers[data.X]))
                        _pc += 2;
                    break;
                case 0xA1:
                    // If key is NOT pressed
                    if (!keys.Contains(_registers[data.X]))
                        _pc += 2;
                    break;
            }
        }

        /// <summary>
		/// Draws an N-byte sprite from register I at (VX / VY). Sets VF=1 a collision occures 
		/// </summary>
        private void DrawSprite(OpCodeData data)
        {
            byte x = _registers[data.X]; // The x coordinate of the sprite
            byte y = _registers[data.Y]; // The y coordinate of the sprite
            byte height = data.N; // The height of the sprite
            _registers[0xF] = 0; // Set VF to 0 before we start drawing the sprite

            for (int yline = 0; yline < height; yline++) // Loop through all the lines of the sprite (defined by the height)
            {
                var spriteLine = _memory[_index + yline]; // One horizontal 8bit long line of the sprite.

                for (int xline = 0; xline < 8; xline++) // Loop through all individual bits of the line
                {
                    byte xCoord = (byte)((x + xline) % DISPLAY_WIDTH); // The x xoordinate of the pixel
                    byte yCoord = (byte)((y + yline) % DISPLAY_HEIGHT); // The y xoordinate of the pixel

                    var spriteBit = (spriteLine >> (7 - xline)) & 1; // The new pixel
                    var oldBit = _displayBuffer[yCoord, xCoord] ? 1 : 0; // The current drawn pixel

                    // If the new pixel isn't equal to the old, the screen has to be redrawn ( = refresh it)
                    if (oldBit != spriteBit)
                        _redrawScreen = true;

                    // New bit is XOR of existing and new.
                    var newBit = oldBit ^ spriteBit;

                    _displayBuffer[yCoord, xCoord] = newBit == 1;

                    // If we wiped out a pixel, set flag for collission (VF = 1)
                    if (oldBit == 1 && newBit == 0)
                        _registers[0xF] = 1;
                }
            }
        }

        /// <summary>
		/// ANDs a random number with NN and stores in VX
		/// </summary>
        private void SetRandomX(OpCodeData data)
        {
            _registers[data.X] = (byte)(_rng.Next(0, 0xFF) & data.NN);
        }

        /// <summary>
		/// Sets the I register to NNN
		/// </summary>
        private void SetIToNNN(OpCodeData data)
        {
            _index = data.NNN;
        }


        /// <summary>
        /// Performs some kind of math on VX
        /// </summary>
        /// <param name="data"></param>
        private void Arithmetic(OpCodeData data)
        {
            switch (data.N)
            {
                case 0x0:
                    // Store the value of register VY in register VX
                    _registers[data.X] = _registers[data.Y];
                    break;
                case 0x1:
                    // Set VX to VX OR VY
                    _registers[data.X] |= _registers[data.Y];
                    break;
                case 0x2:
                    // Set VX to VX AND VY
                    _registers[data.X] &= _registers[data.Y];
                    break;
                case 0x3:
                    // Set VX to VX XOR VY
                    _registers[data.X] ^= _registers[data.Y];
                    break;
                case 0x4:
                    // Add the value of register VY to register VX
                    // Set VF to 1 if a carry occurs
                    // Set VF to 0 if a carry does not occur
                    if (_registers[data.X] + _registers[data.Y] > 0xFF)
                        _registers[0xF] = 1;
                    else
                        _registers[0xF] = 0;

                    _registers[data.X] += _registers[data.Y];
                    break;
                case 0x5:
                    // Subtract the value of register VY from register VX
                    // Set VF to 00 if a borrow occurs
                    // Set VF to 01 if a borrow does not occur
                    if (_registers[data.X] - _registers[data.Y] < 0)
                        _registers[0xF] = 0;
                    else
                        _registers[0xF] = 1;

                    _registers[data.X] -= _registers[data.Y];
                    break;
                case 0x6:
                    // Store the value of register VX shifted right one bit in register VX
                    // Set register VF to the least significant bit prior to the shift
                    _registers[0xF] = (byte)(_registers[data.X] & 0b0000_0001);
                    _registers[data.X] = (byte)(_registers[data.X] >> 0x1);
                    break;
                case 0x7:
                    // Set register VX to the value of VY minus VX
                    // Set VF to 00 if a borrow occurs
                    // Set VF to 01 if a borrow does not occur
                    if (_registers[data.Y] - _registers[data.X] < 0)
                        _registers[0xF] = 0;
                    else
                        _registers[0xF] = 1;

                    _registers[data.X] = (byte)(_registers[data.Y] - _registers[data.X]);
                    break;
                case 0xE:
                    // Store the value of register VX shifted left one bit in register VX
                    // Set register VF to the most significant bit prior to the shift
                    _registers[0xF] = (byte)(_registers[data.X] >> 7);
                    _registers[data.X] = (byte)(_registers[data.X] << 0x1);
                    break;
                default:
                    throw new NotSupportedException($"{data.N:X1} is not supported");
            }
        }

        /// <summary>
        /// Adds NN to VX
        /// </summary>
        private void AddXToNN(OpCodeData data)
        {
            _registers[data.X] += data.NN;
        }

        /// <summary>
		/// Sets the value of VX to NN
		/// </summary>
        private void SetXToNN(OpCodeData data)
        {
            _registers[data.X] = data.NN;
        }

        /// <summary>
		/// Skips the next instruction if VX != VY
		/// </summary>
        private void SkipXNotEqualsY(OpCodeData data)
        {
            if (_registers[data.X] != _registers[data.Y])
                _pc += 2;
        }

        /// <summary>
		/// Skips the next instruction if VX == VY
		/// </summary>
        private void SkipXEqualsY(OpCodeData data)
        {
            if (_registers[data.X] == _registers[data.Y])
                _pc += 2;
        }

        /// <summary>
		/// Skips the next instruction if VX != NN
		/// </summary>
        private void SkipXNotEqualsNN(OpCodeData data)
        {
            if (_registers[data.X] != data.NN)
                _pc += 2;
        }

        /// <summary>
		/// Skips the next instruction if VX == NN
		/// </summary>
        private void SkipXEqualsNN(OpCodeData data)
        {
            if (_registers[data.X] == data.NN)
                _pc += 2;
        }

        /// <summary>
		/// Jumps to subroutine NNN 
        /// Unlike Jump, this pushes the previous program counter to the stack to allow return
		/// </summary>
        private void CallSubroutine(OpCodeData data)
        {
            _stack.Push(_pc);
            _pc = data.NNN;
        }

        /// <summary>
		/// Jumps to location NNN 
        /// Not a subroutine, so old program counter is not pushed to the stack
		/// </summary>
        private void JumpToNNN(OpCodeData data)
        {
            _pc = data.NNN;
        }

        /// <summary>
		/// Jumps to location NNN + V0
        /// Not a subroutine, so old program counter is not pushed to the stack
		/// </summary>
        private void JumpToNNNPlusV0(OpCodeData data)
        {
            _pc = (ushort)(data.NNN + _registers[0]);
        }

        /// <summary>
        /// Returns from a subroutine with setting the program-counter to the top most element on the stack
        /// </summary>
        /// <param name="data"></param>
        private void ReturnSubroutine(OpCodeData data)
        {
            _pc = _stack.Pop();
        }

        /// <summary>
        /// Clears the screen (set all fixels to false/>
        /// </summary>
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

        /// <summary>
        /// Loads all registers from the address in register I
        /// </summary>
        private void LoadX(OpCodeData data)
        {
            for (var i = 0; i <= data.X; i++)
                _registers[i] = _memory[_index + i];
        }

        /// <summary>
        /// Saves all registers from the address in register I
        /// </summary>
        private void SetX(OpCodeData data)
        {
            for (var i = 0; i <= data.X; i++)
                _memory[_index + i] = _registers[i];
        }

        /// <summary>
		/// Takes the decimal representation of VX and puts each character into memory locations
		/// starting at I (with a maximum of 3)
		/// </summary
        private void SetIToBCD(OpCodeData data)
        {
            _memory[_index] = (byte)(_registers[data.X] / 100);
            _memory[_index + 1] = (byte)((_registers[data.X] / 10) % 10);
            _memory[_index + 2] = (byte)((_registers[data.X] % 100) % 10);
        }

        /// <summary>
		/// Sets I to the correct location of the font sprite VX
		/// Each font sprite is 5 bytes long.
		/// </summary>
        private void SetIToFont(OpCodeData data)
        {
            var font = _registers[data.X];
            _index = (ushort)(FONT_INDEX + (font * 5));
        }

        private void AddXToI(OpCodeData data)
        {
            if (_registers[data.X] + _index > 0xFFF)
                _registers[0xF] = 1;
            else
                _registers[0xF] = 0;

            _index += _registers[data.X];
        }

        /// <summary>
        /// Sets the sound timer to the value of VX
        /// </summary>
        private void SetSoundTimer(OpCodeData data)
        {
            _soundTimer = _registers[data.X];
        }

        /// <summary>
        /// Sets the delay timer to the value of VX
        /// </summary>
        private void SetDelayTimer(OpCodeData data)
        {
            _delayTimer = _registers[data.X];
        }

        /// <summary>
		/// Waits for a key to be pressed by looping at the current instruction
		/// </summary>
        private void AwaitAndStoreKeyPress(OpCodeData data)
        {
            var keys = _inputDevice.GetPressedKeys();
            if (keys.Length == 0)
                _pc -= 2;
            else
                _registers[data.X] = keys[0];
        }

        /// <summary>
        /// Sets VX to the value of the delay timer
        /// </summary>
        private void SetXToTimer(OpCodeData data)
        {
            _registers[data.X] = (byte)_delayTimer;
        }
    }
}
