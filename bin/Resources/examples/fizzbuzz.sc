!--
    FizzBuzz by duckboycool

    The software comes as is with no warranty.
    In order to use this implementation, you must include a picture 
    of yourself or somebody that can't be proven to not be yourself 
    in a clown costume in the project that you are using the code in.
    Otherwise, you cannot use the code for any purpose.
--!

start Header
    print "Run on Kepler version " + $_VERSION

    loop is Int and is 1

    NUM is Int and is 100
    NUM is Constant

    fizz_loop returns Int
end Header

start fizz_loop
    line is String and is ""
    num is Boolean and is True

    if loop % 3 equals 0.0
        line is line + "Fizz"
        num is False
    endif

    if loop % 5 equals 0.0
        line is line + "Buzz"
        num is False
    endif

    if num
        print loop
    endif

    if num equals False
        print line
    endif

    if loop < NUM
        loop is loop + 1
        call fizz_loop
    endif
end fizz_loop

call fizz_loop
