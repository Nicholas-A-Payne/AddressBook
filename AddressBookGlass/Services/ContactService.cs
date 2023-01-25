using AddressBookGlass.Data;
using AddressBookGlass.Models;
using AddressBookGlass.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AddressBookGlass.Services
{
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;

        public ContactService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Contact> GetContactByIdAsync(int contactId)
        {
            Contact? contact = new();

            contact = await _context.Contacts
                                    .Include(c => c.User)
                                    .Include(c => c.Categories)
                                    .FirstOrDefaultAsync(c => c.Id == contactId);

            return contact!;
        }

    }
}
