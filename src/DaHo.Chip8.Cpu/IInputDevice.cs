namespace DaHo.Chip8.Cpu
{
    public interface IInputDevice
    {
        /// <summary>
        /// Returns all the pressed keys
        /// </summary>
        byte[] GetPressedKeys();
    }
}
