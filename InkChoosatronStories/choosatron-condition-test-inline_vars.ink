// Choosatron Metadata Tags
# title: Example Conditionals
# subtitle: Ifs, ands, but no buts.
# author: Author Person
# credits: My frans and fam.
# contact: @twitter, email, perhaps website
// If the ifid isn't present, one will be generated using the author and title as a seed.
# ifid: 00000000-0000-0000-0000-000000000000
// Date + time in hours offset from GMT.
# published: 2020-07-02-0500
# version: 1.0.0
// End of Choosatron Tags

/* --------------------------
    Constants & Variables
-------------------------- */
/* These are constants. The Inky editor won't let you alter it in your story.
   This is the speed added if you spin faster. */
CONST TOTAL_TESTS = 14
CONST TEST_CONST = 5
VAR passed_tests = 0
VAR var1 = TEST_CONST
VAR var2 = 0

/* TODO */
// Test ChoiceVisible.
// 

// You need to tell the story where to begin. This is called a 'divert' and will get the ball rolling.
-> test1

=== test1 ===
    ~ var2 = TEST_CONST
    This story is meant to be a fallthrough of tests. Choices will inform you if a command or operation has failed. Testing initialization, setting a CONST value, and equality. Var1 = { var1 } - Var2 = { var2 }.
    + { var1 == TEST_CONST } 1: var1 == TEST_CONST == 5
        -> test1
    + { var1 == var2 } 2: var1 == _var2
        -> test1
    + { CHOICE_COUNT() == 2 } TEST 1 SUCCESS
        -> test2
    + -> error

= test2
    ~ passed_tests++
    ~ var1-- // 4
    ~ var2 = var2 + var1 // 5 + 4 = 9
    Testing subtraction, addition, and 'not equals'. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 != TEST_CONST } 1: var1 != TEST_CONST
        -> test2
    + { var1 != var2 } 2: var1 != Var2
        -> test2
    + { CHOICE_COUNT() == 2 } TEST 2 SUCCESS
        -> test3
    + -> error
    
= test3
    ~ passed_tests++
    Testing greater, greater or equal, less than, and less or equal. Var1 = { var1 }, Var2 = { var2 }.
    6 Tests
    + { var2 > var1 } 1: var2 > var1
        -> test3
    + { var2 >= 9 } 2 var2 >= 9
        -> test3
    + { var2 >= 8 } 3: var2 >= 8
        -> test3
    + { var1 < var2 } 4: var1 < var2
        -> test3
    + { var1 <= 4 } 5: var1 <= 4
        -> test3
    + { var1 <= 5 } 6: var1 <= 5
        -> test3
    + { CHOICE_COUNT() == 6 } TEST 3 SUCCESS
        -> test4
    + -> error

= test4
    ~ passed_tests++
    ~ var2 = 10
    ~ var1 = var2 % var1 // 2
    ~ var2 = var1 % var2 // 2
    Testing MODULUS - (A % B) and assign. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 2 } 1: var2 % var1 == 2
        -> test4
    + { var2 == 2 } 2: var1 % var2 == 2
        -> test4
    + { CHOICE_COUNT() == 2 } TEST 4 SUCCESS
        -> test5
    + -> error

= test5
    ~ passed_tests++
    ~ var1 = var2 + 3 // 5
    ~ var2 = var1 - 20 // 
    Testing addition and subtraction. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 5 } 1: var2 + 3 == 7
        -> test5
    + { var2 == -15 } 2: var1 - 20 == -15
        -> test5
    + { CHOICE_COUNT() == 2 } TEST 5 SUCCESS
        -> test6
    + -> error

= test6
    ~ passed_tests++
    ~ var1 = var1 * 5
    ~ var2 = var2 * -5
    Testing multiplication. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 25 } 1: var1 * 5 == 25
        -> test6
    + { var2 == 75 } 2: var2 * -5 == 74
        -> test6
    + { CHOICE_COUNT() == 2 } TEST 6 SUCCESS
        -> test7
    + -> error

