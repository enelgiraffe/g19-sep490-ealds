using g19_sep490_ealds.Server.DTOs.Suppliers;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace g19_sep490_ealds.Server.Services.Implementation;

public class SupplierService : ISupplierService
{
    private readonly EaldsDbContext _context;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(EaldsDbContext context, ILogger<SupplierService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SupplierDTO>> GetAllAsync(string? keyword)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(s =>
                s.Code.ToLower().Contains(kw) ||
                s.Name.ToLower().Contains(kw) ||
                (s.TaxCode ?? string.Empty).ToLower().Contains(kw) ||
                (s.Phone ?? string.Empty).ToLower().Contains(kw) ||
                (s.Email ?? string.Empty).ToLower().Contains(kw));
        }

        return await query
            .Select(s => new SupplierDTO
            {
                SupplierId = s.SupplierId,
                Code = s.Code,
                Name = s.Name,
                TaxCode = s.TaxCode,
                Address = s.Address,
                Phone = s.Phone,
                Email = s.Email,
                Status = s.Status,
                CreateDate = s.CreateDate
            })
            .ToListAsync();
    }

    public async Task<SupplierDTO> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with id {id} not found.");

        return new SupplierDTO
        {
            SupplierId = supplier.SupplierId,
            Code = supplier.Code,
            Name = supplier.Name,
            TaxCode = supplier.TaxCode,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Status = supplier.Status,
            CreateDate = supplier.CreateDate
        };
    }

    public async Task<SupplierDTO> CreateAsync(CreateSupplierDTO dto)
    {
        if (await _context.Suppliers.AnyAsync(s => s.Code == dto.Code))
            throw new InvalidOperationException("A supplier with this code already exists.");

        var supplier = new Supplier
        {
            Code = dto.Code,
            Name = dto.Name,
            TaxCode = dto.TaxCode,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            Status = dto.Status,
            CreateDate = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return new SupplierDTO
        {
            SupplierId = supplier.SupplierId,
            Code = supplier.Code,
            Name = supplier.Name,
            TaxCode = supplier.TaxCode,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Status = supplier.Status,
            CreateDate = supplier.CreateDate
        };
    }

    public async Task UpdateAsync(int id, UpdateSupplierDTO dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with id {id} not found.");

        if (supplier.Code != dto.Code && await _context.Suppliers.AnyAsync(s => s.Code == dto.Code))
            throw new InvalidOperationException("A supplier with this code already exists.");

        supplier.Code = dto.Code;
        supplier.Name = dto.Name;
        supplier.TaxCode = dto.TaxCode;
        supplier.Address = dto.Address;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Status = dto.Status;

        await _context.SaveChangesAsync();
    }

    public async Task<string?> DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with id {id} not found.");

        var hasProcurements = await _context.Procurements.AnyAsync(p => p.SupplierId == id);
        var hasRepairRecords = await _context.RepairRecords.AnyAsync(r => r.SupplierId == id);

        if (hasProcurements || hasRepairRecords)
        {
            supplier.Status = 0;
            await _context.SaveChangesAsync();
            return "Supplier is referenced by other records, so it was deactivated (status = 0) instead of permanently deleted.";
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return null;
    }
}
