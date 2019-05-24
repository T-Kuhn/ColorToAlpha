using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EdgeColorRemover
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Press 'a' to overwrite all files.");
                Console.WriteLine("Press anything else then 'a' to delete 'tap' and overwrite the normal file.");
                var enteredCharacter = Console.ReadLine();
                var edgeCutter = new EdgeCutter(enteredCharacter == "a");

                edgeCutter.ProcessDirectory("./images/");
            }
            finally
            {
                Console.WriteLine("Press enter to close...");
                Console.ReadLine();
            }
        }
    }

    public class EdgeCutter
    {
        private bool _replaceAll;
        private DoubleColor _backgroundColor;

        public EdgeCutter(bool replaceAll)
        {
            Console.WriteLine("replaceAll: " + replaceAll);
            _replaceAll = replaceAll;
            _backgroundColor = Color.FromArgb(255, 6, 208, 0).ToDoubleColor();
            //_backgroundColor = Color.FromArgb(255, 0, 166, 255).ToDoubleColor();
        }

        public Bitmap Cut(string path)
        {
            Bitmap img = null;
            using (var image = new Bitmap(path))
            {
                img = new Bitmap(image);
                image.Dispose();
                var diffMax = 0.0;
                Func<DoubleColor, Double> getColorDiff =  (col) => col.B - col.R;

                // get greedDifMax
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        var col = img.GetPixel(i, j).ToDoubleColor();
                        var diff = getColorDiff(col);
                        if (diff > diffMax)
                        {
                            diffMax = diff;
                        }
                    }
                }

                // backgroundColor to Alpha
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        var col = img.GetPixel(i, j).ToDoubleColor();

                        // How much more green is there than blue in this specific pixel
                        var diff = getColorDiff(col);

                        // if there IS more green than blue in this pixel, it is a greenish pixel and
                        // needs to be corrected.
                        if (diff > 0)
                        {
                            // correcting greenish pixel in here.

                            // - - calculate the alpha of the new pixel - -
                            // - We already know that it will be a white (1,1,1) pixel.
                            // - We also know that pixels that are (6, 208, 0) will need to get an
                            //   alpha of 1.
                            var degreeInDifference = 1 / diffMax * diff;
                            var alpha = degreeInDifference - 1;

                            // since the algorithm which added the green edge to the
                            // text depicted in the image data wasn't doing traditional alpha blending
                            // the alpha needs to be distorted a little to get as close as possible
                            // to the ideal result.
                            alpha *= alpha;

                            var newCol = new DoubleColor(Clamp01(alpha), 1, 1, 1).ToByteColor();
                            img.SetPixel(i, j, newCol);
                        }
                    }
                }
            }
            return img;
        }

        private double Clamp01(double number)
        {
            return (number > 1) ? 1 : (number < 0) ? 0 : number;
        }

        public void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries) { ProcessFile(fileName); }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries) { ProcessDirectory(subdirectory); }
        }

        private void ProcessFile(string path)
        {
            if (!_replaceAll && path.Contains("tap") && path.Contains(".png") & !path.Contains(".meta"))
            {
                var edgelessBitmap = Cut(path);

                // create path without the "_tap"
                var savePath = path.Replace("_tap", "");

                if (System.IO.File.Exists(savePath))
                {
                    //Console.WriteLine("saving: " + savePath);
                    System.IO.File.Delete(savePath);
                    Console.WriteLine("overwrote: " + savePath);
                    System.IO.File.Delete(path);
                    Console.WriteLine("deleted: " + path);
                    edgelessBitmap.Save(savePath, ImageFormat.Png);
                }
                else
                {
                    Console.WriteLine("rename request: " + path);
                }
            }
            else if (_replaceAll & !path.Contains(".meta") && path.Contains(".png"))
            {
                var edgelessBitmap = Cut(path);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    Console.WriteLine("overwrote: " + path);
                    edgelessBitmap.Save(path, ImageFormat.Png);
                }
            }
        }
    }

    public static class ColorExtensions
    {
        // Color to DoubleColor
        public static DoubleColor ToDoubleColor(this Color col)
        {
            return new DoubleColor(
                col.A / 255.0,
                col.R / 255.0,
                col.G / 255.0,
                col.B / 255.0);
        }
    }

    // Color with components that range from 0 to 1.
    public class DoubleColor
    {
        public double A;
        public double R;
        public double G;
        public double B;

        public DoubleColor(double a, double r, double g, double b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public Color ToByteColor()
        {
            return Color.FromArgb(
                Convert.ToByte(A * 255.0),
                Convert.ToByte(R * 255.0),
                Convert.ToByte(G * 255.0),
                Convert.ToByte(B * 255.0));
        }
    }
}