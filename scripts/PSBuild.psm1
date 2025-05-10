# PSBuild Module for .NET CI/CD
# Author: ktsu.dev
# License: MIT
#
# A comprehensive PowerShell module for automating the build, test, package,
# and release process for .NET applications using Git-based versioning.
# This module provides a complete CI/CD pipeline implementation with:
#   - Semantic versioning from Git history
#   - Changelog generation
#   - .NET building, testing, and packaging
#   - GitHub release automation
#   - NuGet package publishing
# See README.md for detailed documentation and usage examples.

# Set Strict Mode
Set-StrictMode -Version Latest

#region Environment and Configuration

function Initialize-BuildEnvironment {
    <#
    .SYNOPSIS
        Initializes the build environment with standard settings for .NET SDK.
    .DESCRIPTION
        Sets up environment variables for .NET SDK to optimize the build experience:
        - Disables first-time experience to improve performance
        - Opts out of telemetry for privacy
        - Disables logo display for cleaner output

        This function should be called at the beginning of any build workflow.
    .EXAMPLE
        Initialize-BuildEnvironment
        # Output: "Build environment initialized"
    .NOTES
        This function doesn't require any parameters and returns no output.
    #>
    [CmdletBinding()]
    param()

    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = 'true'

    Write-Information "Build environment initialized" -Tags "Initialize-BuildEnvironment"
}

function Get-BuildConfiguration {
    <#
    .SYNOPSIS
        Gets the build configuration based on Git status and environment.
    .DESCRIPTION
        Determines build configuration settings by analyzing Git repository state and environment variables.
        This function evaluates:
        - Whether the build is for an official repository or fork
        - If we're on the main branch and should trigger a release
        - If commits are tagged with [skip ci] to prevent releases
        - Build paths and artifact patterns
        - .NET SDK-specific configuration

        Returns a standardized configuration object containing all settings needed for the build process.
    .PARAMETER ServerUrl
        The GitHub server URL (e.g., "https://github.com").
    .PARAMETER GitRef
        The Git reference (branch/tag) being built (e.g., "refs/heads/main").
    .PARAMETER GitSha
        The Git commit SHA being built.
    .PARAMETER GitHubOwner
        The GitHub owner/organization of the repository.
    .PARAMETER GitHubRepo
        The GitHub repository name.
    .PARAMETER GithubToken
        The GitHub token for API operations.
    .PARAMETER NuGetApiKey
        The NuGet API key for package publishing.
    .PARAMETER WorkspacePath
        The path to the workspace/repository root.
    .PARAMETER ExpectedOwner
        The expected owner/organization of the official repository.
    .PARAMETER ChangelogFile
        The path to the changelog file (typically "CHANGELOG.md").
    .PARAMETER LatestChangelogFile
        The path to the file containing only the latest version's changelog. Defaults to "LATEST_CHANGELOG.md".
    .PARAMETER AssetPatterns
        Array of glob patterns for files to include as release assets (e.g., "*.zip", "*.nupkg").
    .EXAMPLE
        $config = Get-BuildConfiguration -ServerUrl "https://github.com" -GitRef "refs/heads/main" `
            -GitSha "abc123" -GitHubOwner "myorg" -GitHubRepo "myproject" -GithubToken $token `
            -NuGetApiKey $nugetKey -WorkspacePath "C:\projects\myrepo" -ExpectedOwner "myorg" `
            -ChangelogFile "CHANGELOG.md" -AssetPatterns @("*.nupkg", "*.zip")

        # Result contains standardized configuration with all paths and settings needed for the build
    .OUTPUTS
        [PSCustomObject] A configuration object with Success, Error, and Data properties.
        The Data property contains all build configuration settings.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ServerUrl,
        [Parameter(Mandatory=$true)]
        [string]$GitRef,
        [Parameter(Mandatory=$true)]
        [string]$GitSha,
        [Parameter(Mandatory=$true)]
        [string]$GitHubOwner,
        [Parameter(Mandatory=$true)]
        [string]$GitHubRepo,
        [Parameter(Mandatory=$true)]
        [string]$GithubToken,
        [Parameter(Mandatory=$true)]
        [string]$NuGetApiKey,
        [Parameter(Mandatory=$true)]
        [string]$WorkspacePath,
        [Parameter(Mandatory=$true)]
        [string]$ExpectedOwner,
        [Parameter(Mandatory=$true)]
        [string]$ChangelogFile,
        [Parameter(Mandatory=$false)]
        [string]$LatestChangelogFile = "LATEST_CHANGELOG.md",
        [Parameter(Mandatory=$true)]
        [string[]]$AssetPatterns
    )

    # Determine if this is an official repo (verify owner and ensure it's not a fork)
    $IS_OFFICIAL = $false
    if ($GithubToken) {
        try {
            $env:GH_TOKEN = $GithubToken
            $repoInfo = "gh repo view --json owner,nameWithOwner,isFork 2>`$null" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration" | ConvertFrom-Json
            if ($repoInfo) {
                # Consider it official only if it's not a fork AND belongs to the expected owner
                $IS_OFFICIAL = (-not $repoInfo.isFork) -and ($repoInfo.owner.login -eq $ExpectedOwner)
                Write-Information "Repository: $($repoInfo.nameWithOwner), Is Fork: $($repoInfo.isFork), Owner: $($repoInfo.owner.login)" -Tags "Get-BuildConfiguration"
            } else {
                Write-Information "Could not retrieve repository information. Assuming unofficial build." -Tags "Get-BuildConfiguration"
            }
        }
        catch {
            Write-Information "Failed to check repository status: $_. Assuming unofficial build." -Tags "Get-BuildConfiguration"
        }
    }

    Write-Information "Is Official: $IS_OFFICIAL" -Tags "Get-BuildConfiguration"

    # Determine if this is main branch and not tagged
    $IS_MAIN = $GitRef -eq "refs/heads/main"
    $IS_TAGGED = "(git show-ref --tags -d | Out-String).Contains(`"$GitSha`")" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration"

    # Check if all commits are marked with [skip ci]
    $ONLY_SKIP_CI = $false
    if ($IS_MAIN -AND -NOT $IS_TAGGED -AND $IS_OFFICIAL) {
        # Find the last release tag
        $tags = "git tag --list" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration"
        $hasRealTags = ($null -ne $tags) -and ($tags.Count -gt 0) -and (-not [string]::IsNullOrWhiteSpace($tags))

        # Determine the commit range to check
        $commitRange = ""
        if ($hasRealTags) {
            # If we have tags, use the most recent one
            $sortedTags = "git tag --sort=-v:refname" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration"
            $lastTag = $sortedTags[0]
            Write-Information "Found tags. Using most recent tag: $lastTag" -Tags "Get-BuildConfiguration"
            $commitRange = "$lastTag..HEAD"
        } else {
            # If no tags, check all commits or use a limited number to avoid checking the entire history
            Write-Information "No tags found. Checking recent commits only." -Tags "Get-BuildConfiguration"
            $commitRange = "-10" # Last 10 commits
        }

        # Get commit messages for the range
        Write-Information "Checking commits in range: $commitRange" -Tags "Get-BuildConfiguration"
        $allCommitMessages = "git log --format=format:%s $commitRange" | Invoke-ExpressionWithLogging -Tags "Get-BuildConfiguration"

        if ($allCommitMessages) {
            # Ensure we're working with an array - split string output into lines if needed
            if ($allCommitMessages -is [string]) {
                $allCommitMessages = $allCommitMessages -split "`n"
            }

            # Count commits that are not [skip ci]
            $nonSkipCommits = @()
            foreach ($message in $allCommitMessages) {
                if (-not $message.Contains("[skip ci]")) {
                    $nonSkipCommits += $message
                }
            }

            $ONLY_SKIP_CI = ($nonSkipCommits.Count -eq 0)

            Write-Information "Total commits: $($allCommitMessages.Count), Non-skip commits: $($nonSkipCommits.Count)" -Tags "Get-BuildConfiguration"

            if ($ONLY_SKIP_CI) {
                Write-Information "All commits are tagged with [skip ci], skipping release" -Tags "Get-BuildConfiguration"
            }
        } else {
            Write-Information "No commits found in range" -Tags "Get-BuildConfiguration"
        }
    }

    # Only release if we're on main, not tagged, official repo, and have non-skip-ci commits
    $SHOULD_RELEASE = ($IS_MAIN -AND -NOT $IS_TAGGED -AND $IS_OFFICIAL -AND -NOT $ONLY_SKIP_CI)

    # Check for .csx files (dotnet-script)
    $csx = @(Get-ChildItem -Path $WorkspacePath -Recurse -Filter *.csx -ErrorAction SilentlyContinue)
    $USE_DOTNET_SCRIPT = $csx.Count -gt 0

    # Setup paths
    $OUTPUT_PATH = Join-Path $WorkspacePath 'output'
    $STAGING_PATH = Join-Path $WorkspacePath 'staging'

    # Setup artifact patterns
    $PACKAGE_PATTERN = Join-Path $STAGING_PATH "*.nupkg"
    $SYMBOLS_PATTERN = Join-Path $STAGING_PATH "*.snupkg"
    $APPLICATION_PATTERN = Join-Path $STAGING_PATH "*.zip"

    # Set build arguments
    $BUILD_ARGS = $USE_DOTNET_SCRIPT ? "-maxCpuCount:1" : ""

    # Create configuration object with standard format
    $config = [PSCustomObject]@{
        Success = $true
        Error = ""
        Data = @{
            IsOfficial = $IS_OFFICIAL
            IsMain = $IS_MAIN
            IsTagged = $IS_TAGGED
            ShouldRelease = $SHOULD_RELEASE
            OnlySkipCI = $ONLY_SKIP_CI
            UseDotnetScript = $USE_DOTNET_SCRIPT
            OutputPath = $OUTPUT_PATH
            StagingPath = $STAGING_PATH
            PackagePattern = $PACKAGE_PATTERN
            SymbolsPattern = $SYMBOLS_PATTERN
            ApplicationPattern = $APPLICATION_PATTERN
            BuildArgs = $BUILD_ARGS
            WorkspacePath = $WorkspacePath
            DotnetVersion = $script:DOTNET_VERSION
            ServerUrl = $ServerUrl
            GitRef = $GitRef
            GitSha = $GitSha
            GitHubOwner = $GitHubOwner
            GitHubRepo = $GitHubRepo
            GithubToken = $GithubToken
            NuGetApiKey = $NuGetApiKey
            ExpectedOwner = $ExpectedOwner
            Version = "1.0.0-pre.0"
            ReleaseHash = $GitSha
            ChangelogFile = $ChangelogFile
            LatestChangelogFile = $LatestChangelogFile
            AssetPatterns = $AssetPatterns
        }
    }

    return $config
}

#endregion

#region Version Management

