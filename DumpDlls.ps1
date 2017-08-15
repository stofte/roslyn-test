# Runs ProccessMonitor to capture all dlls accessed by dotnet.exe when building solution.
# Provides a starting point for MetadataReferences required when building using Roslyn.
dotnet clean ConsoleApp1 -nologo
start-process "procmon.exe" -ArgumentList "/quiet","/minimized","/loadconfig procmon-conf.pmc","/backingfile data.pml"
start-process "procmon.exe" -ArgumentList "/waitforidle" -wait
dotnet build ConsoleApp1
start-process "procmon.exe" -ArgumentList "/terminate" -wait
start-process "procmon.exe" -ArgumentList "/openlog","data.pml","/saveapplyfilter","/saveas data.csv" -wait
remove-item (Get-Item *.pml) # might generate more then one pml file
$NewCsv = (Get-Date -Format s).Replace(":", "").Replace("-","") + ".csv"
$NewJson = (Get-Date -Format s).Replace(":", "").Replace("-","") + "-win.json"
rename-item (Get-Item data.csv) $NewCsv
node process-csv.js $NewCsv > $NewJson
