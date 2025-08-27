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

            

            // QR Identity + PO Details Combined Section
            AddQRIdentityWithPODetailsSection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor, lightGray);

            // Add first row (2 barcode boxes) on Page 1 if any barcodes
            AddFirstBarcodeRowIfAny(document, qrData, headerFont, bodyFont, monospaceFont);

            // Remaining barcodes continue without page break if more than 2
            if (qrData.BarcodeList != null && qrData.BarcodeList.Count > 2)
            {
                // Force new page only for barcode continuation
                document.Add(new AreaBreak());
                AddBarcodeListSection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor, startIndex: 2);
            }

            // Footer - hanya jika masih ada ruang di halaman terakhir
            // AddFooter(document, bodyFont, primaryColor); // Commented out to prevent extra pages

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

            // Improved: show Destination Country and Source File as separate centered paragraphs
            if (!string.IsNullOrEmpty(qrData.Country))
            {
                document.Add(new Paragraph()
                    .Add(new Text("Destination Country: ").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                    .Add(new Text(qrData.Country).SetFont(bodyFont))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetMarginBottom(6));
            }

            if (!string.IsNullOrEmpty(qrData.FileName))
            {
                document.Add(new Paragraph()
                    .Add(new Text("Source File: ").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                    .Add(new Text(qrData.FileName).SetFont(bodyFont))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(11)
                    .SetMarginBottom(14));
            }

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
                        // Add country and source file under QR image for clarity
                        if (!string.IsNullOrEmpty(qrData.Country))
                        {
                            qrCell.Add(new Paragraph()
                                .Add(new Text("Destination Country: ").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                                .Add(new Text(qrData.Country).SetFont(bodyFont))
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetFontSize(11)
                                .SetMarginBottom(4));
                        }

                        if (!string.IsNullOrEmpty(qrData.FileName))
                        {
                            qrCell.Add(new Paragraph()
                                .Add(new Text("Source File: ").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)))
                                .Add(new Text(qrData.FileName).SetFont(bodyFont))
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetFontSize(10)
                                .SetMarginBottom(4));
                        }
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

                // Shipment Date display (if present)
                if (qrData.ShipmentDate.HasValue)
                {
                    poCell.Add(new Paragraph($"Shipment Date: {qrData.ShipmentDate.Value:dd/MM/yyyy}")
                        .SetFont(bodyFont)
                        .SetFontSize(10)
                        .SetFontColor(ColorConstants.DARK_GRAY)
                        .SetMarginBottom(8));
                }

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

            // Process barcodes in groups of 4 per page (2x2 grid). Rely on natural page flow (no manual AreaBreak here)
            for (int pageStart = startIndex; pageStart < qrData.BarcodeList.Count; pageStart += 4)
            {

                // Compute target quadrant dimensions (4 per page)
                var pageSize = document.GetPdfDocument().GetDefaultPageSize();
                float contentHeight = pageSize.GetHeight() - document.GetTopMargin() - document.GetBottomMargin();
                float contentWidth = pageSize.GetWidth() - document.GetLeftMargin() - document.GetRightMargin();
                // Reduce a bit so 2 rows always muat dalam 1 halaman (hindari pecah jadi 2 halaman @2 barcode)
                float targetCellHeight = (contentHeight / 2f) - 28f; // empiris: margin untuk padding & text
                float halfWidth = contentWidth / 2f;

                // Create 2x2 table with FIXED proportions for perfect quadrants
                var barcodeTable = new Table(new float[] { 50f, 50f }); // Equal column widths
                barcodeTable.SetWidth(UnitValue.CreatePercentValue(100));
                barcodeTable.SetMarginTop(0).SetMarginBottom(0);
                barcodeTable.SetKeepTogether(true); // keep whole 2x2 grid on one page

                // ALWAYS process exactly 4 cells per page (fill empty if needed)
                for (int cellIndex = 0; cellIndex < 4; cellIndex++)
                {
                    var barcodeCell = new Cell();
                    barcodeCell.SetBorder(Border.NO_BORDER);
                    barcodeCell.SetPadding(5);
                    barcodeCell.SetTextAlignment(TextAlignment.CENTER);
                    barcodeCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                    barcodeCell.SetHeight(targetCellHeight); // fixed height yang sudah dikurangi supaya 2 row pas
                    barcodeCell.SetKeepTogether(true);
                    
                    // Check if we have a barcode for this position
                    int barcodeIndex = pageStart + cellIndex;
                    if (barcodeIndex < qrData.BarcodeList.Count)
                    {
                        var barcode = qrData.BarcodeList[barcodeIndex];
                        string displayText = ExtractBarcodeDisplayText(barcode);

                        try
                        {
                            // Generate QR code with FULL barcode value
                            var qrGenerator = new QRCodeGenerator();
                            var qrCodeData = qrGenerator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                            var qrCode = new BitmapByteQRCode(qrCodeData);
                            var qrBytes = qrCode.GetGraphic(15); // Increased for better readability

                            if (qrBytes != null && qrBytes.Length > 0)
                            {
                                var qrImage = ImageDataFactory.Create(qrBytes);
                                
                                // Maximize QR size within quadrant - FULL utilization
                                // QR size maksimal dalam cell (sisakan ruang text)
                                float qrSize = Math.Min(targetCellHeight - 60, halfWidth - 30);
                                if (qrSize < 140) qrSize = 140; // minimum readability
                                if (qrSize > 300) qrSize = 300; // Cap for readability
                                if (qrSize < 180) qrSize = 180; // Ensure good size
                                
                                barcodeCell.Add(new Image(qrImage)
                                    .SetWidth(qrSize)
                                    .SetHeight(qrSize)
                                    .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                    .SetMarginBottom(5));
                            }
                            else
                            {
                                throw new Exception("QR generation failed");
                            }
                        }
                        catch
                        {
                            // Fallback for QR generation errors
                            barcodeCell.Add(new Paragraph("[QR ERROR]")
                                .SetFont(bodyFont)
                                .SetFontSize(14)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetFontColor(ColorConstants.RED)
                                .SetMarginBottom(5));
                        }

                        // Add text below QR
                        var barcodeTextPara = new Paragraph(displayText)
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                            .SetFontSize(11)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(2);
                        barcodeCell.Add(barcodeTextPara);
                        // Country label under each barcode
                        if (!string.IsNullOrEmpty(qrData.Country))
                        {
                            barcodeCell.Add(new Paragraph(qrData.Country)
                                .SetFont(bodyFont)
                                .SetFontSize(9)
                                .SetFontColor(ColorConstants.GRAY)
                                .SetTextAlignment(TextAlignment.CENTER));
                        }
                    }
                    // Empty cell still takes up exact quadrant space
                    
                    barcodeTable.AddCell(barcodeCell);
                }

                document.Add(barcodeTable);
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
                    // Remove border per user request
                    cell.SetBorder(Border.NO_BORDER);
                    cell.SetPadding(10);
                    cell.SetTextAlignment(TextAlignment.CENTER);
                    cell.SetVerticalAlignment(VerticalAlignment.TOP);
                    cell.SetMinHeight(250); // Increase to better balance with upper section while maximizing area
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
                                .SetWidth(150)
                                .SetHeight(150)
                                .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                                .SetMarginBottom(8));
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
                        .SetFontSize(11)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(4));
                    if (!string.IsNullOrEmpty(qrData.Country))
                    {
                        cell.Add(new Paragraph(qrData.Country)
                            .SetFont(bodyFont)
                            .SetFontSize(9)
                            .SetFontColor(ColorConstants.GRAY)
                            .SetTextAlignment(TextAlignment.CENTER));
                    }

                    firstRowTable.AddCell(cell);
                }
                else
                {
                    var empty = new Cell();
                    empty.SetBorder(Border.NO_BORDER);
                    empty.SetMinHeight(250);
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
            // Compact footer to avoid forcing new pages
            document.Add(new Paragraph($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Shipment Finish Good System")
                .SetFont(bodyFont)
                .SetFontSize(8)
                .SetFontColor(primaryColor)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10) // Reduced margin
                .SetMarginBottom(5)); // Reduced margin
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
