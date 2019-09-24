namespace DaHo.Chip8.Cpu
{
    internal struct OpCodeData
    {
        public OpCodeData(ushort nnn, byte nn, byte n, byte x, byte y)
        {
            NNN = nnn;
            NN = nn;
            N = n;
            X = x;
            Y = y;
        }

        public ushort NNN { get; }

        public byte NN { get; }

        public byte N { get; }

        public byte X { get; }

        public byte Y { get; }
    }
}
