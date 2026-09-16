using System;

namespace ITBees.UserManager.Controllers.PlatformOperator.Models;

/// <summary>
/// Aktualizacja danych do faktury firmy przez operatora platformy. Wskazuje się firmę, nie
/// konkretny wiersz - serwis zawsze edytuje najnowszy wiersz InvoiceData firmy (to on jest
/// szablonem kopiowanym przy odnowieniach subskrypcji), a gdy firma nie ma żadnego, tworzy nowy.
/// </summary>
public class PlatformUserInvoiceDataUm
{
    public Guid CompanyGuid { get; set; }

    public string CompanyName { get; set; }
    public string Nip { get; set; }
    public string Street { get; set; }
    public string PostCode { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string InvoiceEmail { get; set; }
    public bool InvoiceRequested { get; set; }
}
