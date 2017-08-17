# Runs ProccessMonitor to capture all dlls accessed by dotnet.exe when building solution.
# Provides a starting point for MetadataReferences required when building using Roslyn.
write-host "Starting clean"
dotnet clean ConsoleApp1
write-host "Starting restore"
dotnet restore ConsoleApp1
write-host "Starting procmon"
start-process "procmon.exe" -ArgumentList "/accepteula","/quiet","/minimized","/loadconfig procmon-conf.pmc","/backingfile data.pml"
start-process "procmon.exe" -ArgumentList "/accepteula","/waitforidle" -wait
write-host "Starting build"
dotnet build ConsoleApp1
start-process "procmon.exe" -ArgumentList "/accepteula","/terminate" -wait
start-process "procmon.exe" -ArgumentList "/accepteula","/openlog","data.pml","/saveapplyfilter","/saveas data.csv" -wait
write-host "Starting node"
node process-raw.js data.csv > ConsoleApp1\ConsoleApp1\dlls.json
# remove-item (Get-Item data.csv)
remove-item (Get-Item *.pml) # might generate more then one pml file
