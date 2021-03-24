using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TrackService.RethinkDb_Abstractions
{
    public interface ICoordinateChangeFeedbackBackgroundService
    {
        Task InsertCordinates(List<Location> locations, int institutionId, int vehicleId);
        List<IdealVehicleResponse> UpdateVehicleStatus();
        void ChangeVehicleStatus(string vehicleId);
        string GetInstitutionId(string mobileId);
        bool CheckVehicleExists(string vehicleId);
        bool CheckInstitutionExists(string institutionId);
        Task<IChangefeed<Coordinates>> GetCoordinatesChangeFeedback(CancellationToken cancellationToken);
        (List<VehicleDetails>, int) GetVehicles(string vehicleId, Pagination pageInfo, IdleModel idleModel);
        void SyncCoordinatesToArchiveTable();   
        void SyncVehiclesToArchiveTable();
        Task InsertMobiles(MobilesModel trackingStats);
        string GetVehicleId(string vehicleId);
        bool SuperInstitutions(string tokenInstitutionId);
        bool CheckVehicleByInstitutionExists(string vehicleId, string institutionId);
        void ClearLiveTrackingDatabase(string vehicleId);
    }
}