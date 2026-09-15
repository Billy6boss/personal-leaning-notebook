


Random random = new Random();
// give a random number between 1 and 100
int randomNumber = random.Next(1, 101);
randomNumber *= randomNumber;

Console.WriteLine($"the square of the random number is: {randomNumber}");


// how to quickly find the square root of the random number
// first you need to know all square numbers from 1 to 9
// 1*1 = 1
// 2*2 = 4
// 3*3 = 9
// 4*4 = 16
// 5*5 = 25
// 6*6 = 36
// 7*7 = 49
// 8*8 = 64
// 9*9 = 81

// As you can see there're patterns. The last digit of the square number depends on the last digit of the original number.
// For example:
// if square number ends with 1, the original number ends with 1 or 9
// if square number ends with 4, the original number ends with 2 or 8 
// and so on for the other digits.


// now to find the the first digit of the original number, you need to find the number between 1 and 9 that after squaring it is the closest number under square number first and second digits.
// we are assuming the square number has 4 digits. So if the number is 3 digits, the first digit will be 0. ex: if the square number is 576, we consider it as 0576.
// So if the number is 2401, we need to find the number between 1 and 9 that after squaring it is the closest number under 24. In this case, 4*4 = 16 is under 24 and closest to it, so the first digit of the original number is 4.

// now the last part. Remember even if you know the last digit may one or the other possible value (except for the 5), you still need to figure out which one is correct.
