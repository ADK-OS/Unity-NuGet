<#
.SYNOPSIS
    Automates creating and publishing a new Unity package release to GitHub.
.DESCRIPTION
    1. Reads the package version from package.json (e.g. 0.3.0).
    2. Stages all repository changes and commits them.
    3. Creates an annotated Git tag (e.g. v0.3.0).
    4. Pushes to origin main and pushes the tag to trigger GitHub Actions release.
#>

param(
    [string]$Version = "",
    [string]$Message = ""
)

$ErrorActionPreference = "Stop"

# 1. Read version from package.json if not provided
if ([string]::IsNullOrWhiteSpace($Version)) {
    if (Test-Path "package.json") {
        $pkg = Get-Content "package.json" -Raw | ConvertFrom-Json
        $Version = $pkg.version
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Error "Could not determine package version from package.json"
    exit 1
}

$tag = if ($Version.StartsWith("v")) { $Version } else { "v$Version" }

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Unity NuGet Release Automation: $tag" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 2. Stage all changes
Write-Host "`n[1/4] Staging changes..." -ForegroundColor Yellow
git add .

# 3. Commit
$commitMsg = if (![string]::IsNullOrWhiteSpace($Message)) { $Message } else { "Release $tag: Unity NuGet package" }
Write-Host "`n[2/4] Committing changes: '$commitMsg'..." -ForegroundColor Yellow
$status = git status --porcelain
if ($status) {
    git commit -m $commitMsg
} else {
    Write-Host "No uncommitted changes." -ForegroundColor Gray
}

# 4. Create Git Tag
Write-Host "`n[3/4] Creating tag '$tag'..." -ForegroundColor Yellow
$existingTag = git tag -l $tag
if ($existingTag) {
    Write-Host "Tag '$tag' already exists locally. Replacing..." -ForegroundColor Yellow
    git tag -d $tag
}
git tag -a $tag -m "Release $tag"

# 5. Push to GitHub
Write-Host "`n[4/4] Pushing to GitHub (main & $tag)..." -ForegroundColor Yellow
git push origin main
git push origin $tag --force

Write-Host "`n✔ Release $tag pushed successfully!" -ForegroundColor Green
Write-Host "GitHub Actions will now automatically build and publish the release at:" -ForegroundColor Green
Write-Host "https://github.com/ADK-OS/Unity-NuGet/releases`n" -ForegroundColor Cyan
