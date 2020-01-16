# Better-Printing-for-OneNote
This project aims to deliver an improved printing experience for OneNote when printing handwritten pages.

## Motivation
When you try to print handwritten text that is larger than a single page of your printer, OneNote just cuts the pages where the bounds are. Often times, text gets split in half and also, if that happens, text is duplicated to the next page. To solve this you have to manually add margins into your handwriting, then look at the print preview again, add another margin, and so forth.

## Features

- easily add page breaks and remove sections
- automatic reversion of OneNote's weird page splitting (don't worry if it looks weird in OneNote, just print it)
- work with PDF and PostScript files
- add page numbers
- add a signature
- directly print from this application
- what you see is what you get; if you select a printer to later print to, the page layout will be applied to the document viewer

## Prerequisites

- Windows 10 64 bit
- [.NET Core](https://dotnet.microsoft.com/download) x64 runtime
- [GhostScript](https://www.ghostscript.com/download/gsdnld.html) x64 (version for working with PDFs and PostScript)

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
%temp%/Better-Printing-for-OneNote/Documents/
Command: 
"C:\Program Files\Better-Printing-for-OneNote\Better-Printing-for-OneNote.exe" "${file}"
```

![PDF24_ConfigurationForAutomation.png](https://user-images.githubusercontent.com/14842772/72112750-cee00c00-333e-11ea-95bf-da4f186c84ba.png)

