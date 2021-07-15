! right now, this file doesn't contain much,
! but as new features are added, they will be included in this file.

print "Test File v1"
print "Updated 7/14/21"
print ""

start Header
    successful_tests is 0
    total_tests is 13

    inc_success returns Int

    print "Testing conditional if statement"
    if True
        successful_tests is successful_tests + 1
    endif

    print "Testing global variables..."
    var1 is 3
    var2 is var1 + 1

    print "Testing variable types..."
    string1 is "a string"
    int1 is 1
    uint1 is u1
    float1 is 1.0

    print "Declaring a static function return type..."
    funct1 returns Int

    ! files are loaded relative to where the executable is launched from
    ! print "Testing file linking..."
    ! link "./VS_BUILD_OUTPUT/kepler_static/examples/link_test.sc"

end Header

start inc_success
    successful_tests is successful_tests + 1
end inc_success

if var1 equals 3
    call inc_success
endif

if var2 equals 4
    call inc_success
endif

if string1 equals "a string"
    call inc_success
endif

if int1 equals 1
    call inc_success
endif

if uint1 equals u1
    call inc_success
endif

if float1 equals 1.0
    call inc_success
endif

print "Declaring a function..."
start funct1
    print "Testing scoped variables..."
    local_var is 99
    local_var2 is local_var + 1

    if local_var equals 99
        call inc_success
    endif

    if local_var2 equals 100
        call inc_success
    endif

    ! return 1
    ! actually return the value once it's added
end funct1

print "Calling a declared function..."
funct_result is call funct1
! print funct_result

print "Validating basic operations..."
add is 2 + 2
subtract is 10 - 5
divide is 1.0 / 2.0
multiply is 2 * 8

if add equals 4
    call inc_success
endif

if subtract equals 5
    call inc_success
endif

if divide equals 0.5
    call inc_success
endif

if multiply equals 16
    call inc_success
endif

print ""
! print "If you're seeing this, all tests were successful!"
print successful_tests + "/" + total_tests + " tests successful"