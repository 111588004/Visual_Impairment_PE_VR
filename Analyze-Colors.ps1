# Analyze-Colors.ps1
# This script extracts cell text and background colors from an Excel file.

param (
    [Parameter(Mandatory=$true)]
    [string]$FilePath
)

$FullPath = Resolve-Path $FilePath
if (-not (Test-Path $FullPath)) {
    Write-Error "File not found: $FullPath"
    exit 1
}

try {
    $Excel = New-Object -ComObject Excel.Application
    $Excel.Visible = $false
    $Workbook = $Excel.Workbooks.Open($FullPath)
    $Worksheet = $Workbook.Sheets.Item(1)
    
    $UsedRange = $Worksheet.UsedRange
    $Rows = $UsedRange.Rows.Count
    $Cols = $UsedRange.Columns.Count

    Write-Host "Extracting data from $Rows rows and $Cols columns..."
    
    $results = @()
    for ($r = 1; $r -le $Rows; $r++) {
        $row = @()
        for ($c = 1; $c -le $Cols; $c++) {
            $cell = $UsedRange.Cells.Item($r, $c)
            $row += @{
                text = $cell.Text
                color = $cell.Interior.Color
                row = $r
                col = $c
            }
        }
        $results += ,$row
    }

    $results | ConvertTo-Json | Out-File -FilePath "excel_analysis.json"
    Write-Host "Analysis exported to excel_analysis.json"

    $Workbook.Close($false)
    $Excel.Quit()
}
catch {
    Write-Error "Failed to analyze Excel file: $_"
    if ($Excel) { $Excel.Quit() }
}
finally {
    # Release COM objects
    if ($UsedRange) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($UsedRange) | Out-Null }
    if ($Worksheet) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Worksheet) | Out-Null }
    if ($Workbook) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Workbook) | Out-Null }
    if ($Excel) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null }
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
}
