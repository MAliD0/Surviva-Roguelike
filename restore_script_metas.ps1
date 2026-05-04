param(
    [Parameter(Mandatory = $true)]
    [string]$BaseCommit
)

$ScriptsRoot = "Assets/_Assets/Scripts"

Write-Host "Using base commit: $BaseCommit"
Write-Host "Restoring only GUID lines in .cs.meta files..."
Write-Host ""

function Test-GitPathExists {
    param(
        [string]$Commit,
        [string]$Path
    )

    git cat-file -e "$Commit`:$Path" 2>$null
    return $LASTEXITCODE -eq 0
}

function Get-OldGuid {
    param(
        [string]$Commit,
        [string]$Path
    )

    $lines = @(git show "$Commit`:$Path" 2>$null)

    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    foreach ($line in $lines) {
        if ($line -match "^guid:\s*([a-fA-F0-9]+)") {
            return $matches[1]
        }
    }

    return $null
}

function Set-MetaGuid {
    param(
        [string]$Path,
        [string]$Guid
    )

    $fullPath = Join-Path (Get-Location).Path $Path
    $lines = @(Get-Content -Path $fullPath)

    $found = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^guid:\s*") {
            $lines[$i] = "guid: $Guid"
            $found = $true
            break
        }
    }

    if (-not $found) {
        throw "No guid line found in current meta: $Path"
    }

    $content = ($lines -join "`n") + "`n"
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($fullPath, $content, $encoding)
}

$currentMetaFiles = @(
    Get-ChildItem -Path $ScriptsRoot -Recurse -Filter "*.cs.meta" |
    ForEach-Object {
        $_.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    }
)

$oldMetaFiles = @(
    git ls-tree -r --name-only $BaseCommit $ScriptsRoot |
    Where-Object { $_ -like "*.cs.meta" }
)

$restored = 0
$skipped = @()

foreach ($currentPath in $currentMetaFiles) {
    $fileName = Split-Path $currentPath -Leaf
    $oldPath = $null

    if (Test-GitPathExists -Commit $BaseCommit -Path $currentPath) {
        $oldPath = $currentPath
    }
    else {
        $matches = @(
            $oldMetaFiles |
            Where-Object {
                (Split-Path $_ -Leaf) -eq $fileName
            }
        )

        if ($matches.Count -eq 1) {
            $oldPath = $matches[0]
        }
        elseif ($matches.Count -gt 1) {
            $skipped += "AMBIGUOUS: $currentPath"
            foreach ($m in $matches) {
                $skipped += "    candidate: $m"
            }
            continue
        }
        else {
            $skipped += "NO MATCH: $currentPath"
            continue
        }
    }

    $oldGuid = Get-OldGuid -Commit $BaseCommit -Path $oldPath

    if ([string]::IsNullOrWhiteSpace($oldGuid)) {
        $skipped += "NO OLD GUID: $currentPath <- $oldPath"
        continue
    }

    Write-Host "Restoring GUID:"
    Write-Host "  $currentPath"
    Write-Host "  from $oldPath"
    Write-Host "  guid $oldGuid"

    Set-MetaGuid -Path $currentPath -Guid $oldGuid

    $restored++
}

Write-Host ""
Write-Host "Done."
Write-Host "GUIDs restored: $restored"

if ($skipped.Count -gt 0) {
    Write-Host ""
    Write-Host "Skipped:"
    foreach ($item in $skipped) {
        Write-Host "  $item"
    }
}