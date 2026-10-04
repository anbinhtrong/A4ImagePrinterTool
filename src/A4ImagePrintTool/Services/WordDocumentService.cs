using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A4ImagePrintTool.Models;
using A4ImagePrintTool.Utilities;

// DrawingML and Picture namespaces
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace A4ImagePrintTool.Services;

/// <summary>
/// Service responsible for generating Microsoft Word (.docx) documents using Open XML SDK.
/// Phase 4B: Embeds the image as a DrawingML Graphic into each table cell, preserving aspect ratio and centering.
/// </summary>
public class WordDocumentService
{
    private static uint _drawingIdCounter = 1;

    /// <summary>
    /// Generates an A4 Word document with a grid table containing the specified image embedded in each cell.
    /// </summary>
    public void GenerateDocument(PrintLayoutOptions options, CalculatedLayout layout)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(layout);

        string? dir = Path.GetDirectoryName(options.OutputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using WordprocessingDocument wordDoc = WordprocessingDocument.Create(options.OutputPath, WordprocessingDocumentType.Document);

        MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document();
        Body body = mainPart.Document.AppendChild(new Body());

        // 1. Add ImagePart to the package and write image stream
        PartTypeInfo imageType = GetImagePartType(options.ImagePath);
        ImagePart imagePart = mainPart.AddImagePart(imageType);
        using (FileStream imageStream = new FileStream(options.ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            imagePart.FeedData(imageStream);
        }

        string relationshipId = mainPart.GetIdOfPart(imagePart);

        // 2. Create and append the grid table with embedded images
        Table table = CreateTable(options, layout, relationshipId);
        body.AppendChild(table);

        // 3. Append a minimal empty paragraph after table with 1pt font and 0 spacing
        // Word requires a paragraph after a table before SectionProperties.
        // Shrinking this paragraph prevents Word from creating a blank or spilled second page!
        var emptyParaPr = new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact }
        );
        body.AppendChild(new Paragraph(emptyParaPr));

        // 4. Configure A4 page setup and margins on the body SectionProperties
        SectionProperties sectionProps = CreateSectionProperties(options);
        body.AppendChild(sectionProps);

