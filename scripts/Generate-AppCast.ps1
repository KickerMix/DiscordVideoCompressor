param(
    [Parameter(Mandatory = $true)]
    [string]$BinaryDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ChangeLogDirectory,

    [Parameter(Mandatory = $true)]
    [string]$DownloadUrl,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseNotesUrl
)

$ErrorActionPreference = "Stop"

netsparkle-generate-appcast `
  -b $BinaryDirectory `
  -p $ChangeLogDirectory `
  -u $DownloadUrl `
  -l $ReleaseNotesUrl
