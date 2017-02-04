using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class SubscriptionListTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListLiveSubscriptions()
        {
            var p = new Plan(GetMockPlanCode(), GetMockPlanName()) {Description = "Subscription Test"};
            p.UnitAmountInCents.Add("USD", 200);
            await p.CreateAsync();
            PlansToDeactivateOnDispose.Add(p);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();

                var sub = new Subscription(account, p, "USD");
                await sub.CreateAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.Live);
            subs.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListActiveSubscriptions()
        {
            var p = new Plan(GetMockPlanCode(), GetMockPlanName()) { Description = "Subscription Test" };
            p.UnitAmountInCents.Add("USD", 300);
            await p.CreateAsync();
            PlansToDeactivateOnDispose.Add(p);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();

                var sub = new Subscription(account, p, "USD");
                await sub.CreateAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.Active);
            subs.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListCanceledSubscriptions()
        {
            var p = new Plan(GetMockPlanCode(), GetMockPlanName()) { Description = "Subscription Test" };
            p.UnitAmountInCents.Add("USD", 400);
            await p.CreateAsync();
            PlansToDeactivateOnDispose.Add(p);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();

                var sub = new Subscription(account, p, "USD");
                await sub.CreateAsync();

                await sub.CancelAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.Canceled);
            subs.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListExpiredSubscriptions()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Subscription Test",
                PlanIntervalLength = 1,
                PlanIntervalUnit = Plan.IntervalUnit.Months
            };
            plan.UnitAmountInCents.Add("USD", 400);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var sub = new Subscription(account, plan, "USD")
                {
                    StartsAt = DateTime.Now.AddMonths(-5)
                };

                await sub.CreateAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.Expired);
            subs.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListFutureSubscriptions()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Subscription Test",
                PlanIntervalLength = 1,
                PlanIntervalUnit = Plan.IntervalUnit.Months
            };
            plan.UnitAmountInCents.Add("USD", 400);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var sub = new Subscription(account, plan, "USD")
                {
                    StartsAt = DateTime.Now.AddMonths(1)
                };

                await sub.CreateAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.Future);
            subs.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListInTrialSubscriptions()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Subscription Test",
                PlanIntervalLength = 1,
                PlanIntervalUnit = Plan.IntervalUnit.Months,
                TrialIntervalLength = 2,
                TrialIntervalUnit = Plan.IntervalUnit.Months
            };
            plan.UnitAmountInCents.Add("USD", 400);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var sub = new Subscription(account, plan, "USD")
                {
                    TrialPeriodEndsAt = DateTime.UtcNow.AddMonths(2)
                };
                await sub.CreateAsync();
            }

            var subs = Subscriptions.List(Subscription.SubscriptionState.InTrial);
            subs.Should().NotBeEmpty();
        }

        /// <summary>
        /// This test isn't constructed as expected, as there doesn't appear to be a way to
        /// programmatically make a subscription past due.
        /// </summary>
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListPastDueSubscriptions()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Subscription Test",
                PlanIntervalLength = 1,
                PlanIntervalUnit = Plan.IntervalUnit.Months
            };
            plan.UnitAmountInCents.Add("USD", 200100);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var subs = new List<Subscription>();
            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var sub = new Subscription(account, plan, "USD");
                await sub.CreateAsync();
                subs.Add(sub);
            }

            var list = Subscriptions.List(Subscription.SubscriptionState.PastDue);
            list.Should().NotContain(subs);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListForAccount()
        {
            var plan1 = new Plan(GetMockPlanCode(), GetMockPlanName()) {Description = "Subscription Test"};
            plan1.UnitAmountInCents.Add("USD", 400);
            await plan1.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan1);

            var plan2 = new Plan(GetMockPlanCode(), GetMockPlanName()) {Description = "Subscription Test"};
            plan2.UnitAmountInCents.Add("USD", 500);
            await plan2.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan2);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan1, "USD");
            await sub.CreateAsync();

            var sub2 = new Subscription(account, plan2, "USD");
            await sub2.CreateAsync();

            var list = account.GetSubscriptions(Subscription.SubscriptionState.All);
            list.Should().NotBeEmpty();
        }
    }
}
