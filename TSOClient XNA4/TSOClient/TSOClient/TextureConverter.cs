using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient
{
    class TextureConverter
    {
        public static Texture2D BitmapToTexture(GraphicsDevice Device, Bitmap Img)
        {
            // Buffer size is size of color array multiplied by 4 because
            // each pixel has 4 color bytes.
            int BufferSize = Img.Height * Img.Width * 4;

            MemoryStream ImgStream = new MemoryStream(BufferSize);
            Img.Save(ImgStream, System.Drawing.Imaging.ImageFormat.Png);

            return Texture2D.FromStream(Device, ImgStream);
        }
    }
}
