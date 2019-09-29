using System.Collections.Generic;

namespace DaHo.Chip8.Cpu
{
    public readonly struct DebugData
    {
        public DebugData(ushort pc, ushort soundTimer, ushort delayTimer, Stack<ushort> stack, List<byte> registers, ushort indexRegister)
        {
            Pc = pc;
            SoundTimer = soundTimer;
            DelayTimer = delayTimer;
            Stack = stack;
            Registers = registers;
            IndexRegister = indexRegister;
        }

        public readonly ushort Pc { get; }

        public readonly ushort SoundTimer { get; }

        public readonly ushort DelayTimer { get; }

        public readonly Stack<ushort> Stack { get; }

        public readonly List<byte> Registers { get; }

        public readonly ushort IndexRegister { get; }

    }
}
