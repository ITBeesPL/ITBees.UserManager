namespace ITBees.UserManager.Interfaces.Models;

public class AdditionalInvoiceDataIm
{
    public string NIP { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public string CompanyName { get; set; }
    public string PostCode { get; set; }
    public string InvoiceEmail { get; set; }
    public bool InvoiceRequested { get; set; }
}