function Get-GitTags {
    <#
    .SYNOPSIS
        Gets sorted git tags from the repository.
    .DESCRIPTION
        Retrieves a list of git tags sorted by version in descending order (newest first).
        This function:
        - Configures git versionsort to correctly handle version tags with prereleases
        - Returns tags with proper sorting to ensure semantic version ordering
        - Returns a default tag ('v1.0.0-pre.0') if no tags exist in the repository

        The output is used for version calculations and changelog generation.
    .EXAMPLE
        $tags = Get-GitTags
        # Returns an array of version tags like @('v2.1.0', 'v2.0.0', 'v1.0.0')
    .OUTPUTS
        [string[]] An array of version tags in descending order, or a default tag if none exist.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param ()

    # Configure git versionsort to correctly handle prereleases
    $suffixes = @('-alpha', '-beta', '-rc', '-pre')
    foreach ($suffix in $suffixes) {
        "git config versionsort.suffix `"$suffix`"" | Invoke-ExpressionWithLogging -Tags "Get-GitTags" | Write-InformationStream -Tags "Get-GitTags"
    }

    Write-Information "Getting sorted tags..." -Tags "Get-GitTags"
    # Get tags and ensure we return an array
    $output = "git tag --list --sort=-v:refname" | Invoke-ExpressionWithLogging -Tags "Get-GitTags"

    $tags = @($output)

    # Return default if no tags exist
    if ($null -eq $tags -or $tags.Count -eq 0) {
        Write-Information "No tags found, returning default v1.0.0-pre.0" -Tags "Get-GitTags"
        return @('v1.0.0-pre.0')
    }

    Write-Information "Found $($tags.Count) tags" -Tags "Get-GitTags"
    return $tags
}

function Get-VersionType {
    <#
    .SYNOPSIS
        Determines the type of semantic version bump needed based on commit history.
    .DESCRIPTION
        Analyzes commit messages and code changes to determine the appropriate semantic version bump:

        1. Major (1.0.0 → 2.0.0): Breaking changes, indicated by [major] tags in commits
        2. Minor (1.0.0 → 1.1.0): New features or non-breaking API changes, indicated by:
           - [minor] tags in commits
           - Detection of public API changes through regex patterns
        3. Patch (1.0.0 → 1.0.1): Bug fixes and changes that don't modify the public API
           - [patch] tags in commits
           - Code changes that don't modify public API
        4. Prerelease (1.0.0 → 1.0.1-pre.1): Small changes or no significant changes
           - [pre] tags in commits
           - Default when no other version bump is detected

        The function returns both the version increment type and the reason for the decision.
    .PARAMETER Range
        The git commit range to analyze (e.g., "v1.0.0...HEAD" or a specific commit range).
    .EXAMPLE
        $result = Get-VersionType -Range "v1.0.0..HEAD"
        # Returns an object with Type ("major", "minor", "patch", or "prerelease") and Reason properties
    .OUTPUTS
        [PSCustomObject] An object with Type and Reason properties explaining the version increment decision.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Range
    )

    # Initialize to the most conservative version bump
    $versionType = "prerelease"
    $reason = "No significant changes detected"

    # Bot and PR patterns to exclude
    $EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
    $EXCLUDE_PRS = '^.*(Merge pull request|Merge branch ''main''|Updated packages in|Update.*package version).*$'

    # First check for explicit version markers in commit messages
    $messages = "git log --format=format:%s `"$Range`"" | Invoke-ExpressionWithLogging -Tags "Get-VersionType"

    foreach ($message in $messages) {
        if ($message.Contains('[major]')) {
            $versionType = 'major'
            $reason = "Explicit [major] tag found in commit message: $message"
            # Return immediately for major version bumps
            return [PSCustomObject]@{
                Type = $versionType
                Reason = $reason
            }
        } elseif ($message.Contains('[minor]') -and $versionType -ne 'major') {
            $versionType = 'minor'
            $reason = "Explicit [minor] tag found in commit message: $message"
        } elseif ($message.Contains('[patch]') -and $versionType -notin @('major', 'minor')) {
            $versionType = 'patch'
            $reason = "Explicit [patch] tag found in commit message: $message"
        } elseif ($message.Contains('[pre]') -and $versionType -eq 'prerelease') {
            # Keep as prerelease, but update reason
            $reason = "Explicit [pre] tag found in commit message: $message"
        }
    }

    # If no explicit version markers, check for code changes
    if ($versionType -eq "prerelease") {
        # Check for any commits that would warrant at least a patch version
        $patchCommits = "git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer=`"$EXCLUDE_BOTS`" --author=`"$EXCLUDE_BOTS`" --grep=`"$EXCLUDE_PRS`" --invert-grep `"$Range`"" | Invoke-ExpressionWithLogging -Tags "Get-VersionType"

        if ($patchCommits) {
            $versionType = "patch"
            $reason = "Found changes warranting at least a patch version"

            # Check for public API changes that would warrant a minor version

            # First, check if we can detect public API changes via git diff
            $apiChangePatterns = @(
                # C# public API patterns
                '^\+\s*(public|protected)\s+(class|interface|enum|struct|record)\s+\w+',  # Added public types
                '^\+\s*(public|protected)\s+\w+\s+\w+\s*\(',                             # Added public methods
                '^\+\s*(public|protected)\s+\w+(\s+\w+)*\s*{',                          # Added public properties
                '^\-\s*(public|protected)\s+(class|interface|enum|struct|record)\s+\w+', # Removed public types
                '^\-\s*(public|protected)\s+\w+\s+\w+\s*\(',                            # Removed public methods
                '^\-\s*(public|protected)\s+\w+(\s+\w+)*\s*{',                          # Removed public properties
                '^\+\s*public\s+const\s',                                              # Added public constants
                '^\-\s*public\s+const\s'                                               # Removed public constants
            )

            # Combine patterns for git diff
            $apiChangePattern = "(" + ($apiChangePatterns -join ")|(") + ")"

            # Search for API changes
            $apiDiffCmd = "git diff `"$Range`" -- `"*.cs`" | Select-String -Pattern `"$apiChangePattern`" -SimpleMatch"
            $apiChanges = Invoke-Expression $apiDiffCmd

            if ($apiChanges) {
                $versionType = "minor"
                $reason = "Public API changes detected (additions, removals, or modifications)"
                return [PSCustomObject]@{
                    Type = $versionType
                    Reason = $reason
                }
            }
        }
    }

    return [PSCustomObject]@{
        Type = $versionType
        Reason = $reason
    }
}

function Get-VersionInfoFromGit {
    <#
    .SYNOPSIS
        Gets comprehensive version information based on Git tags and commit analysis.
    .DESCRIPTION
        Analyzes the Git repository to determine the next version number based on semantic versioning principles:

        1. Finds the most recent version tag in the repository
        2. Analyzes commit history since that tag
        3. Determines the appropriate version increment (major, minor, patch, or prerelease)
        4. Calculates the new version number

        The function follows SemVer 2.0 practices for version calculation, including proper handling
        of prerelease versions. It provides a rich object containing all version components and
        the reasoning behind the version decision.
    .PARAMETER CommitHash
        The Git commit hash being built (typically HEAD or the current commit).
    .PARAMETER InitialVersion
        The version to use if no tags exist in the repository. Defaults to "1.0.0".
    .EXAMPLE
        $versionInfo = Get-VersionInfoFromGit -CommitHash "abc123def456"
        # Returns detailed version information with all components (major, minor, patch, etc.)
    .OUTPUTS
        [PSCustomObject] A comprehensive object with Success, Error, and Data properties.
        The Data property contains version information including:
        - Version (string): The complete version string
        - Major, Minor, Patch (int): Version components
        - IsPrerelease (bool): Whether this is a prerelease version
        - PrereleaseNumber (int): The prerelease version number
        - VersionIncrement (string): The type of increment (major, minor, patch, prerelease)
        - IncrementReason (string): The reason for the version increment
        - CommitRange (string): The Git commit range analyzed
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$InitialVersion = "1.0.0"
    )

    Write-StepHeader "Analyzing Version Information" -Tags "Get-VersionInfoFromGit"
    Write-Information "Analyzing repository for version information..." -Tags "Get-VersionInfoFromGit"
    Write-Information "Commit hash: $CommitHash" -Tags "Get-VersionInfoFromGit"

    # Get all tags
    $tags = Get-GitTags
    Write-Information "Found $($tags.Count) tag(s)" -Tags "Get-VersionInfoFromGit"

    # Get the last tag and its commit
    $usingFallbackTag = $false
    $lastTag = ""

    if ($null -eq $tags -or
        ($tags -is [array] -and $tags.Count -eq 0) -or
        ($tags -is [string] -and $tags.Trim() -eq "")) {
        $lastTag = "v$InitialVersion-pre.0"
        $usingFallbackTag = $true
        Write-Information "No tags found. Using fallback: $lastTag" -Tags "Get-VersionInfoFromGit"
    } elseif ($tags -is [array]) {
        $lastTag = $tags[0]
        Write-Information "Using last tag: $lastTag" -Tags "Get-VersionInfoFromGit"
    } else {
        $lastTag = $tags
        Write-Information "Using single tag: $lastTag" -Tags "Get-VersionInfoFromGit"
    }

    # Extract the version without 'v' prefix
    $lastVersion = $lastTag -replace 'v', ''
    Write-Information "Last version: $lastVersion" -Tags "Get-VersionInfoFromGit"

    # Parse previous version
    $wasPrerelease = $lastVersion.Contains('-')
    $cleanVersion = $lastVersion -replace '-alpha.*$', '' -replace '-beta.*$', '' -replace '-rc.*$', '' -replace '-pre.*$', ''

    $parts = $cleanVersion -split '\.'
    $lastMajor = [int]$parts[0]
    $lastMinor = [int]$parts[1]
    $lastPatch = [int]$parts[2]
    $lastPrereleaseNum = 0

    # Extract prerelease number if applicable
    if ($wasPrerelease -and $lastVersion -match '-(?:pre|alpha|beta|rc)\.(\d+)') {
        $lastPrereleaseNum = [int]$Matches[1]
    }

    # Determine version increment type based on commit range
    Write-Information "$($script:lineEnding)Getting commits to analyze..." -Tags "Get-VersionInfoFromGit"

    # Get the first commit in repo for fallback
    $firstCommit = "git rev-list HEAD" | Invoke-ExpressionWithLogging -Tags "Get-VersionInfoFromGit"
    $firstCommit = $firstCommit[-1]
    Write-Information "First commit: $firstCommit" -Tags "Get-VersionInfoFromGit"

    # Find the last tag's commit
    $lastTagCommit = ""
    if ($usingFallbackTag) {
        $lastTagCommit = $firstCommit
        Write-Information "Using first commit as starting point: $firstCommit" -Tags "Get-VersionInfoFromGit"
    } else {
        $lastTagCommit = "git rev-list -n 1 $lastTag" | Invoke-ExpressionWithLogging -Tags "Get-VersionInfoFromGit"
        Write-Information "Last tag commit: $lastTagCommit" -Tags "Get-VersionInfoFromGit"
    }

    # Define the commit range to analyze
    $commitRange = "$lastTagCommit..$CommitHash"
    Write-Information "Analyzing commit range: $commitRange" -Tags "Get-VersionInfoFromGit"

    # Get the increment type
    $incrementInfo = Get-VersionType -Range $commitRange
    $incrementType = $incrementInfo.Type
    $incrementReason = $incrementInfo.Reason

    # Initialize new version with current values
    $newMajor = $lastMajor
    $newMinor = $lastMinor
    $newPatch = $lastPatch
    $newPrereleaseNum = 0
    $isPrerelease = $false
    $prereleaseLabel = "pre"

    Write-Information "$($script:lineEnding)Calculating new version..." -Tags "Get-VersionInfoFromGit"

    # Calculate new version based on increment type
    switch ($incrementType) {
        'major' {
            $newMajor = $lastMajor + 1
            $newMinor = 0
            $newPatch = 0
            Write-Information "Incrementing major version: $lastMajor.$lastMinor.$lastPatch -> $newMajor.0.0" -Tags "Get-VersionInfoFromGit"
        }
        'minor' {
            $newMinor = $lastMinor + 1
            $newPatch = 0
            Write-Information "Incrementing minor version: $lastMajor.$lastMinor.$lastPatch -> $lastMajor.$newMinor.0" -Tags "Get-VersionInfoFromGit"
        }
        'patch' {
            if (-not $wasPrerelease) {
                $newPatch = $lastPatch + 1
                Write-Information "Incrementing patch version: $lastMajor.$lastMinor.$lastPatch -> $lastMajor.$lastMinor.$newPatch" -Tags "Get-VersionInfoFromGit"
            } else {
                Write-Information "Converting prerelease to stable version: $lastVersion -> $lastMajor.$lastMinor.$lastPatch" -Tags "Get-VersionInfoFromGit"
            }
        }
        'prerelease' {
            if ($wasPrerelease) {
                # Bump prerelease number
                $newPrereleaseNum = $lastPrereleaseNum + 1
                $isPrerelease = $true
                Write-Information "Incrementing prerelease: $lastVersion -> $lastMajor.$lastMinor.$lastPatch-$prereleaseLabel.$newPrereleaseNum" -Tags "Get-VersionInfoFromGit"
            } else {
                # Start new prerelease series
                $newPatch = $lastPatch + 1
                $newPrereleaseNum = 1
                $isPrerelease = $true
                Write-Information "Starting new prerelease: $lastVersion -> $lastMajor.$lastMinor.$newPatch-$prereleaseLabel.1" -Tags "Get-VersionInfoFromGit"
            }
        }
    }

    # Build version string
    $newVersion = "$newMajor.$newMinor.$newPatch"
    if ($isPrerelease) {
        $newVersion += "-$prereleaseLabel.$newPrereleaseNum"
    }

    Write-Information "$($script:lineEnding)Version decision:" -Tags "Get-VersionInfoFromGit"
    Write-Information "Previous version: $lastVersion" -Tags "Get-VersionInfoFromGit"
    Write-Information "New version: $newVersion" -Tags "Get-VersionInfoFromGit"
    Write-Information "Reason: $incrementReason" -Tags "Get-VersionInfoFromGit"

    try {
        # Return comprehensive object with standard format
        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Version = $newVersion
                Major = $newMajor
                Minor = $newMinor
                Patch = $newPatch
                IsPrerelease = $isPrerelease
                PrereleaseNumber = $newPrereleaseNum
                PrereleaseLabel = $prereleaseLabel
                LastTag = $lastTag
                LastVersion = $lastVersion
                LastVersionMajor = $lastMajor
                LastVersionMinor = $lastMinor
                LastVersionPatch = $lastPatch
                WasPrerelease = $wasPrerelease
                LastVersionPrereleaseNumber = $lastPrereleaseNum
                VersionIncrement = $incrementType
                IncrementReason = $incrementReason
                FirstCommit = $firstCommit
                LastCommit = $CommitHash
                LastTagCommit = $lastTagCommit
                UsingFallbackTag = $usingFallbackTag
                CommitRange = $commitRange
            }
        }
    }
    catch {
        return [PSCustomObject]@{
            Success = $false
            Error = $_.ToString()
            Data = [PSCustomObject]@{
                ErrorDetails = $_.Exception.Message
                StackTrace = $_.ScriptStackTrace
            }
            StackTrace = $_.ScriptStackTrace
        }
    }
}

function New-Version {
    <#
    .SYNOPSIS
        Creates a new version file and sets environment variables.
    .DESCRIPTION
        Generates a new version number based on Git history and writes it to VERSION.md.
        This function:
        1. Gets complete version information by analyzing Git commit history
        2. Writes the version number to VERSION.md with proper encoding and line endings
        3. Returns the generated version string for use in other parts of the build process

        The generated version follows semantic versioning principles with proper handling of
        prerelease versions when appropriate.
    .PARAMETER CommitHash
        The Git commit hash being built (typically HEAD or the current commit).
    .PARAMETER OutputPath
        Optional path to write the version file to. Defaults to workspace root.
    .EXAMPLE
        $version = New-Version -CommitHash "abc123def456"
        # Creates VERSION.md with the calculated version number and returns the version string
    .OUTPUTS
        [string] The version string that was generated and written to the file.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$OutputPath = ""
    )

    # Get complete version information object
    $versionInfo = Get-VersionInfoFromGit -CommitHash $CommitHash

    # Write version file with correct line ending
    $filePath = if ($OutputPath) { Join-Path $OutputPath "VERSION.md" } else { "VERSION.md" }
    $version = $versionInfo.Data.Version.Trim()
    [System.IO.File]::WriteAllText($filePath, $version + $script:lineEnding, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Version"

    Write-Information "Previous version: $($versionInfo.Data.LastVersion), New version: $($versionInfo.Data.Version)" -Tags "New-Version"

    return $versionInfo.Data.Version
}

#endregion

#region License Management

function New-License {
    <#
    .SYNOPSIS
        Creates license and copyright files from templates.
    .DESCRIPTION
        Generates LICENSE.md and COPYRIGHT.md files using repository information and the current year.

        This function:
        1. Reads from a license template file (typically MIT license)
        2. Replaces placeholders with project-specific information
        3. Creates both LICENSE.md and COPYRIGHT.md files with proper encoding
        4. Uses the current year for copyright notices

        The license template should contain placeholders like {PROJECT_URL} and {COPYRIGHT}
        which will be replaced with actual values during generation.
    .PARAMETER ServerUrl
        The GitHub server URL (e.g., "https://github.com").
    .PARAMETER Owner
        The repository owner/organization name.
    .PARAMETER Repository
        The repository name.
    .PARAMETER OutputPath
        Optional path to write the license files to. Defaults to workspace root.
    .EXAMPLE
        New-License -ServerUrl "https://github.com" -Owner "myorg" -Repository "myproject"
        # Creates LICENSE.md and COPYRIGHT.md in the current directory
    .EXAMPLE
        New-License -ServerUrl "https://github.com" -Owner "myorg" -Repository "myproject" -OutputPath "./output"
        # Creates LICENSE.md and COPYRIGHT.md in the ./output directory
    .NOTES
        The license template is loaded from the LICENSE.template file in the module directory.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$ServerUrl,
        [Parameter(Mandatory=$true)]
        [string]$Owner,
        [Parameter(Mandatory=$true)]
        [string]$Repository,
        [string]$OutputPath = ""
    )

    if (-not (Test-Path $script:LICENSE_TEMPLATE)) {
        throw "License template not found at: $script:LICENSE_TEMPLATE"
    }

    $year = (Get-Date).Year
    $content = Get-Content $script:LICENSE_TEMPLATE -Raw

    # Project URL
    $projectUrl = "$ServerUrl/$Owner/$Repository"
    $content = $content.Replace('{PROJECT_URL}', $projectUrl)

    # Copyright line
    $copyright = "Copyright (c) 2023-$year $Owner"
    $content = $content.Replace('{COPYRIGHT}', $copyright)

    # Normalize line endings
    $content = $content.ReplaceLineEndings($script:lineEnding)

    $copyrightFilePath = if ($OutputPath) { Join-Path $OutputPath "COPYRIGHT.md" } else { "COPYRIGHT.md" }
    [System.IO.File]::WriteAllText($copyrightFilePath, $copyright + $script:lineEnding, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-License"

    $filePath = if ($OutputPath) { Join-Path $OutputPath "LICENSE.md" } else { "LICENSE.md" }
    [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-License"

    Write-Information "License file created at: $filePath" -Tags "New-License"
}

#endregion

#region Changelog Management

function ConvertTo-FourComponentVersion {
    <#
    .SYNOPSIS
        Converts a version tag to a four-component version for comparison.
    .DESCRIPTION
        Standardizes version tags to a four-component format (major.minor.patch.prerelease) for consistent
        comparison and sorting in version operations. This function:

        1. Removes the 'v' prefix if present
        2. Removes any prerelease designators like '-alpha', '-beta', '-rc', '-pre'
        3. Ensures there are always four components by adding zeros where needed
        4. Returns a consistently formatted version string for numeric comparison

        This standardization is essential for correctly ordering versions with different formats.
    .PARAMETER VersionTag
        The version tag to convert (e.g., "v1.2.3-alpha.1", "v2.0.0", etc.).
    .EXAMPLE
        ConvertTo-FourComponentVersion -VersionTag "v1.2.3-alpha.1"
        # Returns "1.2.3.1"
    .EXAMPLE
        ConvertTo-FourComponentVersion -VersionTag "v2.0.0"
        # Returns "2.0.0.0"
    .OUTPUTS
        [string] A standardized four-component version string.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$VersionTag
    )

    $version = $VersionTag -replace 'v', ''
    $version = $version -replace '-alpha', '' -replace '-beta', '' -replace '-rc', '' -replace '-pre', ''
    $versionComponents = $version -split '\.'
    $versionMajor = [int]$versionComponents[0]
    $versionMinor = [int]$versionComponents[1]
    $versionPatch = [int]$versionComponents[2]
    $versionPrerelease = 0

    if (@($versionComponents).Count -gt 3) {
        $versionPrerelease = [int]$versionComponents[3]
    }

    return "$versionMajor.$versionMinor.$versionPatch.$versionPrerelease"
}

function Get-VersionNotes {
    <#
    .SYNOPSIS
        Generates changelog notes for a specific version range.
    .DESCRIPTION
        Creates formatted Markdown changelog entries for commits between two version tags.
        This function:

        1. Determines the proper version range to analyze
        2. Filters out irrelevant commits (bot commits, merges, etc.)
        3. Formats commit messages into Markdown bullet points with attribution
        4. Adds appropriate headers and version type information

        The resulting changelog follows a consistent format with version headings,
        range information, and properly formatted commit listings.
    .PARAMETER Tags
        Array of all available tags in the repository for reference.
    .PARAMETER FromTag
        The starting tag of the version range (older version).
    .PARAMETER ToTag
        The ending tag of the version range (newer version).
    .PARAMETER ToSha
        Optional specific commit SHA to use as the range end (used for unreleased versions).
    .EXAMPLE
        Get-VersionNotes -Tags @("v2.0.0", "v1.0.0") -FromTag "v1.0.0" -ToTag "v2.0.0"
        # Returns Markdown changelog entries for all commits between v1.0.0 and v2.0.0
    .EXAMPLE
        Get-VersionNotes -Tags @("v1.0.0") -FromTag "v1.0.0" -ToTag "v1.1.0" -ToSha "abc123def456"
        # Returns changelog entries for commits between v1.0.0 and the specified commit
    .OUTPUTS
        [string] Formatted Markdown changelog content for the specified version range.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param (
        [Parameter(Mandatory=$true)]
        [AllowEmptyCollection()]
        [string[]]$Tags,
        [Parameter(Mandatory=$true)]
        [string]$FromTag,
        [Parameter(Mandatory=$true)]
        [string]$ToTag,
        [Parameter()]
        [string]$ToSha = ""
    )

    # Define common patterns used for filtering commits
    $EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
    $EXCLUDE_PRS = '^.*(Merge pull request|Merge branch ''main''|Updated packages in|Update.*package version).*$'

    # Convert tags to comparable versions
    $toVersion = ConvertTo-FourComponentVersion -VersionTag $ToTag
    $fromVersion = ConvertTo-FourComponentVersion -VersionTag $FromTag

    # Parse components for comparison
    $toVersionComponents = $toVersion -split '\.'
    $toVersionMajor = [int]$toVersionComponents[0]
    $toVersionMinor = [int]$toVersionComponents[1]
    $toVersionPatch = [int]$toVersionComponents[2]
    $toVersionPrerelease = [int]$toVersionComponents[3]

    $fromVersionComponents = $fromVersion -split '\.'
    $fromVersionMajor = [int]$fromVersionComponents[0]
    $fromVersionMinor = [int]$fromVersionComponents[1]
    $fromVersionPatch = [int]$fromVersionComponents[2]
    $fromVersionPrerelease = [int]$fromVersionComponents[3]

    # Calculate previous version numbers for finding the correct tag
    $fromMajorVersionNumber = $toVersionMajor - 1
    $fromMinorVersionNumber = $toVersionMinor - 1
    $fromPatchVersionNumber = $toVersionPatch - 1
    $fromPrereleaseVersionNumber = $toVersionPrerelease - 1

    # Determine version type and search tag
    $searchTag = $FromTag
    $versionType = "unknown"

    if ($toVersionPrerelease -ne 0) {
        $versionType = "prerelease"
        $searchTag = "$toVersionMajor.$toVersionMinor.$toVersionPatch.$fromPrereleaseVersionNumber"
    }
    else {
        if ($toVersionPatch -gt $fromVersionPatch) {
            $versionType = "patch"
            $searchTag = "$toVersionMajor.$toVersionMinor.$fromPatchVersionNumber.0"
        }
        if ($toVersionMinor -gt $fromVersionMinor) {
            $versionType = "minor"
            $searchTag = "$toVersionMajor.$fromMinorVersionNumber.0.0"
        }
        if ($toVersionMajor -gt $fromVersionMajor) {
            $versionType = "major"
            $searchTag = "$fromMajorVersionNumber.0.0.0"
        }
    }

    # Handle case where version is same but prerelease was dropped
    if ($toVersionMajor -eq $fromVersionMajor -and
        $toVersionMinor -eq $fromVersionMinor -and
        $toVersionPatch -eq $fromVersionPatch -and
        $toVersionPrerelease -eq 0 -and
        $fromVersionPrerelease -ne 0) {
        $versionType = "patch"
        $searchTag = "$toVersionMajor.$toVersionMinor.$fromPatchVersionNumber.0"
    }

    if ($searchTag.Contains("-")) {
        $searchTag = $FromTag
    }

    $searchVersion = ConvertTo-FourComponentVersion -VersionTag $searchTag

    if ($FromTag -ne "v0.0.0") {
        $foundSearchTag = $false
        $Tags | ForEach-Object {
            if (-not $foundSearchTag) {
                $otherTag = $_
                $otherVersion = ConvertTo-FourComponentVersion -VersionTag $otherTag
                if ($searchVersion -eq $otherVersion) {
                    $foundSearchTag = $true
                    $searchTag = $otherTag
                }
            }
        }

        if (-not $foundSearchTag) {
            $searchTag = $FromTag
        }
    }

    $rangeFrom = $searchTag
    if ($rangeFrom -eq "v0.0.0" -or $rangeFrom -eq "0.0.0.0" -or $rangeFrom -eq "1.0.0.0") {
        $rangeFrom = ""
    }

    $rangeTo = $ToSha
    if ($rangeTo -eq "") {
        $rangeTo = $ToTag
    }

    # Determine proper commit range
    $isNewestVersion = $false
    if ($ToSha -ne "") {
        # If ToSha is provided, this is likely the newest version being generated
        $isNewestVersion = $true
    }

    # Get the actual commit SHA for the from tag if it exists
    if ($rangeFrom -ne "") {
        $fromSha = "git rev-list -n 1 $rangeFrom" | Invoke-ExpressionWithLogging

        # For the newest version with SHA provided (not yet tagged):
        if ($isNewestVersion -and $ToSha -ne "") {
            $range = "$fromSha..$ToSha"
        } else {
            # For already tagged versions, get the SHA for the to tag
            $toShaResolved = "git rev-list -n 1 $rangeTo" | Invoke-ExpressionWithLogging
            $range = "$fromSha..$toShaResolved"
        }
    } else {
        # Handle case with no FROM tag (first version)
        $range = $rangeTo
    }

    # Debug output
    Write-Information "Processing range: $range (From: $rangeFrom, To: $rangeTo)" -Tags "Get-VersionNotes"

    # Try with progressively more relaxed filtering to ensure we show commits

    # Get full commit info with hash to ensure uniqueness
    $format = '%h|%s|%aN'

    # First try with standard filters
    $rawCommits = "git log --pretty=format:`"$format`" --perl-regexp --regexp-ignore-case --grep=`"$EXCLUDE_PRS`" --invert-grep --committer=`"$EXCLUDE_BOTS`" --author=`"$EXCLUDE_BOTS`" `"$range`"" | Invoke-ExpressionWithLogging

    # If no commits found, try with just PR exclusion but no author filtering
    if (($rawCommits | Measure-Object).Count -eq 0) {
        Write-Information "No commits found with standard filters, trying with relaxed author/committer filters..." -Tags "Get-VersionNotes"
        $rawCommits = "git log --pretty=format:`"$format`" --perl-regexp --regexp-ignore-case --grep=`"$EXCLUDE_PRS`" --invert-grep `"$range`"" | Invoke-ExpressionWithLogging
    }

    # If still no commits, try with no filtering at all - show everything in the range
    if (($rawCommits | Measure-Object).Count -eq 0) {
        Write-Information "Still no commits found, trying with no filters..." -Tags "Get-VersionNotes"
        $rawCommits = "git log --pretty=format:`"$format`" `"$range`"" | Invoke-ExpressionWithLogging

        # If it's a prerelease version, include also version update commits
        if ($versionType -eq "prerelease" -and ($rawCommits | Measure-Object).Count -eq 0) {
            Write-Information "Looking for version update commits for prerelease..." -Tags "Get-VersionNotes"
            $rawCommits = "git log --pretty=format:`"$format`" --grep=`"Update VERSION to`" `"$range`"" | Invoke-ExpressionWithLogging
        }
    }

    # Process raw commits into structured format
    $structuredCommits = @()
    foreach ($commit in $rawCommits) {
        $parts = $commit -split '\|'
        if ($parts.Count -ge 3) {
            $structuredCommits += [PSCustomObject]@{
                Hash = $parts[0]
                Subject = $parts[1]
                Author = $parts[2]
                FormattedEntry = "$($parts[1]) ([@$($parts[2])](https://github.com/$($parts[2])))"
            }
        }
    }

    # Get unique commits based on hash (ensures unique commits)
    $uniqueCommits = $structuredCommits | Sort-Object -Property Hash -Unique | ForEach-Object { $_.FormattedEntry }

    Write-Information "Found $(($uniqueCommits | Measure-Object).Count) commits for $ToTag" -Tags "Get-VersionNotes"

    # Format changelog entry
    $versionChangelog = ""
    if (($uniqueCommits | Measure-Object).Count -gt 0) {
        $versionChangelog = "## $ToTag"
        if ($versionType -ne "unknown") {
            $versionChangelog += " ($versionType)"
        }
        $versionChangelog += "$script:lineEnding$script:lineEnding"

        if ($rangeFrom -ne "") {
            $versionChangelog += "Changes since ${rangeFrom}:$script:lineEnding$script:lineEnding"
        }

        # Only filter out version updates for non-prerelease versions
        if ($versionType -ne "prerelease") {
            $filteredCommits = @()
            foreach ($commit in $uniqueCommits) {
                if (-not $commit.Contains("Update VERSION to") -and -not $commit.Contains("[skip ci]")) {
                    $filteredCommits += $commit
                }
            }
        } else {
            $filteredCommits = @()
            foreach ($commit in $uniqueCommits) {
                if (-not $commit.Contains("[skip ci]")) {
                    $filteredCommits += $commit
                }
            }
        }

        foreach ($commit in $filteredCommits) {
            $versionChangelog += "- $commit$script:lineEnding"
        }
        $versionChangelog += "$script:lineEnding"
    } elseif ($versionType -eq "prerelease") {
        # For prerelease versions with no detected commits, include a placeholder entry
        $versionChangelog = "## $ToTag (prerelease)$script:lineEnding$script:lineEnding"
        $versionChangelog += "Incremental prerelease update.$script:lineEnding$script:lineEnding"
    }

    return ($versionChangelog.Trim() + $script:lineEnding)
}

function New-Changelog {
    <#
    .SYNOPSIS
        Creates a comprehensive changelog file with entries for all versions.
    .DESCRIPTION
        Generates a complete CHANGELOG.md file by analyzing Git history and generating
        formatted entries for each version. This function:

        1. Gets all version tags from the repository
        2. Analyzes commits between versions
        3. Formats a comprehensive changelog with proper Markdown formatting
        4. Optionally creates a separate file with only the latest version's changes

        The resulting changelog is properly formatted with:
        - Version headers with semantic version type indicators (major, minor, patch)
        - Commit messages organized by version
        - Author attribution with GitHub links
        - Proper line endings and encoding
    .PARAMETER Version
        The current version number being released.
    .PARAMETER CommitHash
        The Git commit hash being released.
    .PARAMETER OutputPath
        Optional path to write the changelog file to. Defaults to workspace root.
    .PARAMETER IncludeAllVersions
        Whether to include all previous versions in the changelog. Defaults to $true.
    .PARAMETER LatestChangelogPath
        Optional path to write the latest version's changelog to. Defaults to "LATEST_CHANGELOG.md".
    .EXAMPLE
        New-Changelog -Version "2.0.0" -CommitHash "abc123def456"
        # Creates CHANGELOG.md with entries for all versions and LATEST_CHANGELOG.md with just the latest
    .EXAMPLE
        New-Changelog -Version "1.5.0" -CommitHash "abc123def456" -OutputPath "./output" -IncludeAllVersions $false
        # Creates changelog with only the latest version in the specified directory
    .NOTES
        The latest version changelog is particularly useful for GitHub release notes.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Version,
        [Parameter(Mandatory=$true)]
        [string]$CommitHash,
        [string]$OutputPath = "",
        [bool]$IncludeAllVersions = $true,
        [string]$LatestChangelogPath = "LATEST_CHANGELOG.md"
    )

    # Configure git versionsort to correctly handle prereleases
    $suffixes = @('-alpha', '-beta', '-rc', '-pre')
    foreach ($suffix in $suffixes) {
        "git config versionsort.suffix `"$suffix`"" | Invoke-ExpressionWithLogging -Tags "Get-GitTags" | Write-InformationStream -Tags "Get-GitTags"
    }

    # Get all tags sorted by version
    $tags = Get-GitTags
    $changelog = ""

    # Check if we have any tags at all
    $hasTags = $null -ne $tags -and
              ($tags -is [array] -and $tags.Count -gt 0) -or
              ($tags -is [string] -and $tags.Trim() -ne "")

    # For first release, there's no previous tag to compare against
    $previousTag = 'v0.0.0'

    # If we have tags, find the most recent one to compare against
    if ($hasTags) {
        $previousTag = if ($tags -is [array]) {
            $tags[0]  # Most recent tag
        } else {
            $tags  # Single tag
        }
    }

    # Always add entry for current/new version (comparing current commit to previous tag or initial state)
    $currentTag = "v$Version"
    Write-Information "Generating changelog from $previousTag to $currentTag (commit: $CommitHash)" -Tags "New-Changelog"
    $versionNotes = Get-VersionNotes -Tags $tags -FromTag $previousTag -ToTag $currentTag -ToSha $CommitHash

    # Store the latest version's notes for later use in GitHub releases
    $latestVersionNotes = ""

    # If we have changes, add them to the changelog
    if (-not [string]::IsNullOrWhiteSpace($versionNotes)) {
        $changelog += $versionNotes
        $latestVersionNotes = $versionNotes
    } else {
        # Handle no changes detected case - add a minimal entry
        $minimalEntry = "## $currentTag$script:lineEnding$script:lineEnding"
        $minimalEntry += "Initial release or no significant changes since $previousTag.$script:lineEnding$script:lineEnding"

        $changelog += $minimalEntry
        $latestVersionNotes = $minimalEntry
    }

    # Add entries for all previous versions if requested
    if ($IncludeAllVersions -and $hasTags) {
        $tagIndex = 0

        foreach ($tag in $tags) {
            if ($tag -like "v*") {
                $previousTag = "v0.0.0"
                if ($tagIndex -lt $tags.Count - 1) {
                    $previousTag = $tags[$tagIndex + 1]
                }

                if (-not ($previousTag -like "v*")) {
                    $previousTag = "v0.0.0"
                }

                $versionNotes = Get-VersionNotes -Tags $tags -FromTag $previousTag -ToTag $tag
                $changelog += $versionNotes
            }
            $tagIndex++
        }
    }

    # Write changelog to file
    $filePath = if ($OutputPath) { Join-Path $OutputPath "CHANGELOG.md" } else { "CHANGELOG.md" }

    # Normalize line endings in changelog content
    $changelog = $changelog.ReplaceLineEndings($script:lineEnding)

    [System.IO.File]::WriteAllText($filePath, $changelog, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Changelog"

    # Write latest version's changelog to separate file for GitHub releases
    $latestPath = if ($OutputPath) { Join-Path $OutputPath $LatestChangelogPath } else { $LatestChangelogPath }
    $latestVersionNotes = $latestVersionNotes.ReplaceLineEndings($script:lineEnding)
    [System.IO.File]::WriteAllText($latestPath, $latestVersionNotes, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "New-Changelog"
    Write-Information "Latest version changelog saved to: $latestPath" -Tags "New-Changelog"

    $versionCount = if ($hasTags) { @($tags).Count + 1 } else { 1 }
    Write-Information "Changelog generated with entries for $versionCount versions" -Tags "New-Changelog"
}

#endregion

#region Metadata Management

function Update-ProjectMetadata {
    <#
    .SYNOPSIS
        Updates project metadata files based on build configuration.
    .DESCRIPTION
        Comprehensive function that generates and updates all project metadata files including:

        1. Version information (VERSION.md)
        2. License files (LICENSE.md and COPYRIGHT.md)
        3. Changelog files (CHANGELOG.md and latest version changelog)
        4. Author information (AUTHORS.md)
        5. Project URL shortcuts (PROJECT_URL.url and AUTHORS.url)

        The function also:
        - Commits the changes to Git with proper attribution
        - Pushes the changes to the remote repository
        - Returns the version and commit hash for use in the release process

        All files are created with consistent encoding and line endings.
    .PARAMETER BuildConfiguration
        The build configuration object containing paths, version info, and GitHub details.
        Should be obtained from Get-BuildConfiguration.
    .PARAMETER Authors
        Optional array of author names to include in the AUTHORS.md file.
        Each name will be formatted as a bullet point in the authors file.
    .PARAMETER CommitMessage
        Optional commit message to use when committing metadata changes.
        Defaults to "[bot][skip ci] Update Metadata".
    .EXAMPLE
        $config = Get-BuildConfiguration -ServerUrl "https://github.com" -GitRef "refs/heads/main" `
            -GitSha "abc123" -GitHubOwner "myorg" -GitHubRepo "myproject" -GithubToken $token `
            -NuGetApiKey $nugetKey -WorkspacePath "C:\projects\myrepo" -ExpectedOwner "myorg" `
            -ChangelogFile "CHANGELOG.md" -AssetPatterns @("*.nupkg", "*.zip")

        $result = Update-ProjectMetadata -BuildConfiguration $config

        # Updates and commits all metadata files, returns object with version and commit hash
    .EXAMPLE
        $result = Update-ProjectMetadata -BuildConfiguration $config -Authors @("Developer 1", "Developer 2") `
            -CommitMessage "Update project documentation for release"

        # Includes custom authors list and commit message
    .OUTPUTS
        [PSCustomObject] A standardized result object with:
        - Success: Boolean indicating whether the operation succeeded
        - Error: Error message if operation failed
        - Data: Object containing Version, ReleaseHash, and HasChanges properties
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$BuildConfiguration,
        [Parameter(Mandatory = $false)]
        [string[]]$Authors = @(),
        [Parameter(Mandatory = $false)]
        [string]$CommitMessage = "[bot][skip ci] Update Metadata"
    )

    try {
        Write-Information "Generating version information..." -Tags "Update-ProjectMetadata"
        $version = New-Version -CommitHash $BuildConfiguration.ReleaseHash
        Write-Information "Version: $version" -Tags "Update-ProjectMetadata"

        Write-Information "Generating license..." -Tags "Update-ProjectMetadata"
        New-License -ServerUrl $BuildConfiguration.ServerUrl -Owner $BuildConfiguration.GitHubOwner -Repository $BuildConfiguration.GitHubRepo | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Generating changelog..." -Tags "Update-ProjectMetadata"
        # Generate both full changelog and latest version changelog
        New-Changelog -Version $version -CommitHash $BuildConfiguration.ReleaseHash -LatestChangelogPath $BuildConfiguration.LatestChangelogFile | Write-InformationStream -Tags "Update-ProjectMetadata"

        # Create AUTHORS.md if authors are provided
        if ($Authors.Count -gt 0) {
            Write-Information "Generating authors file..." -Tags "Update-ProjectMetadata"
            $authorsContent = "# Project Authors$script:lineEnding$script:lineEnding"
            foreach ($author in $Authors) {
                $authorsContent += "* $author$script:lineEnding"
            }
            [System.IO.File]::WriteAllText("AUTHORS.md", $authorsContent, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"
        }

        # Create AUTHORS.url
        $authorsUrl = "[InternetShortcut]$($script:lineEnding)URL=$($BuildConfiguration.ServerUrl)/$($BuildConfiguration.GitHubOwner)"
        [System.IO.File]::WriteAllText("AUTHORS.url", $authorsUrl, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"

        # Create PROJECT_URL.url
        $projectUrl = "[InternetShortcut]$($script:lineEnding)URL=$($BuildConfiguration.ServerUrl)/$($BuildConfiguration.GitHubOwner)/$($BuildConfiguration.GitHubRepo)"
        [System.IO.File]::WriteAllText("PROJECT_URL.url", $projectUrl, [System.Text.UTF8Encoding]::new($false)) | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Adding files to git..." -Tags "Update-ProjectMetadata"
        $filesToAdd = @(
            "VERSION.md",
            "LICENSE.md",
            "AUTHORS.md",
            "CHANGELOG.md",
            "COPYRIGHT.md",
            "PROJECT_URL.url",
            "AUTHORS.url"
        )
        Write-Information "Files to add: $($filesToAdd -join ", ")" -Tags "Update-ProjectMetadata"
        "git add $filesToAdd" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

        Write-Information "Checking for changes to commit..." -Tags "Update-ProjectMetadata"
        $postStatus = "git status --porcelain" | Invoke-ExpressionWithLogging
        Write-Information "Git status: $($postStatus ? 'Changes detected' : 'No changes')" -Tags "Update-ProjectMetadata"

        # Get the current commit hash regardless of whether we make changes
        $currentHash = "git rev-parse HEAD" | Invoke-ExpressionWithLogging
        Write-Information "Current commit hash: $currentHash" -Tags "Update-ProjectMetadata"

        if (-not [string]::IsNullOrWhiteSpace($postStatus)) {
            # Configure git user before committing
            Set-GitIdentity | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Committing changes..." -Tags "Update-ProjectMetadata"
            "git commit -m `"$CommitMessage`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Pushing changes..." -Tags "Update-ProjectMetadata"
            "git push" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Update-ProjectMetadata"

            Write-Information "Getting release hash..." -Tags "Update-ProjectMetadata"
            $releaseHash = "git rev-parse HEAD" | Invoke-ExpressionWithLogging
            Write-Information "Metadata committed as $releaseHash" -Tags "Update-ProjectMetadata"

            Write-Information "Metadata update completed successfully with changes" -Tags "Update-ProjectMetadata"
            Write-Information "Version: $version" -Tags "Update-ProjectMetadata"
            Write-Information "Release Hash: $releaseHash" -Tags "Update-ProjectMetadata"

            return [PSCustomObject]@{
                Success = $true
                Error = ""
                Data = [PSCustomObject]@{
                    Version = $version
                    ReleaseHash = $releaseHash
                    HasChanges = $true
                }
            }
        }
        else {
            Write-Information "No changes to commit" -Tags "Update-ProjectMetadata"
            Write-Information "Version: $version" -Tags "Update-ProjectMetadata"
            Write-Information "Using current commit hash: $currentHash" -Tags "Update-ProjectMetadata"

            return [PSCustomObject]@{
                Success = $true
                Error = ""
                Data = [PSCustomObject]@{
                    Version = $version
                    ReleaseHash = $currentHash
                    HasChanges = $false
                }
            }
        }
    }
    catch {
        $errorMessage = $_.ToString()
        Write-Information "Failed to update metadata: $errorMessage" -Tags "Update-ProjectMetadata"
        return [PSCustomObject]@{
            Success = $false
            Error = $errorMessage
            Data = [PSCustomObject]@{
                Version = $null
                ReleaseHash = $null
                HasChanges = $false
                StackTrace = $_.ScriptStackTrace
            }
        }
    }
}

