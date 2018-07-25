Q=$(if $(V),,@)

BUILD = msbuild /v:quiet /nologo
TEST_FILTER = --noresult  | grep "Test Files" -A 9999 | grep "Run Settings" -B 9999 | sed '1d;$$d'

NUNIT = mono ./packages/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe --noheader --noresult --labels=before
BRITTLE_BUILD = mono /Library/Frameworks/Mono.framework/Versions/5.10.1/lib/mono/4.5/csc.exe /define:DEBUG /debug+ /debug:portable /optimize- /target:library /nologo /utf8output

all:: build

dist:: build-release
	$(Q) rm -r ./dist/
	$(Q) mkdir -p dist/bin
	$(Q) cp -R src/VinylCutter/bin/Release/ dist/bin/
	$(Q) cp data/VinylCutter dist/
	$(Q) chmod +x dist/VinylCutter

prepare::
	$(Q) /Library/Frameworks/Mono.framework/Versions/Current/Commands/nuget restore VinylCutter.sln	

build::
	$(Q) $(BUILD) src/VinylCutter/VinylCutter.csproj

build-release::
	$(Q) $(BUILD) src/VinylCutter/VinylCutter.csproj /p:Configuration=Release

test:: build
	$(Q) $(BUILD) test/VinylCutter.Tests/VinylCutter.Tests.csproj
	$(Q) $(NUNIT) test/VinylCutter.Tests/bin/Debug/VinylCutter.Tests.dll

TEST_FILES ?= `ls test/VinylCutter.Tests/*.cs`

# Gotta go fast, even if it's brittle
test-fast:: build
	$(Q) mkdir -p test/VinylCutter.Tests/bin/Debug
	$(Q) $(BRITTLE_BUILD) /r:/Library/Frameworks/Mono.framework/Versions/5.10.1/lib/mono/xbuild/Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll /r:packages/Mono.Cecil.0.10.0/lib/net40/Mono.Cecil.dll /r:packages/NUnit.3.10.1/lib/net45/nunit.framework.dll /r:packages/System.Collections.Immutable.1.5.0/lib/netstandard2.0/System.Collections.Immutable.dll /r:src/VinylCutter/bin/Debug/VinylCutter.exe /out:test/VinylCutter.Tests/bin/Debug/VinylCutter.Tests.dll $(TEST_FILES)
	$(Q) cp packages/NUnit.3.10.1/lib/net45/nunit.framework.dll test/VinylCutter.Tests/bin/Debug/nunit.framework.dll
	$(Q) cp packages/Mono.Cecil.0.10.0/lib/net40/Mono.Cecil.dll test/VinylCutter.Tests/bin/Debug/Mono.Cecil.dll
	$(Q) cp packages/System.Collections.Immutable.1.5.0/lib/netstandard2.0/System.Collections.Immutable.dll test/VinylCutter.Tests/bin/Debug/System.Collections.Immutable.dll
	$(Q) cp src/VinylCutter/bin/Debug/VinylCutter.exe test/VinylCutter.Tests/bin/Debug/VinylCutter.exe
	$(Q) $(NUNIT) test/VinylCutter.Tests/bin/Debug/VinylCutter.Tests.dll $(TEST_FILTER)
