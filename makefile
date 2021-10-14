output_location = build/VS_BUILD_OUTPUT
mac_output_location = build/VS_BUILD_OUTPUT/MAC
linux_output_location = build/VS_BUILD_OUTPUT/LINUX
publish_location = build/VS_PUBLISH_OUTPUT
mac_publish_location = build/VS_PUBLISH_OUTPUT/MAC
linux_publish_location = build/VS_PUBLISH_OUTPUT/LINUX
test_file = test_file.kep
project_location = "./src/kepler.csproj"

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
	dotnet publish --output $(publish_location) $(project_location) ;

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

# Just an easy way of running the project without building.
run:
	@ \
	dotnet run --project $(project_location) ;