        mainPart.Document.Save();
    }

    private static PartTypeInfo GetImagePartType(string imagePath)
    {
        string extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => ImagePartType.Png,
            ".jpg" or ".jpeg" => ImagePartType.Jpeg,
            _ => throw new NotSupportedException($"Unsupported image type: {extension}")
        };
    }

    private SectionProperties CreateSectionProperties(PrintLayoutOptions options)
    {
        var sectionProps = new SectionProperties();

        double pageWidth = options.PageWidthMm > 0 ? options.PageWidthMm : ImageLayoutService.A4WidthMm;
        double pageHeight = options.PageHeightMm > 0 ? options.PageHeightMm : ImageLayoutService.A4HeightMm;

        // Page dimensions in twips
        var pageSize = new PageSize
        {
            Width = UnitConverter.MmToTwips(pageWidth),
            Height = UnitConverter.MmToTwips(pageHeight),
            Orient = PageOrientationValues.Portrait
        };

        var pageMargin = new PageMargin
        {
            Top = UnitConverter.MmToTwipsInt(options.MarginTopMm),
            Bottom = UnitConverter.MmToTwipsInt(options.MarginBottomMm),
            Left = UnitConverter.MmToTwips(options.MarginLeftMm),
            Right = UnitConverter.MmToTwips(options.MarginRightMm),
            Header = 0,
            Footer = 0,
            Gutter = 0
        };

        sectionProps.AppendChild(pageSize);
        sectionProps.AppendChild(pageMargin);
        return sectionProps;
    }

    private Table CreateTable(PrintLayoutOptions options, CalculatedLayout layout, string relationshipId)
    {
        var table = new Table();

        uint totalTableWidthTwips = UnitConverter.MmToTwips(layout.AvailableWidthMm);
        uint cellWidthTwips = UnitConverter.MmToTwips(layout.CellWidthMm);
        uint cellHeightTwips = UnitConverter.MmToTwips(layout.CellHeightMm);

        // Table Properties
        var tblPr = new TableProperties();

        // Fixed table width in twips (Dxa)
        tblPr.AppendChild(new TableWidth { Width = totalTableWidthTwips.ToString(), Type = TableWidthUnitValues.Dxa });
        tblPr.AppendChild(new TableLayout { Type = TableLayoutValues.Fixed });

        // Center table horizontally on page
        tblPr.AppendChild(new TableJustification { Val = TableRowAlignmentValues.Center });

        // Zero out default cell margins so images/cells span the exact calculated bounds
        var defaultCellMargins = new TableCellMarginDefault(
            new TopMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
            new BottomMargin { Width = "0", Type = TableWidthUnitValues.Dxa },
            new TableCellLeftMargin { Width = 0, Type = TableWidthValues.Dxa },
            new TableCellRightMargin { Width = 0, Type = TableWidthValues.Dxa }
        );
        tblPr.AppendChild(defaultCellMargins);

        // Configure table borders (cutting lines)
        tblPr.AppendChild(CreateBorders(options.ShowCuttingLines));

        // Disable automatic style borders/formatting from Word default table templates
        tblPr.AppendChild(new TableLook
        {
            Val = "0000",
            FirstRow = false,
            LastRow = false,
            FirstColumn = false,
            LastColumn = false,
            NoHorizontalBand = true,
            NoVerticalBand = true
        });

        table.AppendChild(tblPr);

        // Define TableGrid (columns)
        var tblGrid = new TableGrid();
        for (int c = 0; c < layout.Columns; c++)
        {
            tblGrid.AppendChild(new GridColumn { Width = cellWidthTwips.ToString() });
        }
        table.AppendChild(tblGrid);

        // Rendered image size in EMUs:
        // Use a 96% scaling factor so the image leaves a 2% breathable margin within the cell,
        // preventing Word from clipping top borders or pushing the subsequent row to page 2.
        double safetyScale = 0.96;
        long imageWidthEmu = UnitConverter.MmToEmu(layout.RenderedImageWidthMm * safetyScale);
        long imageHeightEmu = UnitConverter.MmToEmu(layout.RenderedImageHeightMm * safetyScale);

        // Construct Rows and Cells
        for (int r = 0; r < layout.Rows; r++)
        {
            var row = new TableRow();

            // Row properties: allow row to size to content without clipping, but do not allow page split
            var rowPr = new TableRowProperties(
                new CantSplit()
            );
            row.AppendChild(rowPr);

            for (int c = 0; c < layout.Columns; c++)
            {
                var cell = new TableCell();

                var cellPr = new TableCellProperties(
                    new TableCellWidth { Width = cellWidthTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                );

                if (!options.ShowCuttingLines)
                {
                    cellPr.AppendChild(new TableCellBorders(
                        new TopBorder { Val = BorderValues.None },
                        new BottomBorder { Val = BorderValues.None },
                        new LeftBorder { Val = BorderValues.None },
                        new RightBorder { Val = BorderValues.None }
                    ));
                }

                cell.AppendChild(cellPr);

                // Paragraph with centered justification, zero vertical spacing and font size 1pt
                var paraPr = new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                );

                // Create DrawingML Drawing element containing the picture
                var drawing = CreateDrawingElement(relationshipId, imageWidthEmu, imageHeightEmu);
                var run = new Run(
                    new RunProperties(new FontSize { Val = "2" }), // 1pt font size
                    drawing
                );
                var paragraph = new Paragraph(paraPr, run);

                cell.AppendChild(paragraph);
                row.AppendChild(cell);
            }

            table.AppendChild(row);
        }

        return table;
    }

    private static Drawing CreateDrawingElement(string relationshipId, long widthEmu, long heightEmu)
    {
        uint docPrId = Interlocked.Increment(ref _drawingIdCounter);

        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = docPrId, Name = $"Picture {docPrId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }
                ),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Image.png" },
                                new PIC.NonVisualPictureDrawingProperties()
                            ),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print },
                                new A.Stretch(new A.FillRectangle())
                            ),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }
                                ),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            {
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            }
        );
    }

    private TableBorders CreateBorders(bool showCuttingLines)
    {
        if (showCuttingLines)
        {
            return new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }
            );
        }

        return new TableBorders(
            new TopBorder { Val = BorderValues.None },
            new BottomBorder { Val = BorderValues.None },
            new LeftBorder { Val = BorderValues.None },
            new RightBorder { Val = BorderValues.None },
            new InsideHorizontalBorder { Val = BorderValues.None },
            new InsideVerticalBorder { Val = BorderValues.None }
        );
    }
}
