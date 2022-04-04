test:
	dotnet watch --project tests/ test
	
pack:
	dotnet pack -o dist

publish: pack
	dotnet nuget push dist/FsCli.1.0.2.nupkg --api-key $(NUGET_KEY) --source https://api.nuget.org/v3/index.json --interactive