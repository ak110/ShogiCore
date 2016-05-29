
update:
	git pull
	git submodule update
	$(MAKE) build

build:
	mono .nuget/NuGet.exe restore ShogiCore.sln || (echo "Run [sudo make pre-build]" ; exit 1)
	chmod +x packages/xunit.runners.1.9.2/tools/xunit.console.clr4.exe
	xbuild /p:Configuration=Debug ShogiCore.sln

pre-build:
	mozroots --import --machine --sync
	yes | certmgr -ssl -m https://go.microsoft.com
	yes | certmgr -ssl -m https://nugetgallery.blob.core.windows.net
	yes | certmgr -ssl -m https://nuget.org

clean:
	xbuild /t:Clean

.PHONY: update build pre-build clean

