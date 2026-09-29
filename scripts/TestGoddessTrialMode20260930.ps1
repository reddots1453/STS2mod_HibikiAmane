param(
    [string]$ProjectDir = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$rules = Join-Path $ProjectDir 'src/Acts/ActAlignmentChoiceRules.cs'
Add-Type -Path $rules

$resolved = [System.Collections.Generic.HashSet[int]]::new()
function Assert-Choice([bool]$expected, [bool]$actual, [string]$caseName) {
    if ($expected -ne $actual) { throw "Goddess trial mode regression: $caseName" }
}

Assert-Choice $false ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(0, $resolved)) 'Neow has no alternate choice'
Assert-Choice $true ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(1, $resolved)) 'Act 2 choice'
Assert-Choice $true ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(2, $resolved)) 'Act 3 choice'
Assert-Choice $false ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(3, $resolved)) 'No Act 4 choice'
$null = $resolved.Add(1)
Assert-Choice $false ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(1, $resolved)) 'Act 2 idempotent'
Assert-Choice $true ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(2, $resolved)) 'Act 3 still pending'
$null = $resolved.Add(2)
Assert-Choice $false ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::Needs(2, $resolved)) 'Act 3 idempotent'
Assert-Choice $true ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::ValidDelta(2)) 'Positive choice'
Assert-Choice $true ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::ValidDelta(-2)) 'Negative choice'
Assert-Choice $false ([MaidenSuccubus.Acts.ActAlignmentChoiceRules]::ValidDelta(0)) 'Reject zero change'

Write-Host 'Goddess trial mode rules: 10/10 passed.'
