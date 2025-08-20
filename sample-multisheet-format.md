# Format Excel Baru - Multi Sheet Format

## Struktur Format Baru

### Format: Multiple Sheets dengan 3 Kolom
- **Sheet Name**: Nama Negara (JAKARTA, BANDUNG, SURABAYA, dll)
- **Kolom**: PO | Model | Qty
- **No Country Column**: Country ditentukan dari nama sheet

### Contoh Struktur File:

#### Sheet: JAKARTA
| PO | Model | Qty |
|----|--------|-----|
| PO001 | ModelA | 100 |
| PO002 | ModelB | 150 |

#### Sheet: BANDUNG  
| PO | Model | Qty |
|----|--------|-----|
| PO001 | ModelC | 200 |
| PO003 | ModelD | 75 |

#### Sheet: SURABAYA
| PO | Model | Qty |
|----|--------|-----|
| PO004 | ModelE | 300 |
| PO005 | ModelF | 120 |

## Keuntungan Format Baru:
1. **Lebih Terorganisir**: Setiap negara punya sheet sendiri
2. **Lebih Sederhana**: Hanya 3 kolom per sheet
3. **Lebih Scalable**: Mudah menambah negara baru
4. **Lebih Readable**: Lebih mudah dibaca dan dikelola

## How Parsing Works:
1. **Sheet Detection**: Deteksi semua sheet names sebagai country names
2. **Country Normalization**: Normalize country names menggunakan CountryNormalizer
3. **Data Extraction**: Parse setiap sheet dengan 3 kolom (PO | Model | Qty)
4. **Data Merging**: Combine semua data dengan country dari sheet name

## Catatan Teknis:
- Parser hanya mendukung format baru ini
- Backward compatibility dengan format lama sudah dihilangkan
- Format detection akan selalu mendeteksi sebagai "MULTI_SHEET"
