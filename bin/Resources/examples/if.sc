start Header
    funct1 returns Int
    funct2 returns Int

    test_condition is True
    number is 2

end Header

start funct1
    print "number is 1!"
end funct1

start funct2
    print "number is 2!"
end funct2

if test_condition

    print "test_condition is true!"

    if number equals 1
        call funct1
    endif

    if number equals 2
        call funct2
    endif

endif

print "this should be printed no matter what!"