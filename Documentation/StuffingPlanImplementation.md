# Stuffing Plan Format Implementation

## Overview
Implementation of the 4 key data extraction elements from Stuffing Plan Excel files as requested.

## Key Requirements Implemented

### 1. NO PO (Purchase Order) - Inheritance Rule
- **Examples**: 4502195906, 4502195907
- **Rule**: If cell is empty → inherit from row above
- **Implementation**: Uses `currentPO` variable to track and inherit values

```csharp
// NO PO: Inheritance rule - if empty, inherit from row above
if (!string.IsNullOrEmpty(noPOCell))
    currentPO = noPOCell;
```

### 2. NO MODEL (Product Model) - Inheritance Rule
- **Examples**: RF-2000DEB-K, RF-D10EB-K, RF-2400DEB-K  
- **Rule**: If cell is empty → inherit from row above
- **Implementation**: Uses `currentModel` variable to track and inherit values

```csharp
// NO MODEL: Inheritance rule - if empty, inherit from row above
if (!string.IsNullOrEmpty(modelCell))
    currentModel = modelCell;
```

### 3. QTY (Quantity) - Always Has Unique Value
- **Examples**: 1.149 Sets, 383 SP, 63 Sets, 129 Sets
- **Rule**: Always has unique value per row, never inherited
- **Implementation**: Enhanced parsing for various quantity formats

```csharp
// QTY: Always has unique value per row
if (!ParseQuantity(qtyCell, out int qty))
    continue;
```

**Quantity Parsing Enhancement**:
- Handles "Sets" suffix removal
- Handles "SP" suffix removal  
- Handles decimal notation (1.149 → 1149)
- Handles comma notation (1,149 → 1149)

### 4. COUNTRY (Destination) - From Sheet Name
- **Source**: Sheet name mapping
- **Examples**: NORTHAMPTON, PIRAEUS, GENOA, VALENCIA, ERFURT 1, ERFURT 2
- **Rule**: Sheet name = Country destination
- **Implementation**: Extracts country from worksheet name

```csharp
// COUNTRY: Extract from sheet name
var countryFromSheetName = CountryNormalizer.Normalize(worksheet.Name);
```

## Column Detection Enhancement

Enhanced column detection to precisely identify the 3 key columns:
- **NO PO**: Usually column 1, detected by "NO PO" header
- **NO MODEL**: Usually column 2, detected by "NO MODEL" header  
- **QTY**: Usually column 3, detected by "QTY" header

## Data Validation

Only adds valid rows that have:
1. Valid NO PO (current or inherited)
2. Valid NO MODEL (current or inherited) 
3. Valid QTY (must be > 0)
4. Valid COUNTRY (from sheet name)

## Example Data Flow

Input from Stuffing Plan:
```
Sheet: NORTHAMPTON
NO PO        NO MODEL      QTY
4502195906   RF-2000DEB-K  1.149 Sets
             RF-D10EB-K    383 SP  
             RF-2400DEB-K  63 Sets
4502195907                 129 Sets
```

Output Data:
```
Row 1: PO=4502195906, Model=RF-2000DEB-K, Qty=1149, Country=NORTHAMPTON
Row 2: PO=4502195906, Model=RF-D10EB-K,   Qty=383,  Country=NORTHAMPTON  
Row 3: PO=4502195906, Model=RF-2400DEB-K, Qty=63,   Country=NORTHAMPTON
Row 4: PO=4502195907, Model=RF-2400DEB-K, Qty=129,  Country=NORTHAMPTON
```

## Format Detection

The system automatically detects Stuffing Plan format by looking for these indicators:
- "STUFFING PLAN" or "STUFFING_PLAN"
- "Export Department" or "EXPORT DEPARTMENT" 
- "Shipment to" or "SHIPMENT TO"
- "NO PO", "NO MODEL", "QTY"
- "N.W (Kg)", "G.W (Kg)", "Volume (M3)"
- "Ready :", "LCL"

## Multi-Sheet Support

The implementation processes all sheets in the workbook:
- Each sheet represents a different destination country
- Data from all sheets is combined into a single result set
- Country information is preserved per row based on source sheet
