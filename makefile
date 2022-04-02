test:
	dotnet watch --project tests/ test

publish:
	dotnet pack -o dist
	dotnet nuget push dist/FsCli.1.0.0.nupkg --api-key $(NUGET_KEY) --source https://api.nuget.org/v3/index.json --interactive