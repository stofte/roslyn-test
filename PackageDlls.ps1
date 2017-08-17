$files = get-content $args[0] | convertfrom-json
$dllFolder = Resolve-Path -Path ([System.IO.Path]::GetDirectoryName($args[0]))
$distFolderName = "nuget-redist";
$distFolder = [System.IO.Path]::Combine($dllFolder, $distFolderName);
$redistDllsJsonFileName = [System.IO.Path]::Combine($dllFolder, "dlls-redist.json");
write-host "$redistDllsJsonFileName"
$publishedFiles = [System.Collections.ArrayList]@()
$missingFiles = [System.Collections.ArrayList]@()
# contains the merged map with missing dlls in a redist folder
$redistDlls = [System.Collections.ArrayList]@()
foreach($file in $files) {
    if ([System.IO.File]::Exists($file)) { 
        $fileName = [System.IO.Path]::GetFileName($file)
        $published = Join-Path -Path $dllFolder -ChildPath $fileName
        if ([System.IO.File]::Exists($published)) {
            $fcout = fc.exe $file $published;
            if ($fcout.Contains("FC: no differences encountered")) {
                $publishedFiles.Add("$fileName");
            } else {
                $missingFiles.Add("$file");
            }
        }
    } else {
         # write-host "Accessed file not found: $file"
    }
}
write-host "Found: $($publishedFiles.Count)"
write-host "Missing: $($missingFiles.Count)"
new-item $distFolder -itemtype directory -force

foreach($file in $missingFiles) {
    $fileName = [System.IO.Path]::GetFileName($file)
    $copyfile = [System.IO.Path]::Combine($dllFolder, $distFolder, $fileName)
    copy $file $copyfile;
    $publishedFiles.Add([System.IO.Path]::Combine($distFolderName, $fileName))
}

write-host "Total dlls: $($publishedFiles.Count)"

convertto-json $publishedFiles | out-file $redistDllsJsonFileName -encoding utf8
