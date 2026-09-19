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
        public async Task<IActionResult> Get([FromQuery] string? lastName,
                                             [FromQuery] string? dateOfBirth,
                                             [FromQuery] string? phoneNumber)
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
        public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
        {
            try
            {
                NormalizeEmptyStrings(dto);

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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientDto dto)
        {
            try
            {
                NormalizeEmptyStrings(dto);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return UnprocessableEntity(ApiResponse<object>.Fail(errors));
                }

                if (dto.DateOfBirth.HasValue && dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
                    return UnprocessableEntity(ApiResponse<object>.Fail("Date of birth cannot be in the future."));

                var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == id && p.DeletedAt == null);
                if (patient == null)
                    return NotFound(ApiResponse<object>.Fail("Patient not found."));

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != patient.PhoneNumber)
                {
                    bool phoneTaken = await _db.Patients.AnyAsync(p =>
                        p.PhoneNumber == dto.PhoneNumber && p.PatientId != id && p.DeletedAt == null);
                    if (phoneTaken)
                        return UnprocessableEntity(ApiResponse<object>.Fail("A patient with this phone number already exists."));
                }

                // Only overwrite fields the caller actually provided
                if (dto.FirstName != null) patient.FirstName = dto.FirstName;
                if (dto.LastName != null) patient.LastName = dto.LastName;
                if (dto.DateOfBirth.HasValue) patient.DateOfBirth = dto.DateOfBirth.Value;
                if (dto.Sex.HasValue) patient.Sex = dto.Sex.Value;
                if (dto.PhoneNumber != null) patient.PhoneNumber = dto.PhoneNumber;
                if (dto.AddressLine1 != null) patient.AddressLine1 = dto.AddressLine1;
                if (dto.AddressLine2 != null) patient.AddressLine2 = dto.AddressLine2;
                if (dto.City != null) patient.City = dto.City;
                if (dto.State != null) patient.State = dto.State;
                if (dto.ZipCode != null) patient.ZipCode = dto.ZipCode;
                if (dto.Email != null) patient.Email = dto.Email;
                if (dto.InsuranceProvider != null) patient.InsuranceProvider = dto.InsuranceProvider;
                if (dto.InsuranceMemberId != null) patient.InsuranceMemberId = dto.InsuranceMemberId;
                if (dto.PreferredLanguage != null) patient.PreferredLanguage = dto.PreferredLanguage;
                if (dto.EmergencyContactName != null) patient.EmergencyContactName = dto.EmergencyContactName;
                if (dto.EmergencyContactPhone != null) patient.EmergencyContactPhone = dto.EmergencyContactPhone;

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

        private static void NormalizeEmptyStrings(CreatePatientDto dto)
        {
            dto.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email;
            dto.InsuranceProvider = string.IsNullOrWhiteSpace(dto.InsuranceProvider) ? null : dto.InsuranceProvider;
            dto.InsuranceMemberId = string.IsNullOrWhiteSpace(dto.InsuranceMemberId) ? null : dto.InsuranceMemberId;
            dto.EmergencyContactName = string.IsNullOrWhiteSpace(dto.EmergencyContactName) ? null : dto.EmergencyContactName;
            dto.EmergencyContactPhone = string.IsNullOrWhiteSpace(dto.EmergencyContactPhone) ? null : dto.EmergencyContactPhone;
            dto.AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2;
        }

        private static void NormalizeEmptyStrings(UpdatePatientDto dto)
        {
            dto.FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? null : dto.FirstName;
            dto.LastName = string.IsNullOrWhiteSpace(dto.LastName) ? null : dto.LastName;
            dto.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber;
            dto.AddressLine1 = string.IsNullOrWhiteSpace(dto.AddressLine1) ? null : dto.AddressLine1;
            dto.AddressLine2 = string.IsNullOrWhiteSpace(dto.AddressLine2) ? null : dto.AddressLine2;
            dto.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City;
            dto.State = string.IsNullOrWhiteSpace(dto.State) ? null : dto.State;
            dto.ZipCode = string.IsNullOrWhiteSpace(dto.ZipCode) ? null : dto.ZipCode;
            dto.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email;
            dto.InsuranceProvider = string.IsNullOrWhiteSpace(dto.InsuranceProvider) ? null : dto.InsuranceProvider;
            dto.InsuranceMemberId = string.IsNullOrWhiteSpace(dto.InsuranceMemberId) ? null : dto.InsuranceMemberId;
            dto.PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? null : dto.PreferredLanguage;
            dto.EmergencyContactName = string.IsNullOrWhiteSpace(dto.EmergencyContactName) ? null : dto.EmergencyContactName;
            dto.EmergencyContactPhone = string.IsNullOrWhiteSpace(dto.EmergencyContactPhone) ? null : dto.EmergencyContactPhone;
        }
    }
}
