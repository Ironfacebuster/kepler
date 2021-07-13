output_location = VS_BUILD_OUTPUT
publish_location = VS_PUBLISH_OUTPUT
test_file = test_file.sc

all: clean \
		build \
		test \
		publish

log:
	@echo $(test_command) \

build:
	@cp -R ./kepler_static ./$(output_location); \
	dotnet build --output $(output_location); 

publish:
	@echo "Publishing..."; \
	dotnet publish --output $(publish_location);

pack:
	@cp -R ./kepler_static ./$(publish_location); \
	mkdir -p "./bin/BUILD"; \
	makensis "./bin/modern_installer.nsi"
	# @cp -R ./kepler_static ./$(publish_location); \
	# ./bin/create_installer.bat "./bin/installer.nsi"; \
	# mkdir -p "./bin/BUILD"; \
	# makensis "./bin/installer.nsi"

clean:
	@dotnet clean

dotnet_test:
	@echo "Running test..."; \
	dotnet run --file "./$(output_location)/kepler_static/examples/$(test_file)" || (echo -e "\e[1;31mTest failed (code: $$?)\e[0m"; exit 1)

test:
	@ \
	echo -e "\u001b[36m"; \
	echo -e "Testing latest build..."; \
	echo -e "\e[0m"; \
	./$(output_location)/kepler --file "./$(output_location)/kepler_static/examples/$(test_file)" || (echo -e "\e[1;31mTest failed (code: $$?)\e[0m"; exit 1)