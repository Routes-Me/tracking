using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.RethinkDb_Changefeed.DataAccess.Abstraction
{
    public interface IDataAccessRepository
    {
        Task InsertCordinates(List<Location> locations, int institutionId, int vehicleId);
        Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken);
        string GetInstitutionId(string mobileId);
        bool CheckVehicleExists(string vehicleId);
        bool CheckInstitutionExists(string institutionId);
        List<IdealVehicleResponse> UpdateVehicleStatus();
        void ChangeVehicleStatus(string vehicleId);
        void SyncCoordinatesToArchiveTable();
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel mobileModel);
        bool SuperInstitutions(string tokenInstitutionId);
        bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId);
        string GetVehicleId(string mobileId);
    }
}
