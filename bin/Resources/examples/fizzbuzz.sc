start Header
    fizzbuzz returns Int
    increment returns Int

    num is 1
end Header

start fizzbuzz
    string is "" + num

    if num % 3 equals 0.0
        string is "Fizz"
    endif

    if num % 5 equals 0.0
        string is "Buzz"
    endif

    if num % 15 equals 0.0
        string is "FizzBuzz"
    endif

    print string
end fizzbuzz

start increment
    call fizzbuzz

    num is num + 1
end increment

! yeah, I know :(
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment
call increment