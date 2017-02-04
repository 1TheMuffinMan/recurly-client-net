using System;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using System.Threading.Tasks;

namespace Recurly.Test
{
    public class SubscriptionTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName()) { Description = "Lookup Subscription Test" };
            plan.UnitAmountInCents.Add("USD", 1500);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            await sub.CreateAsync();

            sub.ActivatedAt.Should().HaveValue().And.NotBe(default(DateTime));
            sub.State.Should().Be(Subscription.SubscriptionState.Active);

            var fromService = Subscriptions.GetAsync(sub.Uuid);

            fromService.Should().Be(sub);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupSubscriptionPendingChanges()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Lookup Subscription With Pending Changes Test"
            };
            plan.UnitAmountInCents.Add("USD", 1500);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            await sub.CreateAsync();
            sub.UnitAmountInCents = 3000;

            await sub.ChangeSubscriptionAsync(Subscription.ChangeTimeframe.Renewal);

            var newSubscription = await Subscriptions.GetAsync(sub.Uuid);
            newSubscription.PendingSubscription.Should().NotBeNull();
            newSubscription.PendingSubscription.UnitAmountInCents.Should().Be(3000);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Create Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var coup = CreateNewCoupon(3);
            var sub = new Subscription(account, plan, "USD");
            sub.TotalBillingCycles = 5;
            sub.Coupon = coup;
            Assert.Null(sub.TaxInCents);
            Assert.Null(sub.TaxType);
            Assert.Null(sub.TaxRate);
            await sub.CreateAsync();

            sub.ActivatedAt.Should().HaveValue().And.NotBe(default(DateTime));
            sub.State.Should().Be(Subscription.SubscriptionState.Active);
            Assert.Equal(5, sub.TotalBillingCycles);
            Assert.Equal(coup.CouponCode, sub.Coupon.CouponCode);
            Assert.Equal(9, sub.TaxInCents.Value);
            Assert.Equal("usst", sub.TaxType);
            Assert.Equal(0.0875M, sub.TaxRate.Value);

            var sub1 = await Subscriptions.GetAsync(sub.Uuid);
            Assert.Equal(5, sub1.TotalBillingCycles);

        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateBulkSubscriptions()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Create Bulk Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            for (int i = 1; i < 4; i++)
            {
                var sub = new Subscription(account, plan, "USD");
                sub.Bulk = true;
                sub.CreateAsync();

                sub.ActivatedAt.Should().HaveValue().And.NotBe(default(DateTime));
                sub.State.Should().Be(Subscription.SubscriptionState.Active);

            }

        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateSubscriptionWithCoupon()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Create Subscription With Coupon Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var coupon = new Coupon(GetMockCouponCode(), "Sub Test " + GetMockCouponName(), 10);
            coupon.CreateAsync();

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD", coupon.CouponCode);
            sub.CreateAsync();

            sub.ActivatedAt.Should().HaveValue().And.NotBe(default(DateTime));
            sub.State.Should().Be(Subscription.SubscriptionState.Active);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task UpdateSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Update Subscription Plan 1"
            };
            plan.UnitAmountInCents.Add("USD", 1500);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var plan2 = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Update Subscription Plan 2"
            };
            plan2.UnitAmountInCents.Add("USD", 750);
            await plan2.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan2);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            await sub.CreateAsync();
            sub.Plan = plan2;

            sub.ChangeSubscriptionAsync(); // change "Now" is default

            var newSubscription = await Subscriptions.GetAsync(sub.Uuid);

            newSubscription.PendingSubscription.Should().BeNull();
            newSubscription.Plan.Should().Be(plan2);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CancelSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Cancel Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            sub.CancelAsync();

            sub.CanceledAt.Should().HaveValue().And.NotBe(default(DateTime));
            sub.State.Should().Be(Subscription.SubscriptionState.Canceled);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ReactivateSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Reactivate Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            sub.CancelAsync();
            sub.State.Should().Be(Subscription.SubscriptionState.Canceled);

            sub.ReactivateAsync();

            sub.State.Should().Be(Subscription.SubscriptionState.Active);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task TerminateSubscriptionNoRefund()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Terminate No Refund Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 200);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            sub.TerminateAsync(Subscription.RefundType.None);
            sub.State.Should().Be(Subscription.SubscriptionState.Expired);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task TerminateSubscriptionPartialRefund()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Terminate Partial Refund Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 2000);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            sub.TerminateAsync(Subscription.RefundType.Partial);
            sub.State.Should().Be(Subscription.SubscriptionState.Expired);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task TerminateSubscriptionFullRefund()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Terminate Full Refund Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 20000);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            sub.TerminateAsync(Subscription.RefundType.Full);

            sub.State.Should().Be(Subscription.SubscriptionState.Expired);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task PostponeSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Postpone Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();
            var renewal = DateTime.Now.AddMonths(3);

            sub.PostponeAsync(renewal);

            var diff = renewal.Date.Subtract(sub.CurrentPeriodEndsAt.Value.Date).Days;
            diff.Should().Be(1);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task UpdateNotesSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Postpone Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 100);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.CreateAsync();

            Dictionary<string, string> notes = new Dictionary<string, string>();

            notes.Add("CustomerNotes", "New Customer Notes");
            notes.Add("TermsAndConditions", "New T and C");
            notes.Add("VatReverseChargeNotes", "New VAT Notes");

            await sub.UpdateNotesAsync(notes);

            sub.CustomerNotes.Should().Be(notes["CustomerNotes"]);
            sub.TermsAndConditions.Should().Be(notes["TermsAndConditions"]);
            sub.VatReverseChargeNotes.Should().Be(notes["VatReverseChargeNotes"]);

        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateSubscriptionPlanWithAddons()
        {
            Plan plan = null;
            Plan plan2 = null;
            AddOn addon1 = null;
            AddOn addon2 = null;
            Account account = null;
            Subscription sub = null;
            Subscription sub2 = null;
            Subscription sub3 = null;

            try
            {
                plan = new Plan(GetMockPlanCode(), "aarons test plan")
                {
                    Description = "Create Subscription Plan With Addons Test"
                };
                plan.UnitAmountInCents.Add("USD", 100);
                plan.CreateAsync();

                addon1 = plan.NewAddOn("addon1", "addon1");
                addon1.DisplayQuantityOnHostedPage = true;
                addon1.UnitAmountInCents.Add("USD", 100);
                addon1.DefaultQuantity = 1;
                await addon1.CreateAsync();

                plan = Plans.Get(plan.PlanCode);

                var addon_test_1 = plan.GetAddOn("addon1");
                Assert.Equal(addon1.UnitAmountInCents["USD"], addon_test_1.UnitAmountInCents["USD"]);

                plan2 = new Plan(GetMockPlanCode(), "aarons test plan 2")
                {
                    Description = "Create Subscription Plan With Addons Test 2"
                };
                plan2.UnitAmountInCents.Add("USD", 1900);
                await plan2.CreateAsync();

                addon2 = plan2.NewAddOn("addon1", "addon2");
                addon2.DisplayQuantityOnHostedPage = true;
                addon2.UnitAmountInCents.Add("USD", 200);
                addon2.DefaultQuantity = 1;
                await addon2.CreateAsync();

                var addon_test_2 = plan2.GetAddOn("addon1");
                Assert.Equal(addon2.UnitAmountInCents["USD"], addon_test_2.UnitAmountInCents["USD"]);

                account = await CreateNewAccountWithBillingInfoAsync();

                sub = new Subscription(account, plan, "USD");
                sub.AddOns.Add(new SubscriptionAddOn("addon1", 100, 1)); // TODO allow passing just the addon code
                await sub.CreateAsync();

                // confirm that Create() doesn't duplicate the AddOns
                Assert.Equal(1, sub.AddOns.Count);

                sub.ActivatedAt.Should().HaveValue().And.NotBe(default(DateTime));
                sub.State.Should().Be(Subscription.SubscriptionState.Active);

                // test changing the plan of a subscription

                sub2 = await Subscriptions.GetAsync(sub.Uuid);
                sub2.UnitAmountInCents = plan2.UnitAmountInCents["USD"];
                sub2.Plan = plan2;

                foreach (var addOn in sub2.AddOns)
                {
                    addOn.UnitAmountInCents = plan2.UnitAmountInCents["USD"];
                }

                sub2.ChangeSubscriptionAsync(Subscription.ChangeTimeframe.Now);

                // check if the changes were saved
                sub3 = await Subscriptions.GetAsync(sub2.Uuid);
                sub3.UnitAmountInCents.Should().Equals(plan2.UnitAmountInCents["USD"]);
                Assert.Equal(1, sub3.AddOns.Count);
                foreach (var addOn in sub3.AddOns)
                {
                    addOn.UnitAmountInCents.Should().Equals(plan2.UnitAmountInCents["USD"]);
                }

            }
            finally
            {
                if (sub != null) await sub.CancelAsync();
                if (plan2 != null) await plan2.DeactivateAsync();
                if (plan != null) await plan.DeactivateAsync();
                if (account != null) await account.CloseAsync();
            }
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        [Trait("include", "y")]
        public async Task SubscriptionAddOverloads()
        {
            Plan plan = null;
            Account account = null;
            Subscription sub = null;
            System.Collections.Generic.List<AddOn> addons = new System.Collections.Generic.List<AddOn>();

            try
            {
                plan = new Plan(GetMockPlanCode(), "subscription addon overload plan")
                {
                    Description = "Create Subscription Plan With Addons Test"
                };
                plan.UnitAmountInCents.Add("USD", 100);
                plan.CreateAsync();

                int numberOfAddons = 7;

                for (int i = 0; i < numberOfAddons; ++i)
                {
                    var name = "Addon" + i.AsString();
                    var addon = plan.NewAddOn(name, name);
                    addon.DisplayQuantityOnHostedPage = true;
                    addon.UnitAmountInCents.Add("USD", 1000 + i);
                    addon.DefaultQuantity = i + 1;
                    await addon.CreateAsync();
                    addons.Add(addon);
                }

                account = await CreateNewAccountWithBillingInfoAsync();

                sub = new Subscription(account, plan, "USD");
                Assert.NotNull(sub.AddOns);

                sub.AddOns.Add(new SubscriptionAddOn("Addon0", 100, 1));
                sub.AddOns.Add(addons[1]);
                sub.AddOns.Add(addons[2], 2);
                sub.AddOns.Add(addons[3], 3, 100);
                sub.AddOns.Add(addons[4].AddOnCode);
                sub.AddOns.Add(addons[5].AddOnCode, 4);
                sub.AddOns.Add(addons[6].AddOnCode, 5, 100);

                sub.CreateAsync();
                sub.State.Should().Be(Subscription.SubscriptionState.Active);

                for (int i = 0; i < numberOfAddons; ++i)
                {
                    var code = "Addon" + i.AsString();
                    var addon = sub.AddOns.AsQueryable().First(x => x.AddOnCode == code);
                    Assert.NotNull(addon);
                }

                sub.AddOns.RemoveAt(0);
                Assert.Equal(6, sub.AddOns.Count);

                sub.AddOns.Clear();
                Assert.Equal(0, sub.AddOns.Count);

                var subaddon = new SubscriptionAddOn("a", 1);
                var list = new System.Collections.Generic.List<SubscriptionAddOn>();
                list.Add(subaddon);
                sub.AddOns.AddRange(list);
                Assert.Equal(1, sub.AddOns.Capacity);


                sub.AddOns.AsReadOnly();

                Assert.True(sub.AddOns.Contains(subaddon));

                Predicate<SubscriptionAddOn> p = x => x.AddOnCode == "a";
                Assert.True(sub.AddOns.Exists(p));
                Assert.NotNull(sub.AddOns.Find(p));
                Assert.Equal(1, sub.AddOns.FindAll(p).Count);
                Assert.NotNull(sub.AddOns.FindLast(p));

                int count = 0;
                sub.AddOns.ForEach(delegate (SubscriptionAddOn s)
                {
                    count++;
                });
                Assert.Equal(1, count);

                Assert.Equal(0, sub.AddOns.IndexOf(subaddon));

                sub.AddOns.Reverse();
                sub.AddOns.Sort();
            }
            finally
            {
                try
                {
                    if (sub != null && sub.Uuid != null) sub.CancelAsync();
                    if (plan != null) plan.DeactivateAsync();
                    if (account != null) await account.CloseAsync();
                }
                catch (RecurlyException e) { }
            }
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task PreviewSubscription()
        {
            var plan = new Plan(GetMockPlanCode(), GetMockPlanName())
            {
                Description = "Preview Subscription Test"
            };
            plan.UnitAmountInCents.Add("USD", 1500);
            plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var sub = new Subscription(account, plan, "USD");
            sub.UnitAmountInCents = 100;
            Assert.Null(sub.TaxType);
            sub.PreviewAsync();
            Assert.Equal("usst", sub.TaxType);
            Assert.Equal(Subscription.SubscriptionState.Active, sub.State);

            sub.CreateAsync();
            Assert.Throws<Recurly.RecurlyException>(
                delegate
                {
                    sub.PreviewAsync();
                }
            );

            sub.TerminateAsync(Subscription.RefundType.None);
            await account.CloseAsync();
        }
    }
}
