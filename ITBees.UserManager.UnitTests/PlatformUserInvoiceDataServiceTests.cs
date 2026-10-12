using System;
using System.Linq;
using ITBees.Models.Companies;
using ITBees.Models.Users;
using ITBees.UserManager.Interfaces.Models;
using ITBees.UserManager.Services.PlatformOperator;
using ITBees.UserManager.UnitTests.Fakes;
using NUnit.Framework;
// ITBees.Models.Languages has a language class called Is (Icelandic).
using Is = NUnit.Framework.Is;

namespace ITBees.UserManager.UnitTests
{
    /// <summary>
    /// The operator sees the company data the user typed in the application next to the invoice
    /// data from the payment - an invoice can be issued before the user gives the latter.
    /// </summary>
    public class PlatformUserInvoiceDataServiceTests
    {
        private readonly Guid _userGuid = Guid.NewGuid();

        private Company CompanyWithData() => new Company
        {
            Guid = Guid.NewGuid(), CompanyName = "Transport Kowalski Sp. z o.o.", CompanyShortName = "Kowalski",
            Nip = "5252248481", Street = "Długa 1", PostCode = "00-001", City = "Warszawa", Email = "biuro@kowalski.pl"
        };

        private PlatformUserInvoiceDataService ServiceFor(Company company, params InvoiceData[] invoiceData)
        {
            var membership = new UsersInCompany
            {
                Guid = Guid.NewGuid(), UserAccountGuid = _userGuid, CompanyGuid = company.Guid, Company = company
            };
            var invoiceDataRepo = new FakeRepository<InvoiceData>(invoiceData);

            return new PlatformUserInvoiceDataService(new FakeRepository<UsersInCompany>(membership),
                invoiceDataRepo, invoiceDataRepo, new FakeRepository<Company>(company),
                new FakeAspCurrentUserService(new CurrentUser { Guid = Guid.NewGuid() }, isPlatformOperator: true));
        }

        [Test]
        public void Company_data_is_shown_although_the_user_never_gave_invoice_data()
        {
            var company = CompanyWithData();

            var vm = ServiceFor(company).GetForUser(_userGuid).Single();

            Assert.That(vm.InvoiceDataGuid, Is.Null);
            Assert.That(vm.Nip, Is.Null);
            Assert.That(vm.CompanyData.CompanyName, Is.EqualTo("Transport Kowalski Sp. z o.o."));
            Assert.That(vm.CompanyData.CompanyShortName, Is.EqualTo("Kowalski"));
            Assert.That(vm.CompanyData.Nip, Is.EqualTo("5252248481"));
            Assert.That(vm.CompanyData.Street, Is.EqualTo("Długa 1"));
            Assert.That(vm.CompanyData.PostCode, Is.EqualTo("00-001"));
            Assert.That(vm.CompanyData.City, Is.EqualTo("Warszawa"));
            Assert.That(vm.CompanyData.Email, Is.EqualTo("biuro@kowalski.pl"));
        }

        [Test]
        public void Company_data_and_invoice_data_from_the_payment_are_kept_apart()
        {
            var company = CompanyWithData();
            var fromPayment = new InvoiceData
            {
                Guid = Guid.NewGuid(), CompanyGuid = company.Guid, CompanyName = "Jan Kowalski", NIP = "",
                City = "Kraków", Created = DateTime.Now
            };

            var vm = ServiceFor(company, fromPayment).GetForUser(_userGuid).Single();

            Assert.That(vm.InvoiceDataGuid, Is.EqualTo(fromPayment.Guid));
            Assert.That(vm.CompanyName, Is.EqualTo("Jan Kowalski"));
            Assert.That(vm.City, Is.EqualTo("Kraków"));
            Assert.That(vm.CompanyData.CompanyName, Is.EqualTo("Transport Kowalski Sp. z o.o."));
            Assert.That(vm.CompanyData.Nip, Is.EqualTo("5252248481"));
            Assert.That(vm.CompanyData.City, Is.EqualTo("Warszawa"));
        }
    }
}
