# Read-ExcelSkill.ps1
# This script uses Excel COM to read data and output it as JSON.
# Usage: .\Read-ExcelSkill.ps1 -FilePath "path\to\file.xlsx"

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

    $Data = @()
    $Headers = @()

    # Get Headers
    for ($c = 1; $c -le $Cols; $c++) {
        $Headers += $UsedRange.Cells.Item(1, $c).Text
    }

    # Get Data
    for ($r = 2; $r -le $Rows; $r++) {
        $RowObj = [PSCustomObject]@{}
        for ($c = 1; $c -le $Cols; $c++) {
            $Header = $Headers[$c-1]
            if (-not $Header) { $Header = "Column$c" }
            $Value = $UsedRange.Cells.Item($r, $c).Text
            $RowObj | Add-Member -MemberType NoteProperty -Name $Header -Value $Value
        }
        $Data += $RowObj
    }

    $Workbook.Close($false)
    $Excel.Quit()
    
    # Output as JSON
    $Data | ConvertTo-Json
}
catch {
    Write-Error "Failed to read Excel file: $_"
    if ($Excel) { $Excel.Quit() }
}
finally {
    # Release COM objects
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($UsedRange) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Worksheet) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Workbook) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($Excel) | Out-Null
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
}