#endregion

#region Build Operations

function Invoke-DotNetRestore {
    <#
    .SYNOPSIS
        Restores NuGet packages for .NET projects.
    .DESCRIPTION
        Executes 'dotnet restore' with locked mode to ensure consistent package restoration
        across build environments. This function:

        1. Uses locked mode to ensure package consistency
        2. Configures the logger for appropriate verbosity
        3. Verifies the exit code to ensure success

        This should be called before any build operations to ensure dependencies are available.
    .EXAMPLE
        Invoke-DotNetRestore
        # Restores all NuGet packages for the solution in the current directory
    .NOTES
        Uses Assert-LastExitCode to verify the operation succeeded.
    #>
    [CmdletBinding()]
    param()

    Write-StepHeader "Restoring Dependencies" -Tags "Invoke-DotNetRestore"

    # Execute command and stream output directly to console
    "dotnet restore --locked-mode -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetRestore"
    Assert-LastExitCode "Restore failed"
}

function Invoke-DotNetBuild {
    <#
    .SYNOPSIS
        Builds the .NET solution with specified configuration.
    .DESCRIPTION
        Executes 'dotnet build' with the specified configuration and build arguments.
        This function:

        1. Builds the solution with optimized settings for CI environments
        2. Uses incremental build with configurable verbosity
        3. Handles build failures with detailed error reporting
        4. Automatically retries with higher verbosity on failure to provide diagnostics

        The function assumes packages have been restored already (uses --no-restore).
    .PARAMETER Configuration
        The build configuration to use (Debug/Release). Defaults to "Release".
    .PARAMETER BuildArgs
        Additional build arguments to pass to the dotnet build command.
    .EXAMPLE
        Invoke-DotNetBuild -Configuration "Release"
        # Builds the solution in Release mode
    .EXAMPLE
        Invoke-DotNetBuild -Configuration "Debug" -BuildArgs "--nologo"
        # Builds in Debug mode with additional arguments
    .NOTES
        If the build fails initially, the function will retry with more detailed output
        to help diagnose the issue.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [string]$BuildArgs = ""
    )

    Write-StepHeader "Building Solution" -Tags "Invoke-DotNetBuild"

    try {
        # First attempt with quiet verbosity - stream output directly
        "dotnet build --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-incremental $BuildArgs --no-restore" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetBuild"

        if ($LASTEXITCODE -ne 0) {
            Write-Information "Build failed with exit code $LASTEXITCODE. Retrying with detailed verbosity..." -Tags "Invoke-DotNetBuild"

            # Retry with more detailed verbosity - stream output directly
            "dotnet build --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-incremental $BuildArgs --no-restore" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetBuild"

            # Still failed, show diagnostic info and throw error
            if ($LASTEXITCODE -ne 0) {
                Write-Information "Checking for common build issues:" -Tags "Invoke-DotNetBuild"

                # Check for project files
                $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj)
                Write-Information "Found $($projectFiles.Count) project files" -Tags "Invoke-DotNetBuild"

                foreach ($proj in $projectFiles) {
                    Write-Information "  - $($proj.FullName)" -Tags "Invoke-DotNetBuild"
                }

                Assert-LastExitCode "Build failed"
            }
        }
    }
    catch {
        Write-Information "Exception during build process: $_" -Tags "Invoke-DotNetBuild"
        throw
    }
}

