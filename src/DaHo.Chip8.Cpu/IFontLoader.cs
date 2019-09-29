namespace DaHo.Chip8.Cpu
{
    public interface IFontLoader
    {
        /// <summary>
        /// Returns the whole font for the system
        /// </summary>
        byte[] GetFont();
    }
}
