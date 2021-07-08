start Header
    const_1 is Int and is 1
    const_1 is Constant

    funct1 returns Int
end Header

var1 is 0

start funct1
    var1 is var1 + const_1

    print "Number of loops"
    print var1

    call funct1
end funct1

call funct1