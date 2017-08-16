dotnet clean ConsoleApp1
sudo auditctl -w ~/.nuget/packages -k nuget
# timestamp to filter audit events
timestamp=`date -d "-10 seconds" +%H:%M:%S`
dotnet build ConsoleApp1
sudo ausearch -ts $timestamp -k nuget | aureport -f -i > data.log
