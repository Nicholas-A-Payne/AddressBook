#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AddressBookGlass.Data;
using AddressBookGlass.Models;
using Microsoft.AspNetCore.Identity;
using AddressBookGlass.Services.Interfaces;
using AddressBookGlass.Enums;
using Microsoft.AspNetCore.Authorization;
using AddressBookGlass.Services;

namespace AddressBookGlass.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICategoryService _categoryService;
        private readonly IContactService _contactService;
        private readonly DataService _dataService;
        private readonly SearchService _searchService;

        public ContactsController(ApplicationDbContext context,
                                    UserManager<AppUser> userManager,
                                    ICategoryService categoryService,
                                    IContactService contactService,
                                    SearchService searchService, 
                                    DataService dataService)
        {
            _context = context;
            _userManager = userManager;
            _categoryService = categoryService;
            _contactService = contactService;
            _searchService = searchService;
            _dataService = dataService;
        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User);
            var DBResults = _context.Contacts
                                    .Include(c => c.User)
                                    .Include(c => c.Categories)
                                    .Where(c => c.UserId == userId);
           
            List<Contact> contacts = await DBResults.ToListAsync();
            return View(contacts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchContacts(string searchString)
        {
            var userId = _userManager.GetUserId(User); 

            var model = _searchService.SearchContacts(searchString, userId);


            return View(nameof(Index), model);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public async Task<IActionResult> Create()
        {
            string userId = _userManager.GetUserId(User);
           
            ViewData["CategoryList"] = new MultiSelectList(await _categoryService.GetUserCategoriesAsync(userId), "Id", "Name");
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            return View();

        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Birthday,AddressOne,AdressTwo,City,State,ZipCode,Email,PhoneNumber, ImageFile")] Contact contact, List<int> categoryList)
        {
            if (ModelState.IsValid)
            {
                contact.UserId = _userManager.GetUserId(User);
                contact.Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
                contact.Created = _dataService.GetPostGresDate(DateTime.Now);

                if (contact.Birthday != null)
                {
                    contact.Birthday = DateTime.SpecifyKind((DateTime)contact.Birthday, DateTimeKind.Utc);
                }
                _context.Add(contact);
                await _context.SaveChangesAsync();


                await _categoryService.AddContactToCategoriesAsync(categoryList, contact.Id);

                return RedirectToAction(nameof(Index));
            }
            string userId = _userManager.GetUserId(User);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _categoryService.GetUserCategoriesAsync(userId), "Id", "Name");
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }


            string userId = _userManager.GetUserId(User);

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _categoryService.GetUserCategoriesAsync(userId), "Id", "Name", await _categoryService.GetContactCategoryIdsAsync(contact.Id));
            
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,FirstName,LastName,Birthday,AddressOne,AdressTwo,City,State,ZipCode,Email,Created,PhoneNumber,ImageFile, ImageData, ImageType")] Contact contact, List<int> categoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.Created = DateTime.SpecifyKind((DateTime)contact.Created, DateTimeKind.Utc);
                    contact.Created = _dataService.GetPostGresDate(DateTime.Now);

                    if (contact.Birthday != null)
                    {
                        contact.Birthday = DateTime.SpecifyKind((DateTime)contact.Birthday, DateTimeKind.Utc);
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    var oldCategories = await _categoryService.GetContactCategoriesAsync(contact.Id);
                   
                    //Remove contact from their current categories (If any)
                    foreach (var category in oldCategories)
                    {
                        await _categoryService.RemoveContactFromCategoryAsync(category.Id, contact.Id);
                    } 

                    //Add contact to categories chosen by the user (If any)
                    await _categoryService.AddContactToCategoriesAsync(categoryList, contact.Id);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }


            string userId = _userManager.GetUserId(User);

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategpryList"] = new MultiSelectList(await _categoryService.GetUserCategoriesAsync(userId), "Id", "Name");
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Contact contact = await _contactService.GetContactByIdAsync(id.Value);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
