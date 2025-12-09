# Generate WiX components from Publish directory
$publishDir = ".\Publish"
$files = Get-ChildItem $publishDir -File -Exclude "*.pdb"

Write-Host "Generating component list for $($files.Count) files..."

$components = @()
foreach ($file in $files) {
    $components += @"
      <Component>
        <File Source="$($file.Name)" />
      </Component>
"@
}

$componentXml = $components -join "`n"

Write-Host ""
Write-Host "Copy this into Package.wxs ComponentGroup:"
Write-Host ""
Write-Host $componentXml
