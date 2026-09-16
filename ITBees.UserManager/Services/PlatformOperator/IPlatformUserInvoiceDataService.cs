using System;
using System.Collections.Generic;
using ITBees.UserManager.Controllers.PlatformOperator;
using ITBees.UserManager.Controllers.PlatformOperator.Models;

namespace ITBees.UserManager.Services.PlatformOperator;

public interface IPlatformUserInvoiceDataService
{
    /// <summary>Dane do faktury każdej firmy, do której należy wskazany użytkownik.</summary>
    List<PlatformUserCompanyInvoiceDataVm> GetForUser(Guid userGuid);

    /// <summary>
    /// Aktualizuje dane do faktury firmy (najnowszy wiersz InvoiceData; przy jego braku tworzy
    /// nowy). Edycja najnowszego wiersza jest celowa - to on jest kopiowany jako szablon przy
    /// tworzeniu faktury za odnowienie subskrypcji, więc poprawka propaguje się na kolejne faktury.
    /// </summary>
    PlatformUserCompanyInvoiceDataVm Update(PlatformUserInvoiceDataUm um);
}
