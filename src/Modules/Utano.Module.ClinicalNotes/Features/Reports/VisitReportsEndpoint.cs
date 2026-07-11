using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;

namespace Utano.Module.ClinicalNotes.Features.Reports;

[ApiController]
[Route("api/clinical/reports")]
[Authorize]
public class VisitReportsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("visits-by-doctor")]
    [ProducesResponseType(typeof(IReadOnlyList<VisitsByDoctorRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Visit counts grouped by doctor for a date range")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> VisitsByDoctor(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var result = await sender.Send(new VisitsByDoctorQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }

    [HttpGet("visit-demographics")]
    [ProducesResponseType(typeof(VisitDemographicsResponse), (int)HttpStatusCode.OK)]
    [EndpointSummary("Visit demographics (gender and age group) for a date range")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> VisitDemographics(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var result = await sender.Send(new VisitDemographicsQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }
}

// ── Visits by Doctor ─────────────────────────────────────────────────────────

public record VisitsByDoctorQuery(string? DateFrom, string? DateTo)
    : IRequest<IReadOnlyList<VisitsByDoctorRow>>;

public record VisitsByDoctorRow(string DoctorName, int Total, int Completed, int InProgress);

public class VisitsByDoctorHandler(ClinicalNotesDbContext db)
    : IRequestHandler<VisitsByDoctorQuery, IReadOnlyList<VisitsByDoctorRow>>
{
    public async Task<IReadOnlyList<VisitsByDoctorRow>> Handle(VisitsByDoctorQuery q, CancellationToken ct)
    {
        var query = db.Visits.AsNoTracking();

        if (DateOnly.TryParse(q.DateFrom, out var from))
            query = query.Where(v => v.VisitDate >= from);
        if (DateOnly.TryParse(q.DateTo, out var to))
            query = query.Where(v => v.VisitDate <= to);

        var visits = await query
            .Select(v => new { v.DoctorName, v.Status })
            .ToListAsync(ct);

        var rows = visits
            .GroupBy(v => v.DoctorName)
            .Select(g => new VisitsByDoctorRow(
                g.Key,
                g.Count(),
                g.Count(v => v.Status == Domain.Enums.VisitStatus.Completed),
                g.Count(v => v.Status == Domain.Enums.VisitStatus.InProgress)))
            .OrderByDescending(r => r.Total)
            .ToList();

        return rows;
    }
}

// ── Visit Demographics ────────────────────────────────────────────────────────

public record VisitDemographicsQuery(string? DateFrom, string? DateTo)
    : IRequest<VisitDemographicsResponse>;

public record AgeGroupBreakdown(int Under18, int Age18to35, int Age36to50, int Age51to65, int Over65, int Unknown);

public record GenderBreakdown(int Male, int Female, int Other, int Unknown);

public record DoctorDemographicsRow(
    string DoctorName,
    int Total,
    GenderBreakdown Gender,
    AgeGroupBreakdown AgeGroups);

public record VisitDemographicsResponse(
    GenderBreakdown OverallGender,
    AgeGroupBreakdown OverallAgeGroups,
    IReadOnlyList<DoctorDemographicsRow> ByDoctor);

public class VisitDemographicsHandler(ClinicalNotesDbContext db)
    : IRequestHandler<VisitDemographicsQuery, VisitDemographicsResponse>
{
    public async Task<VisitDemographicsResponse> Handle(VisitDemographicsQuery q, CancellationToken ct)
    {
        var query = db.Visits.AsNoTracking();

        if (DateOnly.TryParse(q.DateFrom, out var from))
            query = query.Where(v => v.VisitDate >= from);
        if (DateOnly.TryParse(q.DateTo, out var to))
            query = query.Where(v => v.VisitDate <= to);

        var visits = await query
            .Select(v => new { v.DoctorName, v.PatientGender, v.PatientDateOfBirth, v.VisitDate })
            .ToListAsync(ct);

        static GenderBreakdown CalcGender(IEnumerable<string?> genders)
        {
            var list = genders.ToList();
            return new GenderBreakdown(
                list.Count(g => string.Equals(g, "Male", StringComparison.OrdinalIgnoreCase)),
                list.Count(g => string.Equals(g, "Female", StringComparison.OrdinalIgnoreCase)),
                list.Count(g => g != null && !string.Equals(g, "Male", StringComparison.OrdinalIgnoreCase) && !string.Equals(g, "Female", StringComparison.OrdinalIgnoreCase)),
                list.Count(g => g == null));
        }

        static AgeGroupBreakdown CalcAgeGroups(IEnumerable<(DateOnly? Dob, DateOnly VisitDate)> items)
        {
            int under18 = 0, a18 = 0, a36 = 0, a51 = 0, over65 = 0, unknown = 0;
            foreach (var (dob, visitDate) in items)
            {
                if (dob == null) { unknown++; continue; }
                var age = visitDate.Year - dob.Value.Year;
                if (dob.Value.AddYears(age) > visitDate) age--;
                if (age < 18) under18++;
                else if (age <= 35) a18++;
                else if (age <= 50) a36++;
                else if (age <= 65) a51++;
                else over65++;
            }
            return new AgeGroupBreakdown(under18, a18, a36, a51, over65, unknown);
        }

        var overall = new VisitDemographicsResponse(
            CalcGender(visits.Select(v => v.PatientGender)),
            CalcAgeGroups(visits.Select(v => (v.PatientDateOfBirth, v.VisitDate))),
            visits
                .GroupBy(v => v.DoctorName)
                .Select(g => new DoctorDemographicsRow(
                    g.Key,
                    g.Count(),
                    CalcGender(g.Select(v => v.PatientGender)),
                    CalcAgeGroups(g.Select(v => (v.PatientDateOfBirth, v.VisitDate)))))
                .OrderByDescending(r => r.Total)
                .ToList());

        return overall;
    }
}