function Invoke-DotNetTest {
    <#
    .SYNOPSIS
        Runs unit tests with code coverage collection.
    .DESCRIPTION
        Executes 'dotnet test' for all test projects in the solution with code coverage enabled.
        This function:

        1. Runs tests using the specified build configuration
        2. Collects code coverage metrics using the XPlat Code Coverage collector
        3. Outputs test results to the specified directory
        4. Optimizes for CI environments with appropriate settings

        The function assumes the build has already been completed (uses --no-build).
    .PARAMETER Configuration
        The build configuration to use for tests (Debug/Release). Defaults to "Release".
    .PARAMETER CoverageOutputPath
        The path to output code coverage results. Defaults to "coverage".
    .EXAMPLE
        Invoke-DotNetTest -Configuration "Release" -CoverageOutputPath "./testresults/coverage"
        # Runs tests in Release mode with coverage output to the specified directory
    .NOTES
        Uses Assert-LastExitCode to verify all tests passed successfully.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [string]$CoverageOutputPath = "coverage"
    )

    Write-StepHeader "Running Tests" -Tags "Invoke-DotNetTest"

    # Execute command and stream output directly to console
    "dotnet test -m:1 --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --collect:`"XPlat Code Coverage`" --results-directory $CoverageOutputPath" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetTest"
    Assert-LastExitCode "Tests failed"
}

function Invoke-DotNetPack {
    <#
    .SYNOPSIS
        Creates NuGet packages from .NET projects.
    .DESCRIPTION
        Executes 'dotnet pack' to create NuGet packages (.nupkg) and symbol packages (.snupkg).
        This function:

        1. Creates output directory if it doesn't exist
        2. Packages either all projects or a specific project
        3. Provides detailed logging on failure to assist with troubleshooting
        4. Reports on successfully created packages

        The function assumes the build has already been completed (uses --no-build).
    .PARAMETER Configuration
        The build configuration to use (Debug/Release). Defaults to "Release".
    .PARAMETER OutputPath
        The path to output NuGet packages to. This path must be provided.
    .PARAMETER Project
        Optional specific project to package. If not provided, all projects are packaged.
    .EXAMPLE
        Invoke-DotNetPack -Configuration "Release" -OutputPath "./packages"
        # Packages all projects in the solution
    .EXAMPLE
        Invoke-DotNetPack -OutputPath "./packages" -Project "./src/MyLibrary/MyLibrary.csproj"
        # Packages only the specified project
    .NOTES
        Projects must be properly configured with package metadata in the .csproj files
        for packaging to succeed.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [string]$OutputPath,
        [string]$Project = ""
    )

    Write-StepHeader "Packaging Libraries" -Tags "Invoke-DotNetPack"

    # Ensure output directory exists
    New-Item -Path $OutputPath -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPack"

    # Check if any projects exist
    $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj -ErrorAction SilentlyContinue)
    if ($projectFiles.Count -eq 0) {
        Write-Information "No .NET library projects found to package" -Tags "Invoke-DotNetPack"
        return
    }

    try {
        # Build either a specific project or all projects
        if ([string]::IsNullOrWhiteSpace($Project)) {
            Write-Information "Packaging all projects in solution..." -Tags "Invoke-DotNetPack"
            "dotnet pack --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --output $OutputPath" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"
        } else {
            Write-Information "Packaging project: $Project" -Tags "Invoke-DotNetPack"
            "dotnet pack $Project --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`" --no-build --output $OutputPath" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"
        }

        if ($LASTEXITCODE -ne 0) {
            # Get more details about what might have failed
            Write-Information "Packaging failed with exit code $LASTEXITCODE, trying again with detailed verbosity..." -Tags "Invoke-DotNetPack"
            "dotnet pack --configuration $Configuration -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=detailed`" --no-build --output $OutputPath" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPack"
            throw "Library packaging failed with exit code $LASTEXITCODE"
        }

        # Report on created packages
        $packages = @(Get-ChildItem -Path $OutputPath -Filter *.nupkg -ErrorAction SilentlyContinue)
        if ($packages.Count -gt 0) {
            Write-Information "Created $($packages.Count) packages in $OutputPath" -Tags "Invoke-DotNetPack"
            foreach ($package in $packages) {
                Write-Information "  - $($package.Name)" -Tags "Invoke-DotNetPack"
            }
        } else {
            Write-Information "No packages were created (projects may not be configured for packaging)" -Tags "Invoke-DotNetPack"
        }
    }
    catch {
        $originalException = $_.Exception
        Write-Information "Package creation failed: $originalException" -Tags "Invoke-DotNetPack"
        throw "Library packaging failed: $originalException"
    }
}

