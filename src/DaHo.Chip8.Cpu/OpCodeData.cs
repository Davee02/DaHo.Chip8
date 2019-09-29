namespace DaHo.Chip8.Cpu
{
    internal readonly struct OpCodeData
    {
        public OpCodeData(ushort nnn, byte nn, byte n, byte x, byte y)
        {
            NNN = nnn;
            NN = nn;
            N = n;
            X = x;
            Y = y;
        }

        public readonly ushort NNN { get; }

        public readonly byte NN { get; }

        public readonly byte N { get; }

        public readonly byte X { get; }

        public readonly byte Y { get; }
    }
}
