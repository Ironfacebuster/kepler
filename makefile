output_location = build/VS_BUILD_OUTPUT
mac_output_location = build/VS_BUILD_OUTPUT/MAC
linux_output_location = build/VS_BUILD_OUTPUT/LINUX
publish_location = build/VS_PUBLISH_OUTPUT
test_file = test_file.kep

all: publish \
		pack \
		test_local

log:
	@echo $(test_command) \

build:
	@cp -R ./bin/Resources/examples ./$(output_location); \
	dotnet build --output $(output_location); 

publish_mac:
	dotnet build --output $(mac_output_location) --runtime osx-x64; 
publish_linux:
	dotnet build --output $(mac_output_location) --runtime linux-x64; 

publish:
	@echo "Publishing..."; \
	dotnet publish --output $(publish_location) "./src/kepler.csproj" ;

pack:
	@mkdir -p "./build"; \
	makensis "./scripts/modern_installer.nsi"

clean:
	@dotnet clean

test:
	@ \
	./scripts/run_tests.bat

test_local:
	@ \
	./scripts/run_local_tests.bat

run:
	@ \
	dotnet run --project "./src/kepler.csproj" ;
# test:
# 	@ \
# 	echo -e "\u001b[36m"; \
# 	echo -e "Testing latest build..."; \
# 	echo -e "\e[0m"; \
# 	./$(output_location)/kepler --file "./$(output_location)/examples/$(test_file)" || (echo -e "\e[1;31mTest failed (code: $$?)\e[0m"; exit 1)