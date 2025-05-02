using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("יש לציין את אות הכונן (למשל: E)");
            return;
        }

        string driveLetter = args[0].ToUpper().TrimEnd(':') + ":\\";
        if (!Directory.Exists(driveLetter))
        {
            Console.WriteLine($"הכונן {driveLetter} לא קיים או אינו נגיש.");
            return;
        }

        Console.WriteLine($"שומר צילומי מסך לכונן: {driveLetter}");

        while (true)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");


                if (!Directory.Exists($"{driveLetter}\\screen"))
                {
                    Directory.CreateDirectory($"{driveLetter}\\screen");
                }
                string filePath = Path.Combine(driveLetter, $"screen\\screenshot_{timestamp}.jpg");

                Rectangle bounds = Screen.PrimaryScreen.Bounds;

                using Bitmap fullSizeBmp = new Bitmap(bounds.Width, bounds.Height);
                using Graphics g = Graphics.FromImage(fullSizeBmp);
                g.CopyFromScreen(0, 0, 0, 0, fullSizeBmp.Size);

                // הקטנת גודל לרבע
                int reducedWidth = bounds.Width / 2;
                int reducedHeight = bounds.Height / 2;

                using Bitmap resizedBmp = new Bitmap(reducedWidth, reducedHeight);
                using Graphics gResized = Graphics.FromImage(resizedBmp);
                gResized.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                gResized.DrawImage(fullSizeBmp, 0, 0, reducedWidth, reducedHeight);

                SaveJpegWithQuality(resizedBmp, filePath, 40L); // איכות 40%
                Console.WriteLine($"נשמר: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("שגיאה: " + ex.Message);
            }

            Thread.Sleep(5000); // כל 5 שניות
        }
    }

    static void SaveJpegWithQuality(Image image, string filePath, long quality)
    {
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
        if (jpgEncoder == null)
        {
            image.Save(filePath, ImageFormat.Jpeg);
            return;
        }

        EncoderParameters encoderParams = new EncoderParameters(1);
        EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
        encoderParams.Param[0] = qualityParam;

        image.Save(filePath, jpgEncoder, encoderParams);
    }

    static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        return Array.Find(ImageCodecInfo.GetImageDecoders(), codec => codec.FormatID == format.Guid);
    }
}

