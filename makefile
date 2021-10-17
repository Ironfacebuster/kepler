output_location = build/VS_BUILD_OUTPUT
mac_output_location = build/VS_BUILD_OUTPUT/MAC
linux_output_location = build/VS_BUILD_OUTPUT/LINUX
publish_location = build/VS_PUBLISH_OUTPUT
nightly_location = build/nightly
builds_location = build
mac_publish_location = build/VS_PUBLISH_OUTPUT/MAC
linux_publish_location = build/VS_PUBLISH_OUTPUT/LINUX
test_file = test_file.kep
project_location = "./src/kepler.csproj"

# by default "make" will produce a "release" version
all: clean \
		publish \
		test_local \
		pack 

# build:
# 	@cp -R ./bin/Resources/examples ./$(output_location); \
# 	dotnet build --output $(output_location); 

# Both unused right now.
publish_mac:
	dotnet build --output $(mac_publish_location) --runtime osx-x64; 
publish_linux:
	dotnet build --output $(linux_publish_location) --runtime linux-x64  $(project_location); 

# Publish (build) the executable.
publish:
	@echo "Publishing..."; \
	./scripts/generate_resources.bat release ; \
	dotnet publish -c Release -o $(publish_location) -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true $(project_location)

# Create a nightly build.
nightly:
	@echo "Creating nightly release..."; \
	./scripts/generate_resources.bat nightly; \
	dotnet clean $(project_location); \
	dotnet publish -c Debug -o $(nightly_location) -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true $(project_location); \
	cp "$(nightly_location)/kepler.exe" "$(nightly_location)/kepler-nightly.exe"; \
	tar.exe -cf "$(builds_location)/kepler-nightly.zip" "$(nightly_location)/kepler-nightly.exe"; \
	cp "res/nightly.txt" "$(nightly_location)/readme.txt"; \
	tar.exe -rf "$(builds_location)/kepler-nightly.zip" "$(nightly_location)/readme.txt"

# Pack the published executable into a windows installer.
pack:
	@mkdir -p "./build"; \
	makensis "./scripts/modern_installer.nsi"

clean:
	@dotnet clean $(project_location)

# Use this to run tests with your currently installed version of Kepler.
test:
	@ \
	./scripts/run_tests.bat

# Only use this after ensuring you've built (published) the version you want to test.
test_local:
	@ \
	./scripts/run_local_tests.bat

# Alias for "dotnet run" because the project isn't in the root anymore.
run:
	@ \
	dotnet run --project $(project_location) ;

cleanup:
	@echo Cleaning up after build...; \
	rm -rf "src/obj"; \
	rm -rf "src/bin"; \
	rm -rf "src/Resources";