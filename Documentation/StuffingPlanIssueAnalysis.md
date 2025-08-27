# Issue Analysis & Solution - Stuffing Plan Parsing

## Issues Found

### 1. **Wrong Country Detection**
- **Problem**: Countries showing as USA, Bucharest instead of NORTHAMPTON, PIRAEUS, etc.
- **Root Cause**: System potentially using wrong parser (Single Sheet format instead of Stuffing Plan format)
- **Expected**: Country should come from sheet name (NORTHAMPTON, PIRAEUS, GENOA, VALENCIA, ERFURT 1, ERFURT 2)

### 2. **PO Number and Model Columns Swapped**
- **Problem**: Data shows PO numbers in Model column and vice versa
- **Root Cause**: Incorrect column detection in FindColumnIndexes method
- **Expected**: NO PO in first column, NO MODEL in second column

### 3. **LCL Treated as PO Number**
- **Problem**: "LCL" was included in format detection indicators
- **Root Cause**: LCL is shipment type indicator, NOT a PO number
- **Fixed**: Removed "LCL" from stuffing plan indicators

## Solutions Implemented

### 1. **Enhanced Format Detection**
```csharp
// Added better detection logic with debugging
private bool IsStuffingPlanFormat(IXLWorkbook workbook)
{
    // Look for specific stuffing plan indicators
    var stuffingPlanIndicators = new[]
    {
        "STUFFING PLAN", "Export Department", "Shipment to", 
        "N.W (Kg)", "G.W (Kg)", "Volume (M3)", "Ready :"
        // Removed "LCL" - it's shipment type, not PO indicator
    };
    
    var dataHeaders = new[] { "NO PO", "NO MODEL", "QTY" };
    // Must find at least 2 of 3 required headers OR stuffing plan indicators
}
```

### 2. **Improved Column Detection**  
```csharp
private (int poCol, int modelCol, int qtyCol) FindColumnIndexes(IXLWorksheet worksheet, int headerRow)
{
    // First try exact matches for headers
    // Then try partial matches
    // Add debugging output to see what's found
    // Proper fallback with warnings
}
```

### 3. **Better Header Row Detection**
```csharp
private int FindDataStartRow(IXLWorksheet worksheet)
{
    // Look for rows containing at least 2 of the 3 required headers
    // Removed "LCL" from header indicators
    var headerIndicators = new[] { "NO PO", "NO MODEL", "QTY" };
}
```

### 4. **Added Debug Output**
- Format detection logging
- Column mapping logging  
- Sheet name logging
- Header detection logging

## Expected Data Flow (Fixed)

### Input (Stuffing Plan):
```
Sheet: NORTHAMPTON
NO PO        NO MODEL      QTY
4502195906   RF-2400DEB-K  1.149
             RF-D10EB-K    63  
4502195907   RF-2400DEB-K  129
```

### Output (Should be):
```
Row 1: PO=4502195906, Model=RF-2400DEB-K, Qty=1149, Country=NORTHAMPTON
Row 2: PO=4502195906, Model=RF-D10EB-K,   Qty=63,   Country=NORTHAMPTON  
Row 3: PO=4502195907, Model=RF-2400DEB-K, Qty=129,  Country=NORTHAMPTON
```

## Testing Instructions

1. **Upload the stuffing plan Excel file**
2. **Check console output** for debugging information:
   - Format detection results
   - Sheet names found
   - Column mappings detected
   - Header row detection

3. **Verify results**:
   - Country should match sheet names (NORTHAMPTON, PIRAEUS, etc.)
   - PO numbers should be in correct column (4502195906, 4502195907)
   - Models should be in correct column (RF-2400DEB-K, RF-D10EB-K)
   - Quantities should be parsed correctly (1149, 63, 129)

## Debug Commands

To see debug output when testing:
```bash
dotnet run
# Upload file and watch console for debug messages
```

The debugging will show:
- `=== FORMAT DETECTION START ===`
- `DETECTED: STUFFING_PLAN`
- `Column 1: 'NO PO'`
- `Column 2: 'NO MODEL'` 
- `Column 3: 'QTY'`
- `Final column mapping: PO=1, MODEL=2, QTY=3`
