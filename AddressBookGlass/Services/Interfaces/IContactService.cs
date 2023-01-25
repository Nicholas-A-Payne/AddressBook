using AddressBookGlass.Models;

namespace AddressBookGlass.Services.Interfaces
{
    public interface IContactService
    {
        public Task<Contact> GetContactByIdAsync(int contactId);
    }
}
