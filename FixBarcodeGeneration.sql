-- Script untuk memperbaiki dan menganalisis masalah Barcode Generation
-- SessionId 2 Analysis

-- 1. Backup data current (untuk safety)
SELECT * INTO POMasters_Backup_SessionId2 FROM POMasters WHERE SourceSessionId = 2;
SELECT * INTO BarcodeRegistries_Backup_SessionId2 FROM BarcodeRegistries WHERE SessionId = 2;

-- 2. Analisis masalah current
SELECT 
    'Current Analysis' as Status,
    p.POId,
    p.NoPO,
    p.ModelProduk,
    p.QtyBox as DatabaseQtyBox,
    p.QtyTotal,
    COUNT(b.BarcodeId) as GeneratedBarcodes,
    (p.QtyBox - COUNT(b.BarcodeId)) as Missing
FROM POMasters p
LEFT JOIN BarcodeRegistries b ON b.POId = p.POId AND b.BarcodeType = 'BOX'
WHERE p.SourceSessionId = 2
GROUP BY p.POId, p.NoPO, p.ModelProduk, p.QtyBox, p.QtyTotal
ORDER BY p.POId;

-- 3. Data yang seharusnya (berdasarkan data user)
WITH CorrectData AS (
    SELECT 1 as POId, '4502196133' as NoPO, 'RF-2400DEG-K' as Model, 59 as CorrectQtyBox, 393 as QtyTotal UNION ALL
    SELECT 2, '4502196134', 'RF-2400DEG-K', 5, 15 UNION ALL
    SELECT 3, '4502207470', 'RF-2400DEG-K', 15, 10197 UNION ALL
    SELECT 4, '4502207470', 'RF-D10EG-K', 36, 252 UNION ALL
    SELECT 5, '4502207470', 'RF-P150DEG-S', 58, 7560 UNION ALL
    SELECT 6, '4502207470', 'RF-P50DEG-S', 42, 840 UNION ALL
    SELECT 7, '4502207472', 'RF-2400DEG-K', 12, 684 UNION ALL
    SELECT 8, '4502207472', 'RF-D10EG-K', 15, 45 UNION ALL
    SELECT 9, '4502207472', 'RF-D10EG-W', 1, 3 UNION ALL
    SELECT 10, '4502207472', 'RF-P150DEG-S', 27, 540
)
SELECT 
    'Should Be Analysis' as Status,
    c.POId,
    c.NoPO,
    c.Model,
    c.CorrectQtyBox as ShouldBeQtyBox,
    p.QtyBox as CurrentDatabaseQtyBox,
    (p.QtyBox - c.CorrectQtyBox) as Difference,
    COUNT(b.BarcodeId) as CurrentGenerated,
    (c.CorrectQtyBox - COUNT(b.BarcodeId)) as StillNeedToGenerate
FROM CorrectData c
LEFT JOIN POMasters p ON p.POId = c.POId
LEFT JOIN BarcodeRegistries b ON b.POId = c.POId AND b.BarcodeType = 'BOX'
GROUP BY c.POId, c.NoPO, c.Model, c.CorrectQtyBox, p.QtyBox
ORDER BY c.POId;

-- 4. Summary
SELECT 
    'SUMMARY' as Analysis,
    SUM(270) as TotalShouldBe,
    SUM(p.QtyBox) as TotalInDatabase,
    COUNT(b.BarcodeId) as TotalGenerated,
    (270 - COUNT(b.BarcodeId)) as StillMissing
FROM POMasters p
LEFT JOIN BarcodeRegistries b ON b.POId = p.POId AND b.BarcodeType = 'BOX'
WHERE p.SourceSessionId = 2;
