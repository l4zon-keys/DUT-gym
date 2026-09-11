using LoginFormASPCore6.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginFormASPCore6.Controllers
{
    // Equipment inventory and fault reporting for desk staff (sprint 1
    // feedback). RequireStaff() also admits Admin.
    public class EquipmentController : AppControllerBase
    {
        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxImageFileBytes = 5 * 1024 * 1024;

        private readonly IWebHostEnvironment environment;

        public EquipmentController(MyDbContext db, IWebHostEnvironment environment) : base(db)
        {
            this.environment = environment;
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
        public async Task<IActionResult> Create(Equipment equipment, IFormFile? equipmentPhotoFile)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            ModelState.Remove(nameof(Equipment.Status));

            var duplicate = await Db.Equipment.AnyAsync(e =>
                e.Name == equipment.Name && e.Location == equipment.Location && e.Status != EquipmentStatus.Inactive);
            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "Equipment with this name and location is already registered.");
            }

            ValidatePurchaseDates(equipment);

            string? extension = null;
            if (equipmentPhotoFile != null && equipmentPhotoFile.Length > 0)
            {
                extension = Path.GetExtension(equipmentPhotoFile.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, "Equipment photo must be a JPG or PNG file.");
                }
                else if (equipmentPhotoFile.Length > MaxImageFileBytes)
                {
                    ModelState.AddModelError(string.Empty, "Equipment photo is too large (5MB max).");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.Status = EquipmentStatus.Active;
            Db.Equipment.Add(equipment);
            await Db.SaveChangesAsync();

            if (equipmentPhotoFile != null && equipmentPhotoFile.Length > 0)
            {
                equipment.ImagePath = await SaveEquipmentFileAsync(equipment.Id, "", equipmentPhotoFile, extension!);
                await Db.SaveChangesAsync();
            }

            TempData["Success"] = "Equipment added.";
            return RedirectToAction(nameof(Manage));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            return View(equipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipment equipment, IFormFile? equipmentPhotoFile)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            if (id != equipment.Id) return NotFound();

            var existing = await Db.Equipment.FindAsync(id);
            if (existing == null) return NotFound();

            ModelState.Remove(nameof(Equipment.Status));

            var duplicate = await Db.Equipment.AnyAsync(e =>
                e.Id != id && e.Name == equipment.Name && e.Location == equipment.Location && e.Status != EquipmentStatus.Inactive);
            if (duplicate)
            {
                ModelState.AddModelError(string.Empty, "Equipment with this name and location is already registered.");
            }

            ValidatePurchaseDates(equipment);

            string? extension = null;
            if (equipmentPhotoFile != null && equipmentPhotoFile.Length > 0)
            {
                extension = Path.GetExtension(equipmentPhotoFile.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, "Equipment photo must be a JPG or PNG file.");
                }
                else if (equipmentPhotoFile.Length > MaxImageFileBytes)
                {
                    ModelState.AddModelError(string.Empty, "Equipment photo is too large (5MB max).");
                }
            }

            if (!ModelState.IsValid)
            {
                equipment.Status = existing.Status;
                return View(equipment);
            }

            existing.Name = equipment.Name;
            existing.Location = equipment.Location;
            existing.Brand = equipment.Brand;
            existing.Supplier = equipment.Supplier;
            existing.PurchaseDate = equipment.PurchaseDate;
            existing.PurchasePrice = equipment.PurchasePrice;
            existing.WarrantyExpiryDate = equipment.WarrantyExpiryDate;
            existing.InsuranceProvider = equipment.InsuranceProvider;
            existing.InsurancePolicyNumber = equipment.InsurancePolicyNumber;
            existing.InsuranceExpiryDate = equipment.InsuranceExpiryDate;

            if (equipmentPhotoFile != null && equipmentPhotoFile.Length > 0)
            {
                DeleteEquipmentFile(existing.ImagePath);
                existing.ImagePath = await SaveEquipmentFileAsync(existing.Id, "", equipmentPhotoFile, extension!);
            }

            await Db.SaveChangesAsync();

            TempData["Success"] = $"{existing.Name} updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFault(int id, EquipmentSeverity severity, string? notes, IFormFile? damagePhotoFile)
        {
            var (staff, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            if (equipment.Status == EquipmentStatus.Inactive)
            {
                TempData["Error"] = "Reactivate this equipment before reporting a fault.";
                return RedirectToAction(nameof(Manage));
            }

            string? extension = null;
            if (damagePhotoFile != null && damagePhotoFile.Length > 0)
            {
                extension = Path.GetExtension(damagePhotoFile.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(extension))
                {
                    TempData["Error"] = "Damage photo must be a JPG or PNG file.";
                    return RedirectToAction(nameof(Manage));
                }
                if (damagePhotoFile.Length > MaxImageFileBytes)
                {
                    TempData["Error"] = "Damage photo is too large (5MB max).";
                    return RedirectToAction(nameof(Manage));
                }
            }

            equipment.Status = EquipmentStatus.Faulty;
            equipment.Severity = severity;
            equipment.Notes = notes;
            equipment.ReportedByUserId = staff!.Id;
            equipment.ReportedAt = DateTime.UtcNow;
            equipment.ResolvedAt = null;

            if (damagePhotoFile != null && damagePhotoFile.Length > 0)
            {
                equipment.DamagePhotoPath = await SaveEquipmentFileAsync(equipment.Id, "damage", damagePhotoFile, extension!);
            }

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
            DeleteEquipmentFile(equipment.DamagePhotoPath);
            equipment.DamagePhotoPath = null;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkActive(int id)
        {
            var (_, redirect) = RequireStaff();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            if (equipment.Status != EquipmentStatus.Inactive)
            {
                TempData["Error"] = "Only inactive equipment can be reactivated.";
                return RedirectToAction(nameof(Manage));
            }

            equipment.Status = EquipmentStatus.Active;
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} reactivated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (_, redirect) = RequireAdmin();
            if (redirect != null) return redirect;

            var equipment = await Db.Equipment.FindAsync(id);
            if (equipment == null) return NotFound();

            if (equipment.Status != EquipmentStatus.Inactive)
            {
                TempData["Error"] = "Equipment must be marked inactive before it can be deleted.";
                return RedirectToAction(nameof(Manage));
            }

            var uploadDir = Path.Combine(environment.WebRootPath, "uploads", "equipment", equipment.Id.ToString());
            if (Directory.Exists(uploadDir))
            {
                Directory.Delete(uploadDir, recursive: true);
            }

            Db.Equipment.Remove(equipment);
            await Db.SaveChangesAsync();

            TempData["Success"] = $"{equipment.Name} permanently deleted.";
            return RedirectToAction(nameof(Manage));
        }

        private void ValidatePurchaseDates(Equipment equipment)
        {
            if (equipment.PurchaseDate == null) return;

            if (equipment.WarrantyExpiryDate != null && equipment.WarrantyExpiryDate < equipment.PurchaseDate)
            {
                ModelState.AddModelError(nameof(Equipment.WarrantyExpiryDate), "Warranty expiry can't be before the purchase date.");
            }
            if (equipment.InsuranceExpiryDate != null && equipment.InsuranceExpiryDate < equipment.PurchaseDate)
            {
                ModelState.AddModelError(nameof(Equipment.InsuranceExpiryDate), "Insurance expiry can't be before the purchase date.");
            }
        }

        private async Task<string> SaveEquipmentFileAsync(int equipmentId, string subfolder, IFormFile file, string extension)
        {
            var relativeDir = string.IsNullOrEmpty(subfolder)
                ? Path.Combine("uploads", "equipment", equipmentId.ToString())
                : Path.Combine("uploads", "equipment", equipmentId.ToString(), subfolder);
            var absoluteDir = Path.Combine(environment.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var generatedFileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDir, generatedFileName);
            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine(relativeDir, generatedFileName).Replace('\\', '/');
        }

        private void DeleteEquipmentFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            var absolutePath = Path.Combine(environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }
    }
}
