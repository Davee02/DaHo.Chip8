namespace DaHo.Chip8.Cpu
{
    public interface IPPU
    {
        /// <summary>
        /// Draws the display
        /// </summary>
        void DrawDisplay(bool[,] pixels);
    }
}
