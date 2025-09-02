-- Debug analysis untuk SessionId 2
-- Cek data POMaster
SELECT 
    POId,
    NoPO,
    ModelProduk,
    QtyTotal,
    QtyPallet,
    QtyBox,
    QtyPcs,
    SourceSessionId
FROM POMasters 
WHERE SourceSessionId = 2
ORDER BY POId;

-- Hitung total Box yang seharusnya di-generate
SELECT 
    SUM(QtyBox) as TotalBoxesExpected,
    COUNT(*) as TotalPORecords
FROM POMasters 
WHERE SourceSessionId = 2;

-- Cek barcode yang sudah di-generate
SELECT 
    COUNT(*) as TotalBarcodesGenerated,
    SUM(CASE WHEN BarcodeType = 'MASTER' THEN 1 ELSE 0 END) as MasterBarcodes,
    SUM(CASE WHEN BarcodeType = 'BOX' THEN 1 ELSE 0 END) as BoxBarcodes
FROM BarcodeRegistries 
WHERE SessionId = 2 AND IsActive = 1;

-- Detail barcode per model
SELECT 
    ModelProduct,
    COUNT(*) as BarcodeCount,
    MIN(BoxNumber) as MinBoxNumber,
    MAX(BoxNumber) as MaxBoxNumber
FROM BarcodeRegistries 
WHERE SessionId = 2 AND BarcodeType = 'BOX' AND IsActive = 1
GROUP BY ModelProduct
ORDER BY ModelProduct;

-- Cek POMaster detail per model untuk comparison
SELECT 
    ModelProduk,
    SUM(QtyBox) as TotalBoxesPerModel,
    COUNT(*) as RecordsPerModel
FROM POMasters 
WHERE SourceSessionId = 2
GROUP BY ModelProduk
ORDER BY ModelProduk;
