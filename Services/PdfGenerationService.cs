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

            // QR Identity Section
            AddQRIdentitySection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor);

            // Summary Statistics
            AddSummaryStatistics(document, qrData, headerFont, bodyFont, accentColor, lightGray);

            // PO Details Breakdown
            if (qrData.POSummaries?.Any() == true)
            {
                AddPOBreakdownSection(document, qrData, headerFont, bodyFont, accentColor, lightGray);
            }

            // Barcode List Section - Formatted in columns for A4
            AddBarcodeListSection(document, qrData, headerFont, bodyFont, monospaceFont, accentColor);

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
                // Debug logging for QR Identity PDF
                Console.WriteLine($"[QR Identity PDF DEBUG] QRIdentity: '{qrData.QRIdentity}'");
                Console.WriteLine($"[QR Identity PDF DEBUG] QRImageBase64 length: {qrData.QRImageBase64?.Length ?? 0}");

                byte[]? qrBytes = null;

                // Try existing QR first
                if (!string.IsNullOrEmpty(qrData.QRImageBase64))
                {
                    try
                    {
                        qrBytes = Convert.FromBase64String(qrData.QRImageBase64);
                        Console.WriteLine($"[QR Identity PDF DEBUG] Using existing QR: {qrBytes.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[QR Identity PDF DEBUG] Failed to decode existing QR: {ex.Message}");
                    }
                }

                // Generate new QR if needed
                if (qrBytes == null && !string.IsNullOrEmpty(qrData.QRIdentity))
                {
                    var qrGenerator = new QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(qrData.QRIdentity, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new BitmapByteQRCode(qrCodeData);
                    qrBytes = qrCode.GetGraphic(20);
                    Console.WriteLine($"[QR Identity PDF DEBUG] Generated new QR: {qrBytes.Length} bytes");
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
                    Console.WriteLine($"[QR Identity PDF DEBUG] QR image added successfully");
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
                    Console.WriteLine($"[QR Identity PDF DEBUG] No QR available, showing fallback");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QR Identity PDF DEBUG] Error generating QR: {ex.Message}");
                
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

        private void AddQRIdentitySection(Document document, QRManagementDto qrData,
            PdfFont headerFont, PdfFont bodyFont, PdfFont monospaceFont, DeviceRgb accentColor)
        {
            document.Add(new Paragraph("QR IDENTITY INFORMATION")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetMarginBottom(10));

            try
            {
                // Debug logging
                Console.WriteLine($"[PDF DEBUG] QRIdentity: '{qrData.QRIdentity}'");
                Console.WriteLine($"[PDF DEBUG] QRImageBase64 length: {qrData.QRImageBase64?.Length ?? 0}");

                byte[]? qrBytes = null;

                // Try to use existing QR image first
                if (!string.IsNullOrEmpty(qrData.QRImageBase64))
                {
                    try
                    {
                        qrBytes = Convert.FromBase64String(qrData.QRImageBase64);
                        Console.WriteLine($"[PDF DEBUG] Using existing QR image: {qrBytes.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PDF DEBUG] Failed to decode existing QR: {ex.Message}");
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
                        qrBytes = qrCode.GetGraphic(15);
                        Console.WriteLine($"[PDF DEBUG] Generated new QR: {qrBytes.Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PDF DEBUG] Failed to generate QR: {ex.Message}");
                        qrBytes = null;
                    }
                }

                var identityTable = new Table(2);
                identityTable.SetWidth(UnitValue.CreatePercentValue(100));

                // QR Code cell
                var qrCell = new Cell();
                if (qrBytes != null)
                {
                    try
                    {
                        var qrImage = ImageDataFactory.Create(qrBytes);
                        qrCell.Add(new Image(qrImage)
                            .SetAutoScale(true)
                            .SetMaxWidth(120)
                            .SetMaxHeight(120));
                        Console.WriteLine($"[PDF DEBUG] QR image added successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PDF DEBUG] Failed to create image: {ex.Message}");
                        qrCell.Add(new Paragraph("[QR IMAGE ERROR]")
                            .SetFont(bodyFont)
                            .SetFontSize(10));
                    }
                }
                else
                {
                    // Fallback: show text instead of QR
                    qrCell.Add(new Paragraph("[NO QR IMAGE]")
                        .SetFont(bodyFont)
                        .SetFontSize(10));
                    Console.WriteLine($"[PDF DEBUG] No QR bytes available, showing fallback text");
                }

                qrCell.SetTextAlignment(TextAlignment.CENTER);
                qrCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                identityTable.AddCell(qrCell);

                // Info cell
                var infoCell = new Cell();
                infoCell.Add(new Paragraph("QR Identity:")
                    .SetFont(bodyFont)
                    .SetFontSize(10)
                    .SetMarginBottom(5));
                infoCell.Add(new Paragraph(qrData.QRIdentity ?? "[NO QR IDENTITY]")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                    .SetFontSize(12)
                    .SetMarginBottom(10));
                
                infoCell.Add(new Paragraph($"Generated: {qrData.GeneratedDate:dd/MM/yyyy HH:mm:ss}")
                    .SetFont(bodyFont)
                    .SetFontSize(9));
                infoCell.Add(new Paragraph($"By: {qrData.GeneratedBy}")
                    .SetFont(bodyFont)
                    .SetFontSize(9));
                infoCell.Add(new Paragraph($"File: {qrData.FileName}")
                    .SetFont(bodyFont)
                    .SetFontSize(9));

                identityTable.AddCell(infoCell);
                document.Add(identityTable.SetMarginBottom(20));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PDF DEBUG] Error in AddQRIdentitySection: {ex.Message}");
                // Add error message to PDF
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
            PdfFont headerFont, PdfFont bodyFont, PdfFont monospaceFont, DeviceRgb accentColor)
        {
            if (qrData.BarcodeList?.Any() != true) return;

            // Start barcode list on new page if needed
            if (document.GetRenderer().GetCurrentArea().GetBBox().GetHeight() < 200)
            {
                document.Add(new AreaBreak());
            }

            document.Add(new Paragraph($"BOX BARCODES ({qrData.BarcodeList.Count} items)")
                .SetFont(headerFont)
                .SetFontSize(14)
                .SetFontColor(accentColor)
                .SetMarginBottom(15));

            // Create table with 3 columns: Number, Barcode, QR Code
            var barcodeTable = new Table(new float[] { 1, 4, 2 }); // Column widths
            barcodeTable.SetWidth(UnitValue.CreatePercentValue(100));

            // Add table headers
            barcodeTable.AddHeaderCell(CreateStyledHeaderCell("No.", headerFont, new DeviceRgb(248, 249, 250)));
            barcodeTable.AddHeaderCell(CreateStyledHeaderCell("Barcode", headerFont, new DeviceRgb(248, 249, 250)));
            barcodeTable.AddHeaderCell(CreateStyledHeaderCell("QR Code", headerFont, new DeviceRgb(248, 249, 250)));

            for (int i = 0; i < qrData.BarcodeList.Count; i++)
            {
                var barcode = qrData.BarcodeList[i];

                // Number cell
                var numberCell = new Cell();
                numberCell.Add(new Paragraph($"{i + 1}.")
                    .SetFont(bodyFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER));
                numberCell.SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f));
                numberCell.SetPadding(8);
                numberCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                barcodeTable.AddCell(numberCell);

                // Barcode cell
                var barcodeCell = new Cell();
                barcodeCell.Add(new Paragraph(barcode)
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD))
                    .SetFontSize(9));
                barcodeCell.SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f));
                barcodeCell.SetPadding(8);
                barcodeCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                barcodeTable.AddCell(barcodeCell);

                // QR Code cell
                var qrCell = new Cell();
                try
                {
                    // Generate QR code for this specific barcode
                    var qrGenerator = new QRCodeGenerator();
                    var qrCodeData = qrGenerator.CreateQrCode(barcode, QRCodeGenerator.ECCLevel.Q);
                    var qrCode = new BitmapByteQRCode(qrCodeData);
                    var qrBytes = qrCode.GetGraphic(8); // Smaller QR for list

                    var qrImage = ImageDataFactory.Create(qrBytes);
                    qrCell.Add(new Image(qrImage)
                        .SetAutoScale(true)
                        .SetMaxWidth(60)
                        .SetMaxHeight(60)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER));
                    
                    Console.WriteLine($"[PDF DEBUG] Generated QR for barcode: {barcode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PDF DEBUG] Failed to generate QR for {barcode}: {ex.Message}");
                    // Fallback: show placeholder text
                    qrCell.Add(new Paragraph("[QR]")
                        .SetFont(bodyFont)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER));
                }

                qrCell.SetBorder(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f));
                qrCell.SetPadding(8);
                qrCell.SetTextAlignment(TextAlignment.CENTER);
                qrCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                barcodeTable.AddCell(qrCell);
            }

            document.Add(barcodeTable);
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
    }
}
