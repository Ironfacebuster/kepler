! right now, this file doesn't contain much,
! but as new features are added, they will be included in this file.

print "Test File v1"
print "Updated 7/10/21"
print ""

start Header

    print "Testing global variables..."
    var1 is 3
    var2 is var1 + 1

    print "Testing variable types..."
    string1 is "a string"
    int1 is 1
    uint1 is u1
    float1 is 1.0

    ! print string1 equals "a string"
    ! print int1 equals 1
    ! print uint1 equals u1
    ! print float1 equals 1.0

    print "Declaring a static function return type..."
    funct1 returns Int

    ! files are loaded relative to where the executable is launched from
    print "Testing file linking..."
    link "./BUILD_OUTPUT/examples/link_test.sc"

end Header

print "Declaring a function..."
start funct1
    print "Testing scoped variables..."
    local_var is 99
    local_var2 is local_var + 1

    ! print local_var equals 99
    ! print local_var2 equals 100

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

! print add equals 4
! print subtract equals 5
! print divide equals 0.5
! print multiply equals 16

print ""
print "If you're seeing this, all tests were successful!"