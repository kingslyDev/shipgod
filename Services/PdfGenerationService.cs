using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using iText.IO.Image;
using QRCoder;
using ShipmentFinishGood.DTOs;

namespace ShipmentFinishGood.Services
{
    public class PdfGenerationService : IPdfGenerationService
    {
        public async Task<byte[]> GenerateBarcodesPdfAsync(QRManagementDto qrData)
        {
            return await Task.Run(() => GenerateBarcodesPdf(qrData));
        }

        public async Task<byte[]> GenerateQRIdentityPdfAsync(QRManagementDto qrData)
        {
            return await Task.Run(() => GenerateQRIdentityPdf(qrData));
        }

        public async Task<byte[]> GenerateComprehensiveReportPdfAsync(QRManagementDto qrData)
        {
            return await Task.Run(() => GenerateComprehensiveReportPdf(qrData));
        }

        private byte[] GenerateBarcodesPdf(QRManagementDto qrData)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);

            // Set margins for A4 paper (21cm x 29.7cm)
            document.SetMargins(36, 36, 36, 36); // 0.5 inch margins

            // Fonts
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var monospaceFont = PdfFontFactory.CreateFont(StandardFonts.COURIER);

            // Colors
            var primaryColor = new DeviceRgb(52, 58, 64);      // Dark gray
            var accentColor = new DeviceRgb(0, 123, 255);      // Blue
            var lightGray = new DeviceRgb(248, 249, 250);      // Light background

            // Header Section
            AddHeaderSection(document, qrData, titleFont, headerFont, bodyFont, primaryColor, accentColor);

