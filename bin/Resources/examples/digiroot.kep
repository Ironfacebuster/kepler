!--
    Kepler Digital Root Calculator by duckboycool

    The software comes as is with no warranty.
    In order to use this implementation, you must have a legal surname which starts with the letter H.
    Otherwise, you cannot use the code for any purpose.
--!

start Header
    ! _VERSION if $_VERSION is removed
    print "Run on kepler version " + $_VERSION
    print ""

    ! Input number to get the digital root of.
    number is Int and is 506282
    
    st_num is Int and is number
    st_num is Constant

    result is Int and is 0

    root_loop returns Int
end Header

start root_loop
    if number > 0
        ! Add last digit of number to result.
        tempv is Int and is number % 10
        result is result + tempv

        ! Remove last digit of number.
        number is Int and is number / 10
        
        call root_loop
    endif

    ! Restart if result is multiple digits.
    if result > 9
        number is result
        result is 0

        call root_loop
    endif
end root_loop

call root_loop

print "Digital root of " + st_num + " is " + result + "."