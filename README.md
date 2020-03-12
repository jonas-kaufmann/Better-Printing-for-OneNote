# ![Banner.png](https://user-images.githubusercontent.com/4995833/75390279-4c101200-58e8-11ea-80c0-d58cf6a28c32.png)
This project aims to deliver an improved printing experience for OneNote when printing handwritten pages. **IMPORTANT: Computer has to be left on until printing is finished!**

## Motivation
When you try to print handwritten text that is larger than a single page of your printer, OneNote just cuts the pages where the bounds are. Often times, text gets split in half and also, if that happens, text is duplicated to the next page. To solve this you have to manually add margins into your handwriting, then look at the print preview again, add another margin, and so forth.

## Features
- easily add page breaks and remove sections
- automatic reversion of OneNote's weird page splitting (don't worry if it looks weird in OneNote, just print it)
- add page numbers
- add freely placeable signatures (custom texts on all pages)
- directly print from this application
- what you see is what you print: the specific printer settings will be applied to the view, e.g. page borders
- work with any PDF

## Prerequisites

- Windows 10 64 bit
- [.NET Core](https://dotnet.microsoft.com/download) x64 runtime

## Implementation Notes

- PDFs are being converted to bitmaps in order to make them easily cropable and editable
- for printing this means that you have to **leave your computer on until printing has finished** due to the bitmaps being larger than the printer's buffer

## Automation

Of course, you don't want to manually import the printed file every time. This application can be run from command-line with a file specified:

```bash
Better-Printing-for-OneNote.exe <path to file>
```

So you need a printer that allows the execution of a command after a file has been printed. For example [PDF24](https://en.pdf24.org/) delivers that.

In order to set it up, open the **PDF24 GUI** from start menu. Click on **Settings**. Go to **PDF Printer**. I recommend creating a separate printer, so that you are still able to print PDFs the way you are used to through the standard PDF24 printer device.

The finished configuration will look somewhat like this:

```
Output Directory:
%temp%\Better-Printing-for-OneNote\Documents\
Command: 
"C:\Program Files\Better-Printing-for-OneNote\Better-Printing-for-OneNote.exe" "${file}"
```

![PDF24_ConfigurationForAutomation.png](https://user-images.githubusercontent.com/4995833/73217604-899e4580-4158-11ea-8470-ee4674a41602.PNG)

