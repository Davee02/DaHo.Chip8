using DaHo.Chip8.Cpu;
using SFML.Graphics;
using SFML.System;

namespace DaHo.Chip8
{
    internal class SfmlPPU : IPPU
    {
        private readonly Texture _texture;
        private readonly Sprite _sprite;
        private readonly byte[] _pixels;

        public SfmlPPU()
        {
            _texture = new Texture(Chip8Emu.DISPLAY_WIDTH, Chip8Emu.DISPLAY_HEIGHT);
            _sprite = new Sprite(_texture) { Scale = new Vector2f(10, 10) };
            _pixels = new byte[Chip8Emu.DISPLAY_WIDTH * Chip8Emu.DISPLAY_HEIGHT * 4];
        }

        public void DrawDisplay(bool[,] pixels)
        {
            for (int y = 0; y < Chip8Emu.DISPLAY_HEIGHT; y++)
            {
                for (int x = 0; x < Chip8Emu.DISPLAY_WIDTH; x++)
                {
                    int arrayPosition = (y * Chip8Emu.DISPLAY_WIDTH + x) * 4;
                    _pixels[arrayPosition] = (byte)(pixels[y, x] ? 0xFF : 0);
                    _pixels[arrayPosition + 1] = (byte)(pixels[y, x] ? 0xFF : 0);
                    _pixels[arrayPosition + 2] = (byte)(pixels[y, x] ? 0xFF : 0);
                    _pixels[arrayPosition + 3] = 0xFF;
                }
            }

            _texture.Update(_pixels);
        }

        public Sprite GetSprite() => _sprite;
    }
}