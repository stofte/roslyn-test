dotnet clean ConsoleApp1
sudo auditctl -l
sudo auditctl -w /home/svend/.nuget/packages -k nuget
sleep 5
dotnet build ConsoleApp1
sleep 5
sudo ausearch -k nuget | aureport -f -i > data.log
