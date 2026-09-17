using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voice_AI_Agent.Data;
using Voice_AI_Agent.DTOs;
using Voice_AI_Agent.Model;

namespace Voice_AI_Agent.Controllers
{
    [ApiController]
    [Route("patients")]
    public class PatientController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PatientController(AppDbContext db)
        {
            _db = db;
        }

        // GET /patients?last_name=&date_of_birth=&phone_number=
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name = "last_name")] string? lastName,
                                             [FromQuery(Name = "date_of_birth")] string? dateOfBirth,
                                             [FromQuery(Name = "phone_number")] string? phoneNumber)
        {
            try
            {
                var query = _db.Patients.AsNoTracking().Where(p => p.DeletedAt == null);

                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    query = query.Where(p => p.LastName.Contains(lastName));
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    query = query.Where(p => p.PhoneNumber == phoneNumber);
                }

                if (!string.IsNullOrWhiteSpace(dateOfBirth))
                {
                    if (DateOnly.TryParse(dateOfBirth, out var dob))
                    {
                        query = query.Where(p => p.DateOfBirth == dob);
                    }
                    else
                    {
                        return UnprocessableEntity(ApiResponse<object>.Fail("Invalid date_of_birth format. Use MM/DD/YYYY or ISO date."));
                    }
                }

                var list = await query.ToListAsync();
                return Ok(ApiResponse<object>.Ok(list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Server error: " + ex.Message));
            }
        }

        // GET /patients/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PatientId == id && p.DeletedAt == null);
                if (patient == null)
                    return NotFound(ApiResponse<object>.Fail("Patient not found."));

                return Ok(ApiResponse<object>.Ok(patient));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Server error: " + ex.Message));
            }
        }

        // POST /patients
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PatientDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return UnprocessableEntity(ApiResponse<object>.Fail(errors));
                }

                if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                    return UnprocessableEntity(ApiResponse<object>.Fail("Date of birth cannot be in the future."));
                bool patientExists = await _db.Patients.AnyAsync(p => p.PhoneNumber == dto.PhoneNumber);
                if (patientExists)
                    return UnprocessableEntity(ApiResponse<object>.Fail("A patient with this phone number already exists."));

                var patient = new Patient
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    DateOfBirth = dto.DateOfBirth,
                    Sex = dto.Sex,
                    PhoneNumber = dto.PhoneNumber,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    State = dto.State,
                    ZipCode = dto.ZipCode,
                    Email = dto.Email,
                    InsuranceProvider = dto.InsuranceProvider,
                    InsuranceMemberId = dto.InsuranceMemberId,
                    PreferredLanguage = dto.PreferredLanguage,
                    EmergencyContactName = dto.EmergencyContactName,
                    EmergencyContactPhone = dto.EmergencyContactPhone
                };

                _db.Patients.Add(patient);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = patient.PatientId }, ApiResponse<object>.Ok(patient));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Server error: " + ex.Message));
            }
        }

        // PUT /patients/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PatientDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return UnprocessableEntity(ApiResponse<object>.Fail(errors));
                }

                if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                    return UnprocessableEntity(ApiResponse<object>.Fail("Date of birth cannot be in the future."));

                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == id && p.DeletedAt == null);
                if (patient == null)
                    return NotFound(ApiResponse<object>.Fail("Patient not found."));

                // Update allowed fields
                patient.FirstName = dto.FirstName;
                patient.LastName = dto.LastName;
                patient.DateOfBirth = dto.DateOfBirth;
                patient.Sex = dto.Sex;
                patient.PhoneNumber = dto.PhoneNumber;
                patient.AddressLine1 = dto.AddressLine1;
                patient.AddressLine2 = dto.AddressLine2;
                patient.City = dto.City;
                patient.State = dto.State;
                patient.ZipCode = dto.ZipCode;
                patient.Email = dto.Email;
                patient.InsuranceProvider = dto.InsuranceProvider;
                patient.InsuranceMemberId = dto.InsuranceMemberId;
                patient.PreferredLanguage = dto.PreferredLanguage;
                patient.EmergencyContactName = dto.EmergencyContactName;
                patient.EmergencyContactPhone = dto.EmergencyContactPhone;
                patient.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(patient));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Server error: " + ex.Message));
            }
        }

        // DELETE /patients/{id} (soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == id && p.DeletedAt == null);
                if (patient == null)
                    return NotFound(ApiResponse<object>.Fail("Patient not found."));

                patient.DeletedAt = DateTime.UtcNow;
                patient.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(ApiResponse<object>.Ok(patient));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Server error: " + ex.Message));
            }
        }
    }
}
