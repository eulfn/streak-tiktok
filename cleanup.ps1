$keeps = @("v1.0.0", "v1.0", "v1.5.0", "v1.5", "v2.0.0", "v2.0", "v2.5.0", "v2.5", "v3.0.0", "v3.0", "v1.7.2", "v2.2.5")
$tags = gh release list --limit 1000 --json tagName -q ".[].tagName"
foreach ($tag in $tags) {
    if (-not [string]::IsNullOrWhiteSpace($tag) -and $tag -notin $keeps) {
        Write-Host "Deleting release and tag: $tag"
        gh release delete $tag --cleanup-tag -y
    }
}
Write-Host "Cleanup finished."