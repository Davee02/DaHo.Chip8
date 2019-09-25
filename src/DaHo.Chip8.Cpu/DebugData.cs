using System.Collections.Generic;

namespace DaHo.Chip8.Cpu
{
    public struct DebugData
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

        public ushort Pc { get; }

        public ushort SoundTimer { get; }

        public ushort DelayTimer { get; }

        public Stack<ushort> Stack { get; }

        public List<byte> Registers { get; }

        public ushort IndexRegister { get; }

    }
}
