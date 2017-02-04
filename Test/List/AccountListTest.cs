using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Recurly.Configuration;
using Xunit;
using AccountState = Recurly.Account.AccountState;

namespace Recurly.Test
{
    public class AccountListTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public void List()
        {
            var accounts = Accounts.List();
            accounts.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListActive()
        {
            var tasks = new Task[2];
            tasks[0] = CreateNewAccountAsync();
            tasks[1] = CreateNewAccountAsync();
            await Task.WhenAll(tasks);

            var accounts = Accounts.List(AccountState.Active);
            accounts.Should().HaveCount(x => x >= 2);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListClosed()
        {
            var accountTasks = new List<Task<Account>>(2)
            {
                CreateNewAccountAsync(),
                CreateNewAccountAsync()
            };
            await Task.WhenAll(accountTasks);

            var closeTasks = new Task[2];
            closeTasks[0] = (await accountTasks[0]).CloseAsync();
            closeTasks[1] = (await accountTasks[1]).CloseAsync();
            await Task.WhenAll(closeTasks);

            var accounts = Accounts.List(AccountState.Closed);
            accounts.Should().HaveCount(x => x >= 2);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListPastDue()
        {
            var acct = await CreateNewAccountAsync();

            var adjustment = acct.NewAdjustment("USD", 5000, "Past Due", 1);
            await adjustment.CreateAsync();

            acct.InvoicePendingCharges();

            var accounts = Accounts.List(AccountState.PastDue);
            accounts.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public void AccountList_supports_paging()
        {
            var testSettings = SettingsFixture.TestSettings;
            var moddedSettings = new Settings();

            moddedSettings.Initialize(testSettings.ApiKey, testSettings.Subdomain,
                testSettings.PrivateKey, 5);

            Client.Instance.ApplySettings(moddedSettings);

            var accounts = Accounts.List();
            accounts.Should().HaveCount(5);
            accounts.Capacity.Should().BeGreaterOrEqualTo(5);

            accounts.Next.Should().NotBeEmpty();
        }
    }
}