            // QR Identity + PO Details Combined Section
            AddQRIdentityWithPODetailsSection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor, lightGray);

            // Add first row (2 barcode boxes) on Page 1 if any barcodes
            AddFirstBarcodeRowIfAny(document, qrData, headerFont, bodyFont, monospaceFont);

            // Remaining barcodes start from Page 2 in 2x2 grid if more than 2
            if (qrData.BarcodeList != null && qrData.BarcodeList.Count > 2)
            {
                document.Add(new AreaBreak());
                AddBarcodeListSection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor, startIndex: 2);
            }

            // Footer
            AddFooter(document, bodyFont, primaryColor);

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerateQRIdentityPdf(QRManagementDto qrData)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);

            document.SetMargins(72, 72, 72, 72); // 1 inch margins for better printing

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            try
            {

                byte[]? qrBytes = null;

                // Try existing QR first
                if (!string.IsNullOrEmpty(qrData.QRImageBase64))
                {
                    try
                    {
                        qrBytes = Convert.FromBase64String(qrData.QRImageBase64);
                    }
                    catch
                    {
                    }
                }

                // Generate new QR if needed
                if (qrBytes == null && !string.IsNullOrEmpty(qrData.QRIdentity))
                {
                    var qrGenerator = new QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(qrData.QRIdentity, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new BitmapByteQRCode(qrCodeData);
                    qrBytes = qrCode.GetGraphic(20);
                }

                // Title
                document.Add(new Paragraph("QR IDENTITY CODE")
                    .SetFont(titleFont)
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                // QR Code - Large and centered
                if (qrBytes != null)
                {
                    var qrImage = ImageDataFactory.Create(qrBytes);
                    document.Add(new Image(qrImage)
                        .SetAutoScale(true)
                        .SetMaxWidth(300)
                        .SetMaxHeight(300)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetMarginBottom(30));
                }
                else
                {
                    // Fallback if no QR available
                    document.Add(new Paragraph("[QR CODE NOT AVAILABLE]")
                        .SetFont(bodyFont)
                        .SetFontSize(16)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.RED)
                        .SetBorder(new SolidBorder(1))
                        .SetPadding(20)
                        .SetMarginBottom(30));
                }
            }
            catch (Exception ex)
            {
                
                // Add error info to PDF
                document.Add(new Paragraph("QR IDENTITY CODE - ERROR")
                    .SetFont(titleFont)
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.RED)
                    .SetMarginBottom(20));
                    
                document.Add(new Paragraph($"Error: {ex.Message}")
                    .SetFont(bodyFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(30));
            }

            // QR Identity Text - Large and clear
            document.Add(new Paragraph(qrData.QRIdentity)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBorder(new SolidBorder(1))
                .SetPadding(10)
                .SetMarginBottom(30));

            // Session Information
            var infoTable = new Table(2);
            infoTable.SetWidth(UnitValue.CreatePercentValue(100));

            infoTable.AddCell(CreateInfoCell("File Name:", headerFont))
                     .AddCell(CreateInfoCell(qrData.FileName, bodyFont));
            infoTable.AddCell(CreateInfoCell("Generated Date:", headerFont))
                     .AddCell(CreateInfoCell(qrData.GeneratedDate.ToString("dd/MM/yyyy HH:mm:ss"), bodyFont));
            infoTable.AddCell(CreateInfoCell("Generated By:", headerFont))
                     .AddCell(CreateInfoCell(qrData.GeneratedBy, bodyFont));
            infoTable.AddCell(CreateInfoCell("Status:", headerFont))
                     .AddCell(CreateInfoCell(qrData.Status, bodyFont));

            document.Add(infoTable.SetMarginBottom(20));

            // Summary
            document.Add(new Paragraph("SUMMARY")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetMarginTop(20)
                .SetMarginBottom(10));

            var summaryTable = new Table(4);
            summaryTable.SetWidth(UnitValue.CreatePercentValue(100));

            summaryTable.AddHeaderCell(CreateHeaderCell("Pallets", headerFont));
            summaryTable.AddHeaderCell(CreateHeaderCell("Boxes", headerFont));
            summaryTable.AddHeaderCell(CreateHeaderCell("Pieces", headerFont));
            summaryTable.AddHeaderCell(CreateHeaderCell("Total Qty", headerFont));

            summaryTable.AddCell(CreateSummaryCell(qrData.TotalPallets.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateSummaryCell(qrData.TotalBoxes.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateSummaryCell(qrData.TotalPcs.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateSummaryCell(qrData.TotalQty.ToString("N0"), bodyFont));

            document.Add(summaryTable);

            document.Close();
            return memoryStream.ToArray();
        }

        private byte[] GenerateComprehensiveReportPdf(QRManagementDto qrData)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new PdfWriter(memoryStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);

            document.SetMargins(36, 36, 36, 36);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Cover page
            AddCoverPage(document, qrData, titleFont, headerFont, bodyFont);
            document.Add(new AreaBreak());

            // Detailed sections
            AddDetailedReport(document, qrData, headerFont, bodyFont);

            document.Close();
            return memoryStream.ToArray();
        }

        private void AddHeaderSection(Document document, QRManagementDto qrData, 
            PdfFont titleFont, PdfFont headerFont, PdfFont bodyFont, 
            DeviceRgb primaryColor, DeviceRgb accentColor)
        {
            // Company Header
            document.Add(new Paragraph("SHIPMENT FINISH GOOD")
                .SetFont(titleFont)
                .SetFontSize(18)
                .SetFontColor(primaryColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(5));

            document.Add(new Paragraph("BARCODE REFERENCE DOCUMENT")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Add separator line
            document.Add(new Paragraph()
                .SetBorder(new SolidBorder(accentColor, 2))
                .SetMarginBottom(20));
        }

        private void AddQRIdentityWithPODetailsSection(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, PdfFont monospaceFont, DeviceRgb accentColor, DeviceRgb lightGray)
        {
            document.Add(new Paragraph("QR IDENTITY INFORMATION")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetMarginBottom(15));

            // Create main table with QR on left (40%) and PO Details on right (60%)
            var mainTable = new Table(new float[] { 40, 60 });
            mainTable.SetWidth(UnitValue.CreatePercentValue(100));
            mainTable.SetKeepTogether(true); // Keep section intact on first page

            try
            {

                byte[]? qrBytes = null;

                // Try to use existing QR image first
                if (!string.IsNullOrEmpty(qrData.QRImageBase64))
                {
                    try
                    {
                        qrBytes = Convert.FromBase64String(qrData.QRImageBase64);
                    }
                    catch
                    {
                        qrBytes = null;
                    }
                }

                // Generate new QR if no existing image or decode failed
                if (qrBytes == null && !string.IsNullOrEmpty(qrData.QRIdentity))
                {
                    try
                    {
                        var qrGenerator = new QRCodeGenerator();
                        var qrCodeData = qrGenerator.CreateQrCode(qrData.QRIdentity, QRCodeGenerator.ECCLevel.Q);
                        var qrCode = new BitmapByteQRCode(qrCodeData);
                        qrBytes = qrCode.GetGraphic(30); // Further increased module size for high scan reliability
                    }
                    catch
                    {
                        qrBytes = null;
                    }
                }

                // LEFT CELL - QR Code (styled like barcode boxes with black border)
                var qrCell = new Cell();
                qrCell.SetBorder(new SolidBorder(ColorConstants.BLACK, 1f)); // Black border like barcode boxes
                qrCell.SetPadding(10); // Reduced padding to allocate more space for QR image
                qrCell.SetTextAlignment(TextAlignment.CENTER);
                qrCell.SetVerticalAlignment(VerticalAlignment.TOP); // Align to top for consistent appearance
                qrCell.SetMinHeight(200);
                qrCell.SetKeepTogether(true);

                if (qrBytes != null)
                {
                    try
                    {
                        var qrImage = ImageDataFactory.Create(qrBytes);
                        qrCell.Add(new Image(qrImage)
                            .SetWidth(200) // Fixed large size within 40% column (~209pt width available)
                            .SetHeight(200)
                            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                            .SetMarginBottom(5));
                    }
                    catch
                    {
                        qrCell.Add(new Paragraph("[QR IMAGE ERROR]")
                            .SetFont(bodyFont)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontColor(ColorConstants.RED));
                    }
                }
                else
                {
                    qrCell.Add(new Paragraph("[NO QR IMAGE]")
                        .SetFont(bodyFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontColor(ColorConstants.RED));
                }

                mainTable.AddCell(qrCell);

                // RIGHT CELL - PO Details Table
                var poCell = new Cell();
                poCell.SetBorder(new SolidBorder(ColorConstants.BLACK, 1f)); // Black border to match QR cell
                poCell.SetPadding(15);
                poCell.SetVerticalAlignment(VerticalAlignment.TOP);
                poCell.SetKeepTogether(true);

                // Add PO Details title
                poCell.Add(new Paragraph($"PO DETAILS BREAKDOWN ({qrData.POSummaries?.Count ?? 0} POs)")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                    .SetFontSize(12)
                    .SetFontColor(accentColor)
                    .SetMarginBottom(10));

                // Create PO table
                if (qrData.POSummaries?.Any() == true)
                {
                    // Slightly adjusted column widths to give more space to first columns
                    var poTable = new Table(new float[] { 22, 18, 9, 9, 9, 16, 17 });
                    poTable.SetWidth(UnitValue.CreatePercentValue(100));
                    poTable.SetFontSize(7); // Base font size (data cells further customized)

                    // Headers (compact)
                    poTable.AddHeaderCell(CreateCompactHeaderCell("PO NUMBER", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("MODEL", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("PALLETS", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("BOXES", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("PIECES", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("SCANNED", headerFont, lightGray));
                    poTable.AddHeaderCell(CreateCompactHeaderCell("PROGRESS", headerFont, lightGray));

                    // Data rows (limit to fit in space)
                    foreach (var po in qrData.POSummaries.Take(5))
                    {
                        poTable.AddCell(CreateCompactDataCell(po.PONumber, bodyFont));
                        poTable.AddCell(CreateCompactDataCell(po.ModelProduct, bodyFont));
                        poTable.AddCell(CreateCompactDataCell(po.QtyPallet.ToString(), bodyFont));
                        poTable.AddCell(CreateCompactDataCell(po.QtyBox.ToString(), bodyFont));
                        poTable.AddCell(CreateCompactDataCell(po.QtyPcs.ToString("N0"), bodyFont));
                        poTable.AddCell(CreateCompactDataCell($"{po.ScannedCount}/{po.QtyBox}", bodyFont));
                        poTable.AddCell(CreateCompactDataCell($"{po.ScannedPercentage:F1}%", bodyFont));
                    }

                    poCell.Add(poTable);
                }
                else
                {
                    poCell.Add(new Paragraph("No PO data available")
                        .SetFont(bodyFont)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.GRAY));
                }

                mainTable.AddCell(poCell);
                document.Add(mainTable.SetMarginBottom(10));
            }
            catch (Exception ex)
            {
                document.Add(new Paragraph($"QR GENERATION ERROR: {ex.Message}")
                    .SetFont(bodyFont)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.RED)
                    .SetMarginBottom(20));
            }
        }

        private void AddSummaryStatistics(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, DeviceRgb accentColor, DeviceRgb lightGray)
        {
            document.Add(new Paragraph("QUANTITY SUMMARY")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetMarginBottom(10));

            var summaryTable = new Table(5);
            summaryTable.SetWidth(UnitValue.CreatePercentValue(100));

            // Headers
            summaryTable.AddHeaderCell(CreateStyledHeaderCell("PALLETS", headerFont, lightGray));
            summaryTable.AddHeaderCell(CreateStyledHeaderCell("BOXES", headerFont, lightGray));
            summaryTable.AddHeaderCell(CreateStyledHeaderCell("PIECES", headerFont, lightGray));
            summaryTable.AddHeaderCell(CreateStyledHeaderCell("TOTAL QTY", headerFont, lightGray));
            summaryTable.AddHeaderCell(CreateStyledHeaderCell("PROGRESS", headerFont, lightGray));

            // Values
            summaryTable.AddCell(CreateStyledDataCell(qrData.TotalPallets.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateStyledDataCell(qrData.TotalBoxes.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateStyledDataCell(qrData.TotalPcs.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateStyledDataCell(qrData.TotalQty.ToString("N0"), bodyFont));
            summaryTable.AddCell(CreateStyledDataCell($"{qrData.ScanPercentage}%", bodyFont));

            document.Add(summaryTable.SetMarginBottom(20));
        }

        private void AddPOBreakdownSection(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, DeviceRgb accentColor, DeviceRgb lightGray)
        {
            document.Add(new Paragraph($"PO DETAILS BREAKDOWN ({qrData.POSummaries.Count} POs)")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetMarginBottom(10));

            var poTable = new Table(7);
            poTable.SetWidth(UnitValue.CreatePercentValue(100));
            poTable.SetFontSize(9);

            // Headers
            poTable.AddHeaderCell(CreateStyledHeaderCell("PO NUMBER", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("MODEL", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("PALLETS", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("BOXES", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("PIECES", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("SCANNED", headerFont, lightGray));
            poTable.AddHeaderCell(CreateStyledHeaderCell("PROGRESS", headerFont, lightGray));

            // Data rows
            foreach (var po in qrData.POSummaries)
            {
                poTable.AddCell(CreateStyledDataCell(po.PONumber, bodyFont));
                poTable.AddCell(CreateStyledDataCell(po.ModelProduct, bodyFont));
                poTable.AddCell(CreateStyledDataCell(po.QtyPallet.ToString(), bodyFont));
                poTable.AddCell(CreateStyledDataCell(po.QtyBox.ToString(), bodyFont));
                poTable.AddCell(CreateStyledDataCell(po.QtyPcs.ToString("N0"), bodyFont));
                poTable.AddCell(CreateStyledDataCell($"{po.ScannedCount}/{po.QtyBox}", bodyFont));
                poTable.AddCell(CreateStyledDataCell($"{po.ScannedPercentage:F1}%", bodyFont));
            }

            document.Add(poTable.SetMarginBottom(20));
        }

        private void AddBarcodeListSection(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, PdfFont monospaceFont, DeviceRgb accentColor, int startIndex = 0)
        {
            if (qrData.BarcodeList?.Any() != true) return;

            // Removed BOX BARCODES title to save space (barcode pages start from page 2)

            // Process barcodes in groups of 4 per page (2x2 grid)
            for (int pageStart = startIndex; pageStart < qrData.BarcodeList.Count; pageStart += 4)
            {
                // Add new page if not the first barcode page
                if (pageStart > startIndex)
                {
                    document.Add(new AreaBreak());
                    
                    // Add page header for continuation pages (removed title)
                    document.Add(new Paragraph($"BOX BARCODES (continued) - Page {(pageStart / 4) + 1}")
                        .SetFont(headerFont)
                        .SetFontSize(14)
                        .SetFontColor(accentColor)
                        .SetMarginBottom(10)); // Reduced margin for continuation pages
                }

                // Create table with 2 columns (2x2 grid)
                var barcodeTable = new Table(2);
                barcodeTable.SetWidth(UnitValue.CreatePercentValue(100));
                barcodeTable.SetKeepTogether(true);

                // Get items for this page (max 4)
                var pageItems = qrData.BarcodeList.Skip(pageStart).Take(4).ToList();

                for (int i = 0; i < pageItems.Count; i++)
                {
                    var barcode = pageItems[i];

                    // Extract display text (everything after the last underscore before _BOX_)
                    string displayText = ExtractBarcodeDisplayText(barcode);

                    // Create cell for QR + Text (optimized for page 1 fitting)
                    var barcodeCell = new Cell();
                    barcodeCell.SetBorder(new SolidBorder(ColorConstants.BLACK, 1f)); // Black border like design
                    barcodeCell.SetPadding(12); // Compact padding
                    barcodeCell.SetTextAlignment(TextAlignment.CENTER);
                    barcodeCell.SetVerticalAlignment(VerticalAlignment.TOP);
                    barcodeCell.SetMinHeight(230); // Unified height for consistent 2x2 layout
                    barcodeCell.SetKeepTogether(true);

                    try
                    {
                        // Generate QR code with FULL barcode value
                        var qrGenerator = new QRCodeGenerator();
                        var qrCodeData = qrGenerator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                        var qrCode = new BitmapByteQRCode(qrCodeData);
                        var qrBytes = qrCode.GetGraphic(12); // Good size for readability

                        if (qrBytes != null && qrBytes.Length > 0)
                        {
                            var qrImage = ImageDataFactory.Create(qrBytes);
                            
                            // Add QR Image (optimized size for page fitting)
                            barcodeCell.Add(new Image(qrImage)
                                .SetAutoScale(true)
                                .SetWidth(130) // Slightly larger for clarity on dedicated pages
                                .SetHeight(130)
                                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                .SetMarginBottom(6)); // Compact spacing
                            
                        }
                        else
                        {
                            throw new Exception("QR bytes is null or empty");
                        }
                    }
                    catch
                    {
                        
                        // Fallback: show error placeholder
                        barcodeCell.Add(new Paragraph("[QR ERROR]")
                            .SetFont(bodyFont)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontColor(ColorConstants.RED)
                            .SetBorder(new SolidBorder(ColorConstants.RED, 1f))
                            .SetPadding(10)
                            .SetMarginBottom(6)); // Compact spacing
                    }

                    // Add barcode value below QR (optimized for page fitting)
                    barcodeCell.Add(new Paragraph(displayText)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                        .SetFontSize(10) // Slightly smaller font to fit better
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(2)); // Minimal margin

                    barcodeTable.AddCell(barcodeCell);
                }

                // Fill empty cells if less than 4 items on last page
                int emptyCellsNeeded = 4 - pageItems.Count;
                for (int j = 0; j < emptyCellsNeeded; j++)
                {
                    var emptyCell = new Cell();
                    emptyCell.SetBorder(Border.NO_BORDER);
                    emptyCell.SetMinHeight(230); // Match barcode cell height
                    barcodeTable.AddCell(emptyCell);
                }

                document.Add(barcodeTable.SetMarginBottom(10));
            }
        }

        // Add first row (2 barcode boxes) on Page 1 below QR identity & PO details
        private void AddFirstBarcodeRowIfAny(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, PdfFont monospaceFont)
        {
            if (qrData.BarcodeList == null || qrData.BarcodeList.Count == 0) return;

            var firstRowTable = new Table(2);
            firstRowTable.SetWidth(UnitValue.CreatePercentValue(100));
            firstRowTable.SetKeepTogether(true);

            int cells = Math.Min(2, qrData.BarcodeList.Count);
            for (int i = 0; i < 2; i++)
            {
                if (i < cells)
                {
                    string barcode = qrData.BarcodeList[i];
                    string displayText = ExtractBarcodeDisplayText(barcode);

                    var cell = new Cell();
                    cell.SetBorder(new SolidBorder(ColorConstants.BLACK, 1f));
                    cell.SetPadding(10);
                    cell.SetTextAlignment(TextAlignment.CENTER);
                    cell.SetVerticalAlignment(VerticalAlignment.TOP);
                    cell.SetMinHeight(200); // Slightly shorter to fit on first page
                    cell.SetKeepTogether(true);

                    try
                    {
                        var qrGenerator = new QRCodeGenerator();
                        var qrCodeData = qrGenerator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                        var qrCode = new BitmapByteQRCode(qrCodeData);
                        var qrBytes = qrCode.GetGraphic(12);
                        if (qrBytes != null && qrBytes.Length > 0)
                        {
                            var qrImage = ImageDataFactory.Create(qrBytes);
                            cell.Add(new Image(qrImage)
                                .SetAutoScale(true)
                                .SetWidth(120)
                                .SetHeight(120)
                                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                .SetMarginBottom(6));
                        }
                        else
                        {
                            throw new Exception("QR bytes missing");
                        }
                    }
                    catch
                    {
                        cell.Add(new Paragraph("[QR ERROR]")
                            .SetFont(bodyFont)
                            .SetFontSize(10)
                            .SetFontColor(ColorConstants.RED)
                            .SetMarginBottom(6));
                    }

                    cell.Add(new Paragraph(displayText)
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(2));

                    firstRowTable.AddCell(cell);
                }
                else
                {
                    var empty = new Cell();
                    empty.SetBorder(new SolidBorder(ColorConstants.BLACK, 1f));
                    empty.SetMinHeight(200);
                    firstRowTable.AddCell(empty);
                }
            }

            document.Add(firstRowTable.SetMarginBottom(10));
        }

        // Helper method to extract display text from barcode
        private string ExtractBarcodeDisplayText(string fullBarcode)
        {
            if (string.IsNullOrEmpty(fullBarcode)) return "N/A";

            // Find BOX pattern and extract the meaningful part
            // Example: QR_3_20250825115813_BOX_RP-2400DBG-K_001 -> BOX_RP-2400DBG-K_001
            var boxIndex = fullBarcode.IndexOf("_BOX_", StringComparison.OrdinalIgnoreCase);
            if (boxIndex >= 0)
            {
                // Return from BOX onwards
                return fullBarcode.Substring(boxIndex + 1); // Skip the first underscore
            }

            // Fallback: if no BOX pattern, try to find last meaningful part
            var parts = fullBarcode.Split('_');
            if (parts.Length >= 3)
            {
                // Return last 2-3 parts joined
                return string.Join("_", parts.TakeLast(Math.Min(3, parts.Length)));
            }

            // Final fallback: return as is
            return fullBarcode;
        }

        private void AddFooter(Document document, PdfFont bodyFont, DeviceRgb primaryColor)
        {
            document.Add(new Paragraph()
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 1))
                .SetMarginTop(30)
                .SetMarginBottom(10));

            document.Add(new Paragraph($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Shipment Finish Good System")
                .SetFont(bodyFont)
                .SetFontSize(8)
                .SetFontColor(primaryColor)
                .SetTextAlignment(TextAlignment.CENTER));
        }

        private void AddCoverPage(Document document, QRManagementDto qrData,
            PdfFont titleFont, PdfFont headerFont, PdfFont bodyFont)
        {
            // Implementation for comprehensive report cover page
            document.Add(new Paragraph("COMPREHENSIVE SHIPMENT REPORT")
                .SetFont(titleFont)
                .SetFontSize(24)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(100)
                .SetMarginBottom(50));

            // Add more cover page content...
        }

        private void AddDetailedReport(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont)
        {
            // Implementation for detailed report sections
            // Add scanning history, detailed analytics, etc.
        }

        // Helper methods for consistent cell styling
        private Cell CreateInfoCell(string content, PdfFont font)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(font)
                .SetFontSize(10))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(5);
        }

        private Cell CreateHeaderCell(string content, PdfFont font)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(font)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetBorder(new SolidBorder(1))
                .SetPadding(8);
        }

        private Cell CreateSummaryCell(string content, PdfFont font)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(12)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new SolidBorder(1))
                .SetPadding(8);
        }

        private Cell CreateStyledHeaderCell(string content, PdfFont font, DeviceRgb backgroundColor)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBackgroundColor(backgroundColor)
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(6);
        }

        private Cell CreateStyledDataCell(string content, PdfFont font)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(font)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
                .SetPadding(4);
        }

        // Compact header cell for PO details (smaller font & padding)
        private Cell CreateCompactHeaderCell(string content, PdfFont font, DeviceRgb backgroundColor)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBackgroundColor(backgroundColor)
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(3);
        }

        // Compact data cell for PO details
        private Cell CreateCompactDataCell(string content, PdfFont font)
        {
            return new Cell().Add(new Paragraph(content)
                .SetFont(font)
                .SetFontSize(7)
                .SetTextAlignment(TextAlignment.CENTER))
                .SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
                .SetPadding(2);
        }
    }
}
