start Header
    ! define some variables
	integer is Int and is 5
    unsigned_integer is uInt and is u5

    float is Float and is 5.0

    string is String and is "test string"

    ! link "./examples/return.sc"
end Header

start function1 
    ! should not be instantiated in the global scope!
    testvar is Float and is 0.0
    string is "this is a different string!"
    ! all variables declared within this function are lost when the function is exited

    start function2 
        blah is "whatever"
    end function2

    call function2
end function1

call function1