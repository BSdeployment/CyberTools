// See https://aka.ms/new-console-template for more information


using System.Runtime.CompilerServices;
using System.Xml.Schema;



string folderPath = @"C:\screenlog";

if(!Directory.Exists(folderPath)){
    Directory.CreateDirectory(folderPath);
}





void CaptureScreen(string path){

var screensize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;

    using(var bitmap = new Bitmap(screensize.Width,screensize.Height))
    {
           using(var g  = Graphics.FromImage(bitmap)){
                g.CopyFromScreen(0,0,0,0,screensize);
           }
           bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);

    }


}
int num =1;
while(true){
    string filename = Path.Combine(folderPath,$"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
    Console.WriteLine($"log {num} files now");
    CaptureScreen(filename);
    Thread.Sleep(5000);
    num++;
    
}



