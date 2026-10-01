using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public class BusinessIdGeneratorService : IBusinessIdGeneratorService
    {
        private const string Prefix = "LNK";
        private const string UnspecifiedCode = "00";

        private readonly ApplicationDbContext _context;

        public BusinessIdGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateBusinessId(string area, string road, string street)
        {
            var areaCode = await GetOrCreateLocationCode("Area", area);
            var roadCode = await GetOrCreateLocationCode("Road", road);
            var streetCode = await GetOrCreateLocationCode("Street", street);

            var sequence = await GetNextSequence(areaCode, roadCode, streetCode);

            return $"{Prefix}{areaCode}{roadCode}{streetCode}{sequence:D4}";
        }

        private async Task<string> GetOrCreateLocationCode(string type, string rawValue)
        {
            var normalized = (rawValue ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized))
            {
                return UnspecifiedCode;
            }

            var existing = await _context.LocationCodes
                .FirstOrDefaultAsync(l => l.Type == type && l.Value == normalized);
            if (existing != null)
            {
                return existing.Code;
            }

            // Deterministic: assign the next available 2-digit code for this type,
            // based on how many distinct values of this type have been seen so far.
            var count = await _context.LocationCodes.CountAsync(l => l.Type == type);
            var code = ((count % 99) + 1).ToString("D2");

            var locationCode = new LocationCode
            {
                Type = type,
                Value = normalized,
                Code = code,
                CreatedAt = DateTime.UtcNow
            };

            _context.LocationCodes.Add(locationCode);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another request inserted the same (Type, Value) concurrently - re-read it.
                var raced = await _context.LocationCodes
                    .FirstOrDefaultAsync(l => l.Type == type && l.Value == normalized);
                if (raced != null)
                {
                    return raced.Code;
                }
                throw;
            }

            return code;
        }

        private async Task<int> GetNextSequence(string areaCode, string roadCode, string streetCode)
        {
            var seq = await _context.BusinessIdSequences.FirstOrDefaultAsync(s =>
                s.AreaCode == areaCode && s.RoadCode == roadCode && s.StreetCode == streetCode);

            if (seq == null)
            {
                seq = new BusinessIdSequence
                {
                    AreaCode = areaCode,
                    RoadCode = roadCode,
                    StreetCode = streetCode,
                    NextSequence = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.BusinessIdSequences.Add(seq);
            }

            var sequence = seq.NextSequence;
            seq.NextSequence = sequence + 1;
            seq.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return sequence;
        }
    }
}
