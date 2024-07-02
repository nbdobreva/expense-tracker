using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.Controllers
{
    public class BudgetEntryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetEntryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            PopulateCategoriesDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Description,Amount,Date,CategoryId")] BudgetEntry budgetEntry)
        {
            if (ModelState.IsValid)
            {
                _context.Add(budgetEntry);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCategoriesDropDownList(budgetEntry.CategoryId);
            return View(budgetEntry);
        }

        private void PopulateCategoriesDropDownList(object selectedCategory = null)
        {
            var categoriesQuery = from c in _context.Categories
                                  orderby c.Name
                                  select c;

            ViewBag.CategoryId = new SelectList(categoriesQuery.AsNoTracking(), "Id", "Name", selectedCategory);
        }

        public async Task<IActionResult> Index(string timeRange)
        {
            ViewBag.TimeRange = timeRange;

            var budgetEntries = _context.BudgetEntries.Include(b => b.Category).AsQueryable();

            if (!string.IsNullOrEmpty(timeRange))
            {
                switch (timeRange)
                {
                    case "today":
                        var today = DateTime.Now.Date;
                        budgetEntries = budgetEntries.Where(e => e.Date.Date == today);
                        break;
                    case "lastWeek":
                        var lastWeek = DateTime.Now.AddDays(-7);
                        budgetEntries = budgetEntries.Where(e => e.Date >= lastWeek);
                        break;
                    case "lastMonth":
                        var lastMonth = DateTime.Now.AddMonths(-1);
                        budgetEntries = budgetEntries.Where(e => e.Date >= lastMonth);
                        break;
                    default:
                        break;
                }
            }

            decimal totalAmount = await budgetEntries.SumAsync(entry => entry.Amount);
            ViewBag.TotalAmount = totalAmount;

            return View(await budgetEntries.ToListAsync());
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetEntry = await _context.BudgetEntries.FindAsync(id);
            if (budgetEntry == null)
            {
                return NotFound();
            }

            PopulateCategoriesDropDownList(budgetEntry.CategoryId);
            return View(budgetEntry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,Amount,Date,CategoryId")] BudgetEntry budgetEntry)
        {
            if (id != budgetEntry.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(budgetEntry);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BudgetEntryExists(budgetEntry.Id))
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

            PopulateCategoriesDropDownList(budgetEntry.CategoryId);
            return View(budgetEntry);
        }

        private bool BudgetEntryExists(int id)
        {
            return _context.BudgetEntries.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var budgetEntry = await _context.BudgetEntries
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (budgetEntry == null)
            {
                return NotFound();
            }

            return View(budgetEntry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var budgetEntry = await _context.BudgetEntries.FindAsync(id);
            _context.BudgetEntries.Remove(budgetEntry);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}