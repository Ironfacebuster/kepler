! I'm considering renaming this from SCode to Phobos or Kepler

! this is a test program

! define main code Header section
start Header
    ! define some variables
	PI is Float and is 3.1415926535897931
	PI is Constant

	variable1 is Int and is 5
	variable1 is Constant

	variable2 is Int and is 100

	! note: default modifier is "variable"!
	variable3 is String and is "This is a string"
	! once a constant modifier is assigned, it can't be changed!
	! function1 is Constant Function

	! define static return type
	! function1 returns Int

    ! defining static non-positional argument types
	function1 uses var1 as Int
	! you can also chain together assignments!
	function1 uses var2 as Int and var3 as Float

	! link other Kepler files like this
	! link "return.sc"
end Header

! this function uses non-positional arguments
! in this case, the arguments are defined in the main Header
start function1

	! return var1 + var2
end function1

! this function uses positional arguments
! start function2 using var1, var2
! 	! internal function header
! 	start Header
! 		var1 is Int
! 		var2 is Int

! 		function2 returns Int 
! 	end Header

! 	return var1 + var2
! end function2


! this function uses non-positional arguments
funct1_result is call function1 with var1 as variable1, var2 as variable2
! JavaScript example
! funct1_result = function1(variable1, variable2)

! this function uses positional arguments
funct2_result is call function2 with variable1, variable2

if var3 equals True
	var2 is 0
	var3 is False
else if var2 equals 100
	var2 is 50
	var3 is True
else
	var2 is 0
	var3 is True

!-- this is a test of comment blocks. --!

! variable1 is 100
!-- 
	This
	is
	another
	test 
--!