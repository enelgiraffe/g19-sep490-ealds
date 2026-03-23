using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using g19_sep490_ealds.Server.Models;
using g19_sep490_ealds.Server.Models.DTOs;

namespace g19_sep490_ealds.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly EaldsDbContext _context;

    public SuppliersController(EaldsDbContext context)
    {
        _context = context;
    }

    // GET: api/Suppliers
    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
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

        return Ok(suppliers);
    }

    // GET: api/Suppliers/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            return NotFound();
        }

        var supplierDTO = new SupplierDTO
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

        return Ok(supplierDTO);
    }

    // POST: api/Suppliers
    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check for duplicate code
        if (await _context.Suppliers.AnyAsync(s => s.Code == dto.Code))
        {
            return BadRequest("A supplier with this code already exists.");
        }

        var supplier = new Supplier
        {
            Code = dto.Code,
            Name = dto.Name,
            TaxCode = dto.TaxCode,
            Address = dto.Address,
            Phone = dto.Phone,
            Email = dto.Email,
            Status = 1, // 1 for Active
            CreateDate = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var supplierDTO = new SupplierDTO
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

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplierDTO);
    }

    // PUT: api/Suppliers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            return NotFound();
        }

        // Check for duplicate code if code is changed
        if (supplier.Code != dto.Code && await _context.Suppliers.AnyAsync(s => s.Code == dto.Code))
        {
            return BadRequest("A supplier with this code already exists.");
        }

        supplier.Code = dto.Code;
        supplier.Name = dto.Name;
        supplier.TaxCode = dto.TaxCode;
        supplier.Address = dto.Address;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Status = dto.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SupplierExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Suppliers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        // Check if supplier is referenced in Procurements or RepairRecords
        var hasProcurements = await _context.Procurements.AnyAsync(p => p.SupplierId == id);
        var hasRepairRecords = await _context.RepairRecords.AnyAsync(r => r.SupplierId == id);

        if (hasProcurements || hasRepairRecords)
        {
            // Soft delete by setting status to 0
            supplier.Status = 0;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Supplier is referenced by other records, so it was deactivated (status = 0) instead of permanently deleted." });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SupplierExists(int id)
    {
        return _context.Suppliers.Any(e => e.SupplierId == id);
    }
}
