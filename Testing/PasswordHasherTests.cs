using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Identity;

namespace Testing
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher<User> hasher = new();

        [Fact]
        public void HashPassword_ThenVerify_WithCorrectPassword_Succeeds()
        {
            var user = new User { EmpName = "Test", Gender = "Female", StudentNumber = "12345678", Email = "t@dut4life.ac.za" };
            var hashed = hasher.HashPassword(user, "Correct1!");

            var result = hasher.VerifyHashedPassword(user, hashed, "Correct1!");

            Assert.Equal(PasswordVerificationResult.Success, result);
        }

        [Fact]
        public void HashPassword_ThenVerify_WithWrongPassword_Fails()
        {
            var user = new User { EmpName = "Test", Gender = "Female", StudentNumber = "12345678", Email = "t@dut4life.ac.za" };
            var hashed = hasher.HashPassword(user, "Correct1!");

            var result = hasher.VerifyHashedPassword(user, hashed, "WrongPassword1!");

            Assert.Equal(PasswordVerificationResult.Failed, result);
        }

        [Fact]
        public void HashPassword_NeverStoresPlaintext()
        {
            var user = new User { EmpName = "Test", Gender = "Female", StudentNumber = "12345678", Email = "t@dut4life.ac.za" };
            var plaintext = "Correct1!";

            var hashed = hasher.HashPassword(user, plaintext);

            Assert.NotEqual(plaintext, hashed);
        }
    }
}
