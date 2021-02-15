using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace WinLibrary.Module
{
    public class ResourceCache : IDisposable
    {
        protected readonly Dictionary<string, Image> images = new Dictionary<string, Image>();

        protected Image getImage(string filepath)
        {
            if (!images.TryGetValue(filepath, out var img))
            {
                if (!File.Exists(filepath)) return images[filepath] = null;

                using var s = File.OpenRead(filepath);

                return images[filepath] = Image.FromStream(s);
            }

            return img;
        }


        public void Dispose()
        {
            foreach (var img in images) img.Value.Dispose();

            images.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
