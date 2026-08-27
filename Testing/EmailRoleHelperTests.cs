using LoginFormASPCore6.Models;

namespace Testing
{
    public class EmailRoleHelperTests
    {
        [Theory]
        [InlineData("jane@dut4life.ac.za", EmailRoleHelper.StudentRole)]
        [InlineData("JANE@DUT4LIFE.AC.ZA", EmailRoleHelper.StudentRole)]
        [InlineData("john@dut.ac.za", EmailRoleHelper.StaffRole)]
        [InlineData("someone@gmail.com", EmailRoleHelper.UnknownRole)]
        [InlineData("", EmailRoleHelper.UnknownRole)]
        [InlineData(null, EmailRoleHelper.UnknownRole)]
        public void GetRole_ResolvesExpectedRoleFromDomain(string? email, string expectedRole)
        {
            var role = EmailRoleHelper.GetRole(email);

            Assert.Equal(expectedRole, role);
        }
    }
}
