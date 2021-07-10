! right now, this file doesn't contain much,
! but as new features are added, they will be included in this file.

print "Test File v1"
print "Updated 7/10/21"
print ""

start Header

    print "Testing global variables..."
    var1 is 0
    var2 is var1 + 1
    ! print var1
    ! print var2

    print "Testing variable types..."
    string1 is "a string"
    int1 is 1
    uint1 is u1
    float1 is 1.0

    ! print "Expected 'a string', got: " + string1
    ! print "Expected 1, got: " + int1
    ! print "Expected u1, got: " + uint1
    ! print "Expected 1.0, got: " + float1

    print "Declaring a static function return type..."
    funct1 returns Int

    ! files are loaded relative to where the executable is launched from
    print "Testing file linking..."
    link "./BUILD_OUTPUT/examples/link_test.sc"

end Header

print "Declaring a function..."
start funct1
    print "Testing scoped variables..."
    local_var is 0
    local_var2 is local_var + 1
    
    ! print "Expected 0, got: " + local_var
    ! print "Expected 1, got: " + local_var2

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

! print "Expected 4, got: " + add
! print "Expected 5, got: " + subtract
! print "Expected 0.5, got: " + divide
! print "Expected 16, got: " + multiply

print ""
print "If you're seeing this, all tests were successful!"