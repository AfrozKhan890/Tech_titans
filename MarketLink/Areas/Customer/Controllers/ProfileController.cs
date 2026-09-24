using MarketLink.Areas.Customer;
using MarketLink.Areas.Customer.Services;
using MarketLink.Data;
using MarketLink.Models;
using MarketLink.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(AuthenticationSchemes = CustomerAuthDefaults.AuthenticationScheme, Policy = CustomerAuthDefaults.CustomerPolicy)]
    public class ProfileController : CustomerControllerBase
    {
        public ProfileController(MarketLinkDbContext db, ICustomerIdentityService identity) : base(db, identity) { }

        public async Task<IActionResult> Index()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);

            var addresses = await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId)
                .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Label).ToListAsync();
            var parts = customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var first = parts.FirstOrDefault() ?? customer.FullName;
            var last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

            return View(new CustomerProfileViewModel
            {
                FirstName = first,
                LastName = last,
                Email = customer.Email,
                PhoneNumber = customer.Phone,
                Bio = customer.Bio,
                DefaultCity = customer.DefaultCity,
                ProfileImageUrl = customer.ProfileImageUrl,
                MemberSince = customer.RegisteredAt,
                TotalOrders = await Db.Orders.CountAsync(o => o.CustomerId == customer.CustomerId),
                TotalFavorites = await Db.Favorites.CountAsync(f => f.CustomerId == customer.CustomerId),
                TotalAddresses = addresses.Count,
                Addresses = addresses
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CustomerProfileViewModel model)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            if (!ModelState.IsValid)
            {
                model.Addresses = await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId).ToListAsync();
                return View(model);
            }

            customer.FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}".Trim();
            customer.Phone = model.PhoneNumber?.Trim();
            customer.Bio = model.Bio?.Trim();
            customer.DefaultCity = model.DefaultCity?.Trim();
            await Db.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Addresses()
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            return View(await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId)
                .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Label).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(CustomerAddressViewModel model)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            if (!ModelState.IsValid) { TempData["Error"] = "Please complete the address fields."; return RedirectToAction(nameof(Addresses)); }

            var existing = await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId).ToListAsync();
            var makeDefault = model.IsDefault || existing.Count == 0;
            if (makeDefault) foreach (var address in existing) address.IsDefault = false;

            Db.CustomerAddresses.Add(new CustomerAddress
            {
                CustomerId = customer.CustomerId,
                Label = model.Label.Trim(),
                AddressLine1 = model.AddressLine1.Trim(),
                AddressLine2 = model.AddressLine2?.Trim(),
                City = model.City.Trim(),
                State = model.State.Trim(),
                PostalCode = model.PostalCode.Trim(),
                IsDefault = makeDefault
            });
            await Db.SaveChangesAsync();
            TempData["Success"] = "Address added successfully.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(CustomerAddressViewModel model)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            if (!ModelState.IsValid) { TempData["Error"] = "Please complete the address fields."; return RedirectToAction(nameof(Addresses)); }

            var address = await Db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == model.Id && a.CustomerId == customer.CustomerId);
            if (address == null) return NotFound();

            if (model.IsDefault)
            {
                var existing = await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId).ToListAsync();
                foreach (var a in existing) a.IsDefault = false;
            }

            address.Label = model.Label.Trim();
            address.AddressLine1 = model.AddressLine1.Trim();
            address.AddressLine2 = model.AddressLine2?.Trim();
            address.City = model.City.Trim();
            address.State = model.State.Trim();
            address.PostalCode = model.PostalCode.Trim();
            address.IsDefault = model.IsDefault;
            await Db.SaveChangesAsync();
            TempData["Success"] = "Address updated successfully.";
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            var addresses = await Db.CustomerAddresses.Where(a => a.CustomerId == customer.CustomerId).ToListAsync();
            if (addresses.All(a => a.Id != id)) return NotFound();
            foreach (var address in addresses) address.IsDefault = address.Id == id;
            await Db.SaveChangesAsync();
            return RedirectToAction(nameof(Addresses));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null) return Challenge(CustomerAuthDefaults.AuthenticationScheme);
            var address = await Db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customer.CustomerId);
            if (address != null) { Db.CustomerAddresses.Remove(address); await Db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Addresses));
        }
    }
}
