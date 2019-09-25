using DaHo.Chip8.Cpu;
using System.Collections.Generic;
using System.Linq;
using static SFML.Window.Keyboard;

namespace DaHo.Chip8
{
    internal class InputDevice : IInputDevice
    {
        private HashSet<byte> _pressedKeys = new HashSet<byte>(0xF);
        private readonly Dictionary<Key, byte> _keyMap = new Dictionary<Key, byte>
        {
            { Key.Num1, 0x1 },
            { Key.Num2, 0x2 },
            { Key.Num3, 0x3 },
            { Key.Num4, 0xC },
            { Key.Q, 0x4 },
            { Key.W, 0x5 },
            { Key.E, 0x6 },
            { Key.R, 0xD },
            { Key.A, 0x7 },
            { Key.S, 0x8 },
            { Key.D, 0x9 },
            { Key.F, 0xE },
            { Key.Y, 0xA },
            { Key.X, 0x0 },
            { Key.C, 0xB },
            { Key.V, 0xF },
        };

        public byte[] GetPressedKeys()
        {
            var keys = _pressedKeys.ToArray();
            _pressedKeys.Clear();
            return keys;
        }

        public void KeyDown(Key key)
        {
            if(_keyMap.ContainsKey(key))
                _pressedKeys.Add(_keyMap[key]);
        }
    }
}