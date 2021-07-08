start Header
    var1 is Int and is 100
    function1 returns Int
    function2 returns Int

    ! link "./examples/types.sc"
end Header

start function1
    print "Hello world!"

    call function2
end function1

start function2
    print "Hello world, again!"

    call function1
end function2

print var1

call function1