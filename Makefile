Q=$(if $(V),,@)

all:: build

# To prevent bootstrapping issues we check in src/VinylCutter/ParserRecords.g.cs
regenerate::
	$(Q) dotnet run --project src/VinylCutter.csproj src/ParserRecords.rcs --output src/VinylCutter/

build::
	$(Q) dotnet build -nologo /v:q

build-release::
	$(Q) dotnet build -c Release -nologo /v:q

test:: 
	$(Q) dotnet test -nologo /v:q test/VinylCutter.Test.csproj

test-fast:: build
	$(Q) dotnet test --no-build -nologo /v:q --filter `basename $(TEST_FILES) | sed 's/\.[^.]*$$//'` test/VinylCutter.Test.csproj
