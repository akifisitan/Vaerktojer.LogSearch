# Generate random number of files to create (between 3 and 15)
$numFiles = Get-Random -Minimum 10 -Maximum 11

Write-Host "Creating $numFiles test files concurrently..." -ForegroundColor Green

# Create array of file configurations
$fileConfigs = 1..$numFiles | ForEach-Object {
    @{
        Index = $_
        Size = Get-Random -Minimum 100 -Maximum 101
        FileName = "$([guid]::NewGuid().ToString()).log"
    }
}

# Display what we're about to create
$fileConfigs | ForEach-Object {
    Write-Host "Will create $($_.FileName) with size $($_.Size)MB..." -ForegroundColor Yellow
}

Write-Host "`nStarting concurrent execution..." -ForegroundColor Cyan

# Run all commands concurrently (PowerShell 7+ required)
$results = $fileConfigs | ForEach-Object -Parallel {
    $config = $_
    
    $result = @{
        FileName = $config.FileName
        Size = $config.Size
        Success = $false
        Output = ""
        Error = ""
    }
    
    try {
        $process = Start-Process -FilePath "C:\Users\user\projects\Vaerktojer.LogSearch\Vaerktojer.LogSearch.TestConsoleApp\bin\Debug\net8.0\Vaerktojer.LogSearch.TestConsoleApp.exe" -ArgumentList "--size", "$($config.Size)MB", "--output", $config.FileName -Wait -PassThru -NoNewWindow
        
        $result.Success = ($process.ExitCode -eq 0)
        $result.Output = "Process completed with exit code: $($process.ExitCode)"
    }
    catch {
        $result.Error = $_.Exception.Message
    }
    
    return $result
} -ThrottleLimit 5

# Display results
Write-Host "`nResults:" -ForegroundColor Cyan
foreach ($result in $results) {
    if ($result.Success) {
        Write-Host "✓ Successfully created $($result.FileName) ($($result.Size)MB)" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to create $($result.FileName) ($($result.Size)MB)" -ForegroundColor Red
        if ($result.Error) {
            Write-Host "  Error: $($result.Error)" -ForegroundColor Red
        }
    }
}

Write-Host "`nCompleted creating $($numFiles) test files concurrently!" -ForegroundColor Cyan