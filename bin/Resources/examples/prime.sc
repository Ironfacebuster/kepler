start Header

    test_number is 0

    int_c is 0
    isprime is True

    check_prime returns Boolean
    loop returns Boolean
    
end Header

start loop 

    int_c is int_c + 1

    ! check if int_c is not 1, and not test_number
    if int_c > 1
        if int_c < test_number
            ! if test_number is evenly divisible by int_c
            if test_number % int_c equals 0.0
                isprime is False
            endif
        endif
    endif

    ! if int_c is less than test_number, and not prime
    if isprime
        if int_c < test_number
            call loop
        endif
    endif

end loop

start check_prime 

    ! print "Checking " + test_number
    int_c is 0
    isprime is True
    
    call loop

    if isprime 
        print test_number + " is prime!"
    endif

    if isprime equals False
        print test_number + " is not prime!"
    endif

end check_prime

test_number is 35
call check_prime