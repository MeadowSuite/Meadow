dotnet restore --configfile Nuget.Config
dotnet msbuild -p:Configuration=Debug /maxcpucount:1 /nodeReuse:false
rmdir /s /q "coverage\"
rmdir /s /q "report\"
mkdir "coverage\"
echo {}> "coverage\coverage.json"
dotnet test -c Debug --no-build --no-restore /p:CollectCoverage=true /p:CoverletOutputFormat=\"json,opencover\" /p:CoverletOutput="%cd%\coverage\coverage" /p:MergeWith="%cd%\coverage\coverage.json"
dotnet tool install dotnet-reportgenerator-globaltool --version 4.0.0-rc4 --tool-path reportgenerator
reportgenerator\reportgenerator -reports:coverage\coverage.opencover.xml -targetdir:report -reporttypes:HTML
rmdir /s /q "coverage\"
rmdir /s /q "reportgenerator\"