using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class AccountTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateAccount()
        {
            var acct = new Account(GetUniqueAccountCode());
            await acct.CreateAsync();
            acct.CreatedAt.Should().NotBe(default(DateTime));
            Assert.False(acct.TaxExempt.Value);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateAccountWithParameters()
        {
            var acct = new Account(GetUniqueAccountCode())
            {
                Username = "testuser1",
                Email = "testemail@test.com",
                FirstName = "Test",
                LastName = "User",
                CompanyName = "Test Company",
                AcceptLanguage = "en",
                VatNumber = "my-vat-number",
                TaxExempt = true,
                EntityUseCode = "I",
                CcEmails = "cc1@test.com,cc2@test.com",
                Address = new Address()
            };

            string address = "123 Faux Street";
            acct.Address.Address1 = address;

            await acct.CreateAsync();

            acct.Username.Should().Be("testuser1");
            acct.Email.Should().Be("testemail@test.com");
            acct.FirstName.Should().Be("Test");
            acct.LastName.Should().Be("User");
            acct.CompanyName.Should().Be("Test Company");
            acct.AcceptLanguage.Should().Be("en");
            acct.CcEmails.Should().Be("cc1@test.com,cc2@test.com");
            Assert.Equal("my-vat-number", acct.VatNumber);
            Assert.True(acct.TaxExempt.Value);
            Assert.Equal("I", acct.EntityUseCode);
            Assert.Equal(address, acct.Address.Address1);
            Assert.False(acct.VatLocationValid);
        }

        [Fact]
        public void DontSerializeNullAddress()
        {
            var account = new Account("testAcct");
            account.Address.Should().BeNull();
        }

        [Fact]
        public async Task CreateAccountWithBillingInfo()
        {
            var accountCode = GetUniqueAccountCode();
            var account = new Account(accountCode, NewBillingInfo(accountCode));

            await account.CreateAsync();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupAccount()
        {
            var newAcct = new Account(GetUniqueAccountCode())
            {
                Email = "testemail@recurly.com"
            };
            await newAcct.CreateAsync();

            var account = await Accounts.GetAsync(newAcct.AccountCode);

            account.Should().NotBeNull();
            account.AccountCode.Should().Be(newAcct.AccountCode);
            account.Email.Should().Be(newAcct.Email);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public void FindNonExistentAccount()
        {
            Action get = () => Accounts.GetAsync("totallynotfound!@#$");
            get.ShouldThrow<NotFoundException>();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task UpdateAccount()
        {
            var acct = new Account(GetUniqueAccountCode());
            await acct.CreateAsync();

            acct.LastName = "UpdateTest123";
            acct.TaxExempt = true;
            acct.VatNumber = "woot";
            await acct.UpdateAsync();

            var getAcct = await Accounts.GetAsync(acct.AccountCode);
            acct.LastName.Should().Be(getAcct.LastName);
            Assert.True(acct.TaxExempt.Value);
            Assert.Equal("woot", acct.VatNumber);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CloseAccount()
        {
            var accountCode = GetUniqueAccountCode();
            var acct = new Account(accountCode);
            await acct.CreateAsync();

            await acct.CloseAsync();

            var getAcct = await Accounts.GetAsync(accountCode);
            getAcct.State.Should().Be(Account.AccountState.Closed);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ReopenAccount()
        {
            var accountCode = GetUniqueAccountCode();
            var acct = new Account(accountCode);
            await acct.CreateAsync();
            await acct.CloseAsync();

            await acct.ReopenAsync();

            var test = await Accounts.GetAsync(accountCode);
            acct.State.Should().Be(test.State).And.Be(Account.AccountState.Active);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetAccountNotes()
        {
            var account = await CreateNewAccountAsync();

            var notes = account.GetNotes();

            notes.Should().BeEmpty();
        }
    }
}
