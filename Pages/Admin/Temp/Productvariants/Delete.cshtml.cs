using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using losol.EventManagement.Data;
using losol.EventManagement.Models;

namespace losol.EventManagement.Pages.Admin.Temp.Productvariants
{
    public class DeleteModel : PageModel
    {
        private readonly losol.EventManagement.Data.ApplicationDbContext _context;

        public DeleteModel(losol.EventManagement.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProductVariant ProductVariant { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ProductVariant = await _context.ProductVariant
                .Include(p => p.Product).SingleOrDefaultAsync(m => m.ProductVariantId == id);

            if (ProductVariant == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ProductVariant = await _context.ProductVariant.FindAsync(id);

            if (ProductVariant != null)
            {
                _context.ProductVariant.Remove(ProductVariant);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
