using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Equipment inventory and fault reporting for desk staff (sprint 1
    // feedback). RequireStaff() also admits Admin.
    public class EquipmentController : AppControllerBase
    {
        public EquipmentController(MyDbContext db) : base(db)
        {
        }

        public async Task<IActionResult> Manage()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment
                .OrderBy(e => e.Status == EquipmentStatus.Active ? 1 : 0)
                .ThenByDescending(e => e.Severity)
                .ThenBy(e => e.Name)
                .ToListAsync();
            return View(equipment);
        }

        public IActionResult Create()
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;
            return View(new Equipment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipment equipment)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            ModelState.Remove(nameof(Equipment.Status));
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.Status = EquipmentStatus.Active;
            Db.Equipment.Add(equipment);
            await Db.SaveChangesAsync();

            TempData["Success"] = "Equipment added.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFault(int id, EquipmentSeverity severity, string? notes)
        {
            var (staff, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            equipment.Status = EquipmentStatus.Faulty;
            equipment.Severity = severity;
            equipment.Notes = notes;
            equipment.ReportedByUserId = staff!.Id;
            equipment.ReportedAt = DateTime.UtcNow;
            equipment.ResolvedAt = null;
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} reported as faulty.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnderMaintenance(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            equipment.Status = EquipmentStatus.UnderMaintenance;
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} marked as under maintenance.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkResolved(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            equipment.Status = EquipmentStatus.Active;
            equipment.Severity = null;
            equipment.ResolvedAt = DateTime.UtcNow;
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} marked as resolved.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkInactive(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            equipment.Status = EquipmentStatus.Inactive;
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} marked as inactive.";
            return RedirectToAction(nameof(Manage));
        }
    }
}
