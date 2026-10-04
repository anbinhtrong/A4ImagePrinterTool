# A4ImagePrintTool

A lightweight, high-performance C# .NET 10 console application designed to arrange and tile multiple copies of an image onto an A4 page inside a Microsoft Word (`.docx`) document for printing.

Built cleanly with **DocumentFormat.OpenXml** without requiring Microsoft Office, Word interop, WPF, or any heavy desktop UI frameworks.

---

## 🌟 Key Features

- **Proportional Image Scaling**: Images are dynamically scaled to fit within each table cell while strictly preserving their original aspect ratio (never stretched or clipped).
- **Exact Single-Page Guarantee**: Carefully manages page margins, table cell boundaries, zero internal padding, and Word line spacing so the layout never unexpectedly overflows onto a second page.
- **Configurable Grid Layout**: Arbitrary user-defined rows and columns ($2 \times 2$, $2 \times 3$, $3 \times 4$, etc.).
- **Cutting Line Guides**: Toggle subtle cutting borders or completely transparent borders for borderless printing.
- **Automatic Collision Handling**: If an output `.docx` file already exists, it automatically appends a number suffix (e.g. `(1)`, `(2)`) to avoid overwriting or file-locking conflicts.
- **Pure Zero-Dependency Image Parsing**: Direct binary header reading for PNG and JPEG dimensions without external imaging libraries (e.g., `System.Drawing`, `SkiaSharp`).
- **Shared Package Media**: Embeds the image stream only once within the Open XML package regardless of how many grid cells display it, keeping document size minimal.
- **Configurable Settings via `appsettings.json`**: Customizable default page dimensions, margins, limits, output paths, and input images.

---

## 📐 Architecture & Technology

- **Language & Runtime**: C# 13 / .NET 10
- **Document Engine**: [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK)
- **Configuration**: `Microsoft.Extensions.Configuration` (JSON)
- **Architecture**: Clean separation of concerns:
  - `Models/`: Strongly typed models (`PrintLayoutOptions`, `CalculatedLayout`, `PrintSettings`).
  - `Services/`:
    - `ImageLayoutService`: Pure geometric calculations (margins, available printable area, aspect ratios).
    - `WordDocumentService`: Open XML packaging, page setup, DrawingML image embedding, and table construction.
  - `Utilities/`:
    - `ImageInfoReader`: Fast binary header inspection for PNG and JPEG image resolutions.
    - `InputValidator`: Input sanitization, path checks, and non-conflicting file numbering.
    - `UnitConverter`: Conversions between millimeters, Twips (DXA), and EMUs.

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Building the Project

```bash
# Clone the repository
git clone https://github.com/your-username/A4ImagePrintTool.git
cd A4ImagePrintTool

# Restore and build the solution
dotnet build
```

### Running the Application

```bash
dotnet run --project src/A4ImagePrintTool/A4ImagePrintTool.csproj
```

### Interactive Console Flow

```text
=== A4 Image Print Tool ===

Image path (press Enter for default: 'D:\Practices\ImageHandlers\ImagePrinter\images\lotus_incent.png'):
> 

Rows:
> 2

Columns:
> 3

Show cutting lines? (Y/N): 
> N

Output path (press Enter for default: 'D:\Practices\ImageHandlers\ImagePrinter\Output\lotus_incent-A4.docx'):
> 

Configuration:
  Image:          lotus_incent.png
  Layout:         2 rows × 3 columns
  Total images:   6
  Cutting lines:  No
  Output:         D:\Practices\ImageHandlers\ImagePrinter\Output\lotus_incent-A4.docx

Proceed? (Y/N): 
> Y

Generating document...
✓ Document generated successfully.
Saved to: D:\Practices\ImageHandlers\ImagePrinter\Output\lotus_incent-A4.docx
```

---

## ⚙️ Configuration (`appsettings.json`)

You can customize default print settings in `src/A4ImagePrintTool/appsettings.json`:

```json
{
  "PrintSettings": {
    "PageWidthMm": 210,
    "PageHeightMm": 297,
    "MarginTopMm": 10,
    "MarginBottomMm": 10,
    "MarginLeftMm": 10,
    "MarginRightMm": 10,
    "MaxRows": 10,
    "MaxColumns": 10,
    "ShowCuttingLines": false,
    "OutputSuffix": "-A4",
    "DefaultImagePath": "D:\\Practices\\ImageHandlers\\ImagePrinter\\images\\lotus_incent.png",
    "DefaultOutputDir": "D:\\Practices\\ImageHandlers\\ImagePrinter\\Output"
  }
}
```

---

## 🧪 Unit Tests

The test suite covers layout calculations, aspect ratio constraints, document generation, Open XML measurements, and input validations.

```bash
dotnet test
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
