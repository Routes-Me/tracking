using System.Threading.Tasks;
using TrackService.Dtos;

namespace TrackService.Services
{
    public interface ICheckinService
    {
        Task<CheckinReadDto> PostCheckin(CheckinCreateDto checkinCreateDto);
    }
}