function Invoke-DotNetPublish {
    <#
    .SYNOPSIS
        Publishes .NET applications and creates distribution archives.
    .DESCRIPTION
        Executes 'dotnet publish' for all executable projects and creates zip archives for distribution.
        This function:

        1. Identifies all .csproj files in the solution
        2. Publishes each project to its own output directory
        3. Creates versioned zip archives for each published application
        4. Provides detailed reporting on the publishing process

        The function assumes the build has already been completed (uses --no-build).
    .PARAMETER Configuration
        The build configuration to use (Debug/Release). Defaults to "Release".
    .PARAMETER BuildConfiguration
        The build configuration object containing output paths, version, and other settings.
        This object should be obtained from Get-BuildConfiguration.
    .EXAMPLE
        Invoke-DotNetPublish -Configuration "Release" -BuildConfiguration $buildConfig
        # Publishes all executable projects and creates zip archives
    .NOTES
        Projects that aren't configured as executables will be detected and skipped automatically.
        The zip archives include the version number from the build configuration.
    #>
    [CmdletBinding()]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    Write-StepHeader "Publishing Applications" -Tags "Invoke-DotNetPublish"

    # Find all projects
    $projectFiles = @(Get-ChildItem -Recurse -Filter *.csproj -ErrorAction SilentlyContinue)
    if ($projectFiles.Count -eq 0) {
        Write-Information "No .NET application projects found to publish" -Tags "Invoke-DotNetPublish"
        return
    }

    # Clean output directory if it exists
    if (Test-Path $BuildConfiguration.OutputPath) {
        Remove-Item -Recurse -Force $BuildConfiguration.OutputPath | Write-InformationStream -Tags "Invoke-DotNetPublish"
    }

    # Ensure staging directory exists
    New-Item -Path $BuildConfiguration.StagingPath -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"

    $publishedCount = 0
    foreach ($csproj in $projectFiles) {
        $projName = [System.IO.Path]::GetFileNameWithoutExtension($csproj)
        $outDir = Join-Path $BuildConfiguration.OutputPath $projName
        $stageFile = Join-Path $BuildConfiguration.StagingPath "$projName-$($BuildConfiguration.Version).zip"

        Write-Information "Publishing $projName..." -Tags "Invoke-DotNetPublish"

        # Create output directory
        New-Item -Path $outDir -ItemType Directory -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"

        # Publish application - stream output directly
        "dotnet publish $csproj --no-build --configuration $Configuration --framework net$($BuildConfiguration.DotnetVersion) --output $outDir  -logger:`"Microsoft.Build.Logging.ConsoleLogger,Microsoft.Build;Summary;ForceNoAlign;ShowTimestamp;ShowCommandLine;Verbosity=quiet`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-DotNetPublish"

        if ($LASTEXITCODE -eq 0) {
            # Create zip archive
            Compress-Archive -Path "$outDir/*" -DestinationPath $stageFile -Force | Write-InformationStream -Tags "Invoke-DotNetPublish"
            $publishedCount++
            Write-Information "Successfully published and archived $projName" -Tags "Invoke-DotNetPublish"
        } else {
            Write-Information "Skipping $projName (not configured as an executable project)" -Tags "Invoke-DotNetPublish"
            continue
        }
    }

    if ($publishedCount -gt 0) {
        Write-Information "Published $publishedCount application(s)" -Tags "Invoke-DotNetPublish"
    } else {
        Write-Information "No applications were published (projects may not be configured as executables)" -Tags "Invoke-DotNetPublish"
    }
}

#endregion

#region Publishing and Release

function Invoke-NuGetPublish {
    <#
    .SYNOPSIS
        Publishes NuGet packages to GitHub Packages and NuGet.org.
    .DESCRIPTION
        Publishes all NuGet packages (.nupkg) found in the specified location to both
        GitHub Packages and NuGet.org repositories. This function:

        1. Locates all packages matching the pattern in the build configuration
        2. First publishes to GitHub Packages using the GitHub token
        3. Then publishes to NuGet.org using the provided API key
        4. Uses --skip-duplicate to avoid errors when packages already exist
        5. Reports success or failure for each repository

        Requires appropriate authentication tokens to be provided in the build configuration.
    .PARAMETER BuildConfiguration
        The build configuration object containing package patterns, GitHub token, and NuGet API key.
        This object should be obtained from Get-BuildConfiguration.
    .EXAMPLE
        Invoke-NuGetPublish -BuildConfiguration $buildConfig
        # Publishes all NuGet packages in the staging directory to GitHub Packages and NuGet.org
    .NOTES
        Requires a valid GitHub token and NuGet API key to be provided in the build configuration.
        The function will fail if packages exist but tokens are invalid or missing.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    # Check if there are any packages to publish
    $packages = @(Get-Item -Path $BuildConfiguration.PackagePattern -ErrorAction SilentlyContinue)
    if ($packages.Count -eq 0) {
        Write-Information "No packages found to publish" -Tags "Invoke-NuGetPublish"
        return
    }

    Write-Information "Found $($packages.Count) package(s) to publish" -Tags "Invoke-NuGetPublish"

    Write-StepHeader "Publishing to GitHub Packages" -Tags "Invoke-NuGetPublish"

    # Execute the command and stream output
    "dotnet nuget push `"$($BuildConfiguration.PackagePattern)`" --api-key `"$($BuildConfiguration.GithubToken)`" --source `"https://nuget.pkg.github.com/$($BuildConfiguration.GithubOwner)/index.json`" --skip-duplicate" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-NuGetPublish"
    Assert-LastExitCode "GitHub package publish failed"

    Write-StepHeader "Publishing to NuGet.org" -Tags "Invoke-NuGetPublish"

    # Execute the command and stream output
    "dotnet nuget push `"$($BuildConfiguration.PackagePattern)`" --api-key `"$($BuildConfiguration.NuGetApiKey)`" --source `"https://api.nuget.org/v3/index.json`" --skip-duplicate" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Invoke-NuGetPublish"
    Assert-LastExitCode "NuGet.org package publish failed"
}

function New-GitHubRelease {
    <#
    .SYNOPSIS
        Creates a new GitHub release with assets.
    .DESCRIPTION
        Creates a complete GitHub release including:

        1. Creating and pushing a new Git tag for the version
        2. Creating a GitHub release associated with the tag
        3. Generating release notes from the changelog
        4. Uploading all specified release assets (NuGet packages, application archives, etc.)

        Uses the GitHub CLI (gh) to perform all operations, requiring the GH_TOKEN environment
        variable to be set with an appropriate GitHub token.
    .PARAMETER BuildConfiguration
        The build configuration object containing version, commit hash, GitHub token, and asset patterns.
        This object should be obtained from Get-BuildConfiguration.
    .EXAMPLE
        New-GitHubRelease -BuildConfiguration $buildConfig
        # Creates a GitHub release for the version in the build config, with all specified assets
    .NOTES
        Requires the GitHub CLI (gh) to be installed and properly authenticated.
        The GitHub token must have appropriate permissions to create releases.

        Release notes are generated from either:
        1. The LATEST_CHANGELOG.md file (preferred)
        2. The full CHANGELOG.md file (fallback)

        Additionally, GitHub's automatic release notes are also included.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    # Set GitHub token for CLI
    $env:GH_TOKEN = $BuildConfiguration.GithubToken

    # Configure git user
    Set-GitIdentity | Write-InformationStream -Tags "New-GitHubRelease"

    # Create and push the tag first
    Write-Information "Creating and pushing tag v$($BuildConfiguration.Version)..." -Tags "New-GitHubRelease"
    "git tag -a `"v$($BuildConfiguration.Version)`" `"$($BuildConfiguration.ReleaseHash)`" -m `"Release v$($BuildConfiguration.Version)`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to create git tag"

    "git push origin `"v$($BuildConfiguration.Version)`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to push git tag"

    # Collect all assets
    $assets = @()
    foreach ($pattern in $BuildConfiguration.AssetPatterns) {
        $matched = Get-Item -Path $pattern -ErrorAction SilentlyContinue
        if ($matched) {
            $assets += $matched.FullName
        }
    }

    # Create release
    Write-StepHeader "Creating GitHub Release v$($BuildConfiguration.Version)" -Tags "New-GitHubRelease"

    $releaseArgs = @(
        "release",
        "create",
        "v$($BuildConfiguration.Version)"
    )

    # Add target commit
    $releaseArgs += "--target"
    $releaseArgs += $BuildConfiguration.ReleaseHash.ToString()

    # Add notes generation
    $releaseArgs += "--generate-notes"

    # First check for latest changelog file (preferred for releases)
    $latestChangelogPath = "LATEST_CHANGELOG.md"
    if (Test-Path $latestChangelogPath) {
        Write-Information "Using latest version changelog from $latestChangelogPath" -Tags "New-GitHubRelease"
        $releaseArgs += "--notes-file"
        $releaseArgs += $latestChangelogPath
    }
    # Fall back to full changelog if specified in config and latest not found
    elseif (Test-Path $BuildConfiguration.ChangelogFile) {
        Write-Information "Using full changelog from $($BuildConfiguration.ChangelogFile)" -Tags "New-GitHubRelease"
        $releaseArgs += "--notes-file"
        $releaseArgs += $BuildConfiguration.ChangelogFile
    }

    # Add assets as positional arguments
    $releaseArgs += $assets

    # Join the arguments into a single string
    $releaseArgs = $releaseArgs -join ' '

    "gh $releaseArgs" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "New-GitHubRelease"
    Assert-LastExitCode "Failed to create GitHub release"
}

#endregion

#region Utility Functions

function Assert-LastExitCode {
    <#
    .SYNOPSIS
        Verifies that the last command executed successfully.
    .DESCRIPTION
        Critical utility function that checks if the last executed command succeeded.
        If the last exit code is non-zero (indicating failure), this function:

        1. Formats an error message with details about the failure
        2. Logs the error to the information stream
        3. Throws an exception to halt the build process

        This ensures that build failures are caught immediately and not silently ignored.
    .PARAMETER Message
        The error message to display if the exit code check fails.
    .PARAMETER Command
        Optional. The command that was executed, for better error reporting.
    .EXAMPLE
        dotnet build
        Assert-LastExitCode "The build process failed" -Command "dotnet build"
        # Throws an exception if dotnet build returned a non-zero exit code
    .NOTES
        This function is used extensively throughout the module to ensure each step completes successfully.
        It provides a consistent error handling pattern across all operations.
    #>
    [CmdletBinding()]
    param (
        [string]$Message = "Command failed",
        [string]$Command = ""
    )

    if ($LASTEXITCODE -ne 0) {
        $errorDetails = "Exit code: $LASTEXITCODE"
        if (-not [string]::IsNullOrWhiteSpace($Command)) {
            $errorDetails += " | Command: $Command"
        }

        $fullMessage = "$Message$script:lineEnding$errorDetails"
        Write-Information $fullMessage -Tags "Assert-LastExitCode"
        throw $fullMessage
    }
}

function Write-StepHeader {
    <#
    .SYNOPSIS
        Writes a formatted step header to the console.
    .DESCRIPTION
        Creates a visually distinct header for build steps in the console output to improve
        readability of the build process logs. The header is formatted with a standard style
        to make each major step easily identifiable in the build log.
    .PARAMETER Message
        The header message to display.
    .PARAMETER Tags
        Optional array of tags to include in the logging output for filtering and organization.
        Defaults to "Write-StepHeader" if not provided.
    .EXAMPLE
        Write-StepHeader "Restoring Packages"
        # Outputs: "=== Restoring Packages ==="
    .EXAMPLE
        Write-StepHeader "Building Solution" -Tags "Build", "MyProject"
        # Outputs the header with the specified tags for filtering
    .NOTES
        The format uses "===" to clearly delineate steps in the build process.
        Line endings are added before and after for clear visual separation.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Message,
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Write-StepHeader")
    )
    Write-Information "$($script:lineEnding)=== $Message ===$($script:lineEnding)" -Tags $Tags
}

function Test-AnyFiles {
    <#
    .SYNOPSIS
        Tests if any files match the specified pattern.
    .DESCRIPTION
        Utility function that checks if any files exist matching a given glob pattern.
        This function:

        1. Uses Get-Item with error silencing to prevent errors for non-matching patterns
        2. Returns a boolean indicating whether any matching files were found
        3. Handles arrays properly by forcing array wrapping with @()

        This is useful for determining if certain file types exist before attempting operations on them.
    .PARAMETER Pattern
        The glob pattern to check for matching files (e.g., "*.nupkg", "output/*.zip").
    .EXAMPLE
        if (Test-AnyFiles -Pattern "*.nupkg") {
            Write-Host "NuGet packages found!"
        }
    .EXAMPLE
        $hasLogs = Test-AnyFiles -Pattern "logs/*.log"
        # Returns $true if any .log files exist in the logs directory
    .OUTPUTS
        [bool] True if any files match the pattern, false otherwise.
    #>
    [CmdletBinding()]
    [OutputType([bool])]
    param (
        [Parameter(Mandatory=$true)]
        [string]$Pattern
    )

    # Use array subexpression to ensure consistent collection handling
    $matchingFiles = @(Get-Item -Path $Pattern -ErrorAction SilentlyContinue)
    return $matchingFiles.Count -gt 0
}

function Write-InformationStream {
    <#
    .SYNOPSIS
        Streams output to the Information stream with optional tags.
    .DESCRIPTION
        Utility function that writes objects to the information stream with optional filtering tags.
        This function:

        1. Takes input from the pipeline or as a parameter
        2. Writes each object to the information stream
        3. Attaches tags for filtering and categorization

        This provides a consistent way to handle output throughout the module.
    .PARAMETER Object
        The object to write to the information stream.
    .PARAMETER Tags
        Optional array of tags to include in the information output for filtering and organization.
        Defaults to "Write-InformationStream" if not provided.
    .EXAMPLE
        "Build completed" | Write-InformationStream
        # Outputs the string to the information stream with default tag
    .EXAMPLE
        & git status | Write-InformationStream -Tags "Git", "Status"
        # Streams git status output to the information stream with custom tags
    .NOTES
        Uses Write-Information internally, which respects the $InformationPreference setting.
        Tags are useful for filtering output when processing large logs.
    #>
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline=$true, ParameterSetName="Object")]
        [object]$Object,
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Write-InformationStream")
    )

    process {
        # Use array subexpression to ensure consistent collection handling
        $Object | ForEach-Object {
            Write-Information $_ -Tags $Tags
        }
    }
}

function Invoke-ExpressionWithLogging {
    <#
    .SYNOPSIS
        Invokes an expression and logs the command and result.
    .DESCRIPTION
        Executes PowerShell expressions or commands with consistent logging and output handling.
        This function:

        1. Accepts either a ScriptBlock or a string command
        2. Logs the command being executed
        3. Executes the command and returns its output
        4. Properly handles arrays and collections

        This provides a consistent way to execute and log commands throughout the module.
    .PARAMETER ScriptBlock
        The script block to execute.
    .PARAMETER Command
        A string command to execute, which will be converted to a script block.
    .PARAMETER Tags
        Optional tags to include in the logging output for filtering and organization.
        Defaults to "Invoke-ExpressionWithLogging" if not provided.
    .EXAMPLE
        $result = Invoke-ExpressionWithLogging -Command "git status"
        # Executes git status, logs the command, and returns the output
    .EXAMPLE
        $files = Invoke-ExpressionWithLogging { Get-ChildItem -Path "./src" -Recurse }
        # Executes the script block and returns its result
    .OUTPUTS
        The output of the executed command or script block.
    .NOTES
        This function is useful for ensuring consistent logging across all command executions,
        especially when debugging command failures in build scripts.
    #>
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline=$true, ParameterSetName="ScriptBlock")]
        [scriptblock]$ScriptBlock,

        [Parameter(ValueFromPipeline=$true, ParameterSetName="Command")]
        [string]$Command,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]$Tags = @("Invoke-ExpressionWithLogging")
    )

    process {
        # Convert command string to scriptblock if needed
        if ($PSCmdlet.ParameterSetName -eq "Command" -and -not [string]::IsNullOrWhiteSpace($Command)) {
            Write-Information "Executing command: $Command" -Tags $Tags
            $ScriptBlock = [scriptblock]::Create($Command)
        }
        else {
            Write-Information "Executing script block: $ScriptBlock" -Tags $Tags
        }

        if ($ScriptBlock) {
            # Execute the expression and capture its output
            $output = & $ScriptBlock

            # Handle different output types properly
            if ($null -eq $output) {
                # No output - return nothing
                return
            }
            elseif ($output -is [array]) {
                # Array output - return as is
                return $output
            }
            else {
                # Single item - return as single item
                return $output
            }
        }
    }
}

function Get-GitLineEnding {
    <#
    .SYNOPSIS
        Gets the correct line ending based on Git configuration.
    .DESCRIPTION
        Determines the appropriate line ending (LF or CRLF) based on Git configuration settings.
        This function:

        1. Checks git core.eol setting (explicit line ending configuration)
        2. Falls back to core.autocrlf setting (line ending conversion setting)
        3. Uses system default line ending if no Git settings are found

        This ensures consistent line endings in all generated files, respecting the repository's
        line ending configuration.
    .EXAMPLE
        $lineEnding = Get-GitLineEnding
        # Returns "`n" (LF) or "`r`n" (CRLF) based on Git configuration
    .OUTPUTS
        [string] Returns either "`n" for LF or "`r`n" for CRLF line endings.
    .NOTES
        The function follows Git's own line ending resolution logic:
        - core.eol takes precedence if set ('lf' or 'crlf')
        - core.autocrlf is checked next ('true', 'input', or 'false')
        - System default is used as last resort
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $autocrlf = "git config --get core.autocrlf" | Invoke-ExpressionWithLogging
    $eol = "git config --get core.eol" | Invoke-ExpressionWithLogging

    # If core.eol is set, use that
    if ($LASTEXITCODE -eq 0 -and $eol -in @('lf', 'crlf')) {
        return if ($eol -eq 'lf') { "`n" } else { "`r`n" }
    }

    # Otherwise use autocrlf setting
    if ($LASTEXITCODE -eq 0) {
        switch ($autocrlf.ToLower()) {
            'true' { return "`n" }  # Git will convert to CRLF on checkout
            'input' { return "`n" } # Always use LF
            'false' {
                # Use OS default
                return [System.Environment]::NewLine
            }
            default {
                # Default to OS line ending if setting is not recognized
                return [System.Environment]::NewLine
            }
        }
    }

    # If git config fails or no setting found, use OS default
    return [System.Environment]::NewLine
}

function Set-GitIdentity {
    <#
    .SYNOPSIS
        Configures Git user identity for automated operations.
    .DESCRIPTION
        Sets up a standard Git user identity for automated operations like GitHub Actions.
        This function:

        1. Sets the global Git user name to "Github Actions"
        2. Sets the global Git user email to "actions@users.noreply.github.com"
        3. Verifies each operation succeeds

        This ensures that Git operations performed by the module have proper attribution
        and can be easily identified as automated actions.
    .EXAMPLE
        Set-GitIdentity
        # Configures Git with standard GitHub Actions identity
    .NOTES
        This is typically used before performing Git operations like committing changes
        and creating tags during the automated release process.
    #>
    [CmdletBinding()]
    param()

    Write-Information "Configuring git user for GitHub Actions..." -Tags "Set-GitIdentity"
    "git config --global user.name `"Github Actions`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Set-GitIdentity"
    Assert-LastExitCode "Failed to configure git user name"
    "git config --global user.email `"actions@users.noreply.github.com`"" | Invoke-ExpressionWithLogging | Write-InformationStream -Tags "Set-GitIdentity"
    Assert-LastExitCode "Failed to configure git user email"
}

#endregion

#region High-Level Workflows

function Invoke-BuildWorkflow {
    <#
    .SYNOPSIS
        Executes the complete build and test workflow.
    .DESCRIPTION
        High-level function that orchestrates the entire build and test process including:

        1. Initializing the build environment
        2. Installing any required tools (like dotnet-script if needed)
        3. Restoring NuGet packages
        4. Building the solution
        5. Running tests with code coverage

        This provides a single entry point for executing the build portion of the CI/CD pipeline.
    .PARAMETER Configuration
        The build configuration to use (Debug/Release). Defaults to "Release".
    .PARAMETER BuildArgs
        The build configuration (Debug/Release). Defaults to "Release".
    .PARAMETER BuildConfiguration
        The build configuration object from Get-BuildConfiguration.
    .OUTPUTS
        PSCustomObject with Success, Error, and Data properties.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [string]$Configuration = "Release",
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    try {
        Write-StepHeader "Starting Release Process" -Tags "Invoke-ReleaseWorkflow"

        # Package and publish if not skipped
        $packagePaths = @()

        # Create NuGet packages
        try {
            Write-StepHeader "Packaging Libraries" -Tags "Invoke-DotNetPack"
            Invoke-DotNetPack -Configuration $Configuration -OutputPath $BuildConfiguration.StagingPath | Write-InformationStream -Tags "Invoke-DotNetPack"

            # Add package paths if they exist
            if (Test-Path $BuildConfiguration.PackagePattern) {
                $packagePaths += $BuildConfiguration.PackagePattern
            }
            if (Test-Path $BuildConfiguration.SymbolsPattern) {
                $packagePaths += $BuildConfiguration.SymbolsPattern
            }
        }
        catch {
            Write-Information "Library packaging failed: $_" -Tags "Invoke-DotNetPack"
            Write-Information "Continuing with release process without NuGet packages." -Tags "Invoke-DotNetPack"
        }

        # Create application packages
        try {
            Invoke-DotNetPublish -Configuration $Configuration -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "Invoke-DotNetPublish"

            # Add application paths if they exist
            if (Test-Path $BuildConfiguration.ApplicationPattern) {
                $packagePaths += $BuildConfiguration.ApplicationPattern
            }
        }
        catch {
            Write-Information "Application publishing failed: $_" -Tags "Invoke-DotNetPublish"
            Write-Information "Continuing with release process without application packages." -Tags "Invoke-DotNetPublish"
        }

        # Publish packages if we have any and NuGet key is provided
        $packages = @(Get-Item -Path $BuildConfiguration.PackagePattern -ErrorAction SilentlyContinue)
        if ($packages.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($BuildConfiguration.NuGetApiKey)) {
            Write-StepHeader "Publishing NuGet Packages" -Tags "Invoke-NuGetPublish"
            try {
                Invoke-NuGetPublish -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "Invoke-NuGetPublish"
            }
            catch {
                Write-Information "NuGet package publishing failed: $_" -Tags "Invoke-NuGetPublish"
                Write-Information "Continuing with release process." -Tags "Invoke-NuGetPublish"
            }
        }

        # Create GitHub release
        Write-StepHeader "Creating GitHub Release" -Tags "New-GitHubRelease"
        Write-Information "Creating release for version $($BuildConfiguration.Version)..." -Tags "New-GitHubRelease"
        New-GitHubRelease -BuildConfiguration $BuildConfiguration | Write-InformationStream -Tags "New-GitHubRelease"

        Write-StepHeader "Release Process Completed" -Tags "Invoke-ReleaseWorkflow"
        Write-Information "Release process completed successfully!" -Tags "Invoke-ReleaseWorkflow"
        return [PSCustomObject]@{
            Success = $true
            Error = ""
            Data = [PSCustomObject]@{
                Version = $BuildConfiguration.Version
                ReleaseHash = $BuildConfiguration.ReleaseHash
                PackagePaths = $packagePaths
            }
        }
    }
    catch {
        Write-Information "Release workflow failed: $_" -Tags "Invoke-ReleaseWorkflow"
        return [PSCustomObject]@{
            Success = $false
            Error = $_.ToString()
            Data = [PSCustomObject]@{
                ErrorDetails = $_.Exception.Message
                PackagePaths = @()
            }
            StackTrace = $_.ScriptStackTrace
        }
    }
}

function Invoke-CIPipeline {
    <#
    .SYNOPSIS
        Executes the CI/CD pipeline.
    .DESCRIPTION
        Executes the CI/CD pipeline, including metadata updates and build workflow.
    .PARAMETER BuildConfiguration
        The build configuration to use.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]$BuildConfiguration
    )

    Write-Information "BuildConfiguration: $($BuildConfiguration | ConvertTo-Json -Depth 10)" -Tags "Invoke-CIPipeline"

    try {
        Write-Information "Updating metadata..." -Tags "Invoke-CIPipeline"
        $metadata = Update-ProjectMetadata `
            -BuildConfiguration $BuildConfiguration

        if ($null -eq $metadata) {
            Write-Information "Metadata update returned null" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Metadata update returned null"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "Metadata: $($metadata | ConvertTo-Json -Depth 10)" -Tags "Invoke-CIPipeline"

        $BuildConfiguration.Version = $metadata.Data.Version
        $BuildConfiguration.ReleaseHash = $metadata.Data.ReleaseHash

        if (-not $metadata.Success) {
            Write-Information "Failed to update metadata: $($metadata.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Failed to update metadata: $($metadata.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "Running build workflow..." -Tags "Invoke-CIPipeline"
        $result = Invoke-BuildWorkflow -BuildConfiguration $BuildConfiguration
        if (-not $result.Success) {
            Write-Information "Build workflow failed: $($result.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Build workflow failed: $($result.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "Running release workflow..." -Tags "Invoke-CIPipeline"
        $result = Invoke-ReleaseWorkflow -BuildConfiguration $BuildConfiguration
        if (-not $result.Success) {
            Write-Information "Release workflow failed: $($result.Error)" -Tags "Invoke-CIPipeline"
            return [PSCustomObject]@{
                Success = $false
                Error = "Release workflow failed: $($result.Error)"
                StackTrace = $_.ScriptStackTrace
            }
        }

        Write-Information "CI/CD pipeline completed successfully" -Tags "Invoke-CIPipeline"
        return [PSCustomObject]@{
            Success = $true
            Version = $metadata.Data.Version
            ReleaseHash = $metadata.Data.ReleaseHash
        }
    }
    catch {
        Write-Information "CI/CD pipeline failed: $_" -Tags "Invoke-CIPipeline"
        return [PSCustomObject]@{
            Success = $false
            Error = "CI/CD pipeline failed: $_"
            StackTrace = $_.ScriptStackTrace
        }
    }
}

#endregion

#region Module Variables
$script:DOTNET_VERSION = '9.0'
$script:LICENSE_TEMPLATE = Join-Path $PSScriptRoot "LICENSE.template"
