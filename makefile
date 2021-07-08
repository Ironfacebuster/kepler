output_location = BUILD_OUTPUT
test_file = test_file.sc

all: clean \
		build \
		test

log:
	@echo $(test_command) \

build:
	@dotnet build --output $(output_location)

clean:
	@dotnet clean

dotnet_test:
	@ \
	echo "Running test..."; \
	dotnet run --file "./$(output_location)/examples/$(test_file)" || (echo -e "\e[1;31mTest failed (code: $$?)\e[0m"; exit 1)

test:
	@ \
	echo -e "\u001b[36m"; \
	echo -e "Testing latest build..."; \
	echo -e "\e[0m"; \
	./$(output_location)/kepler --file "./$(output_location)/examples/$(test_file)" || (echo -e "\e[1;31mTest failed (code: $$?)\e[0m"; exit 1)