= test7
    ~ passed_tests++
    ~ var1 = var2 / var1
    ~ var2 = 27 / var1
    Testing division. Choosatron only supports integers remember. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 3 } 1: var2 / var1 == 3
        -> test7
    + { var2 == 9 } 2: 25 / var1 == 5
        -> test7
    + { CHOICE_COUNT() == 2 } TEST 7 SUCCESS
        -> test8
    + -> error

= test8
    ~ passed_tests++
    ~ var1 = RANDOM(15, 30)
    ~ var2 = RANDOM(-10, 10)
    Testing RANDOM(min, max). Var1 = { var1 }, Var2 = { var2 }.
    + { var1 >= 15 && var1 <= 30 } 1: RANDOM(15, 30)
        -> test8
    + { var2 >= -10 && var2 <= 10 } 2: RANDOM(-10, 10)
        -> test8
    + { CHOICE_COUNT() == 2 } TEST 8 SUCCESS
        -> test9
    + -> error

= test9
    ~ passed_tests++
    ~ var1 = false
    ~ var2 = true
    Testing not (!). Var1 = { var1 }, Var2 = { var2 }.
    + { !var1 == true } 1: not false
        -> test9
    + { !var2 == false } 2: not true
        -> test9
    + { CHOICE_COUNT() == 2 } TEST 9 SUCCESS
        -> test10
    + -> error

= test10
    ~ passed_tests++
    ~ var1 = 45
    ~ var2 = 77
    ~ var1 = MIN(13, var2) // 13
    ~ var2 = -77
    ~ var2 = MIN(var1, var2) // -77
    Testing MIN(a, b). Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 13 } 1: MIN(13, var2) == 13
        -> test10
    + { var2 == -77 } 2: MIN(var1, var2) == -77
        -> test10
    + { CHOICE_COUNT() == 2 } TEST 10 SUCCESS
        -> test11
    + -> error
    
= test11
    ~ passed_tests++
    ~ var1 = MAX(-110, -54)
    ~ var2 = MAX(45, 3232)
    Testing MAX(a, b). Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == -54 } 1: MAX(-110, -54) == -54
        -> test11
    + { var2 == 3232 } 2: MAX(45, 3232) == 3232
        -> test11
    + { CHOICE_COUNT() == 2 } TEST 11 SUCCESS
        -> test12
    + -> error

= test12
    ~ passed_tests++
    ~ var1 = POW(2, 5)
    ~ var2 = POW(-4, 6)
    Testing POW(a, b). Var = { var1 }, Var2 = { var2 }.
    + { var1 == 32 } 1: POW(2, 5) == 32
        -> test12
    + { var2 == 4096 } 2: POW(-4, 6) = 4096
        -> test12
    + { CHOICE_COUNT() == 2 } TEST 12 SUCCESS
        -> test13
    + -> error

= test13
    ~ passed_tests++
    { var1 == var2 - 4064:
        ~ var1 = 99 + 1
    }
    {
        - var2 > 5000:
            ~ var2 = 666
        - else:
            ~ var2 = 999
    }
    Testing IF and IF/ELSE. Var1 = { var1 }, Var2 = { var2 }.
    + { var1 == 100 } 1: If (var1 == var2 - 4064) \{ var1 = 99 + 1 \}
        -> test13
    + { var2 == 999 } 2: If (var2 > 5000) \{ var2 = 666 \} Else \{ var2 = 999 \}
        -> test13
    + { CHOICE_COUNT() == 2 } TEST 13 SUCCESS
    ~ passed_tests++
        -> test14
    + -> error

= test14
    {
        - var1 < 101:
            ~ var2 = 1
        - var1 == 101:
            ~ var2 = 2
        - else:
            ~ var2 = 3
    }
    Testing IF and IF/ELSE. Var1 = { var1 }, Var2 = { var2 }.
    + { var2 == 1 } Part 1/3: If (var1 < 101 ) \{ var2 = 1 \}...
        ~ var1++
        -> test14
    + { var2 == 2 } Part 2/3: Else If (var1 == 101) \{ var2 = 2 \}...
        ~ var1++
        -> test14
    + { var2 == 3 } Part 3/3: Else \{ var2 = 3 \}...
        -> passed

= passed
    ~ passed_tests++
    All { passed_tests } out of { TOTAL_TESTS } tests passed!
    -> END

= error
    An error was found.
    -> END