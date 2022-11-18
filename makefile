test:
	dotnet watch --project tests/ test
	
pack:
	dotnet pack -o $(NUGET_LOCAL)

publish: pack
	dotnet nuget push $(NUGET_LOCAL)/FsCli.1.0.3.nupkg --api-key $(NUGET_KEY) --source https://api.nuget.org/v3/index.json --interactive