# Ethical Screen Capture Tool

This C# console application captures a screenshot of the user's primary screen every 5 seconds, resizes it to 50%, compresses it to 40% JPEG quality, and saves it to a designated external drive.  

📁 Screenshots are saved to the path:  
`<DriveLetter>:\screen\screenshot_<timestamp>.jpg`

> ⚠️ **Important Notice**  
> This tool is developed **for ethical and educational purposes only**. It must **not** be used without the user's knowledge and consent. Any misuse is strictly discouraged and may be illegal under your local laws.

---

## ✅ Features

- Captures the primary screen every 5 seconds
- Automatically resizes the image to half the original resolution
- Saves screenshots as compressed `.jpg` files (40% quality)
- Stores screenshots in a `screen` folder on a specified drive

---

## 🚀 Usage

### Requirements
- .NET 6.0 or later
- Windows OS (uses `System.Windows.Forms` and `System.Drawing`)

### How to Run

1. Build the application using Visual Studio or `dotnet build`
2. Run the executable from the command line, passing the target drive letter as an argument:

```bash
ScreenCapture.exe E

