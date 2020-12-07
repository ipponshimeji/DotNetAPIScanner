$gacDir = "$env:windir\Microsoft.NET\assembly"

function ListAssembly([string]$name, [System.IO.FileSystemInfo]$dirInfo) {
	$parts = $dirInfo.Name -split '_'
	[PSCustomObject]@{
		Name = $name
		Target = $parts[0]
		Version = $parts[1]
		PublicKeyToken = $parts[3]
	}
}

function ListAssemblies([string]$dir) {
	Get-ChildItem $dir | ForEach-Object { $name = $_.Name; Get-ChildItem $_.FullName | ForEach-Object { ListAssembly $name $_ } }
}

@(
	'GAC_MSIL';
	'GAC_64'
) | ForEach-Object { ListAssemblies("$gacDir\$